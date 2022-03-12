using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Abilifier;
using Abilifier.Patches;
using BattleTech;
using BattleTech.Rendering;
using BattleTech.Save;
using BattleTech.UI;
using CustAmmoCategories;
using CustomActivatableEquipment;
using CustomComponents;
using CustomUnits;
using DG.Tweening;
using Harmony;
using HBS.Math;
using HBS.Pooling;
using Localize;
using StrategicOperations.Framework;
using SVGImporter;
using UnityEngine;
using UnityEngine.UI;
using MechStructureRules = BattleTech.MechStructureRules;
using TrooperSquad = CustomUnits.TrooperSquad;

namespace StrategicOperations.Patches
{
    public class GarrisonLOSPatches
    {
        // Copied wholesale from FrostRaptor's ConcreteJungle, because why reinvent the wheel. Plus if I made it the wheel would be square.
        // When a trap turret's line of sight is calculated, give it 'x-ray' vision to see through the shell building.
        [HarmonyPatch(typeof(LineOfSight), "GetVisibilityToTargetWithPositionsAndRotations")]
        [HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(Vector3), typeof(ICombatant), typeof(Vector3), typeof(Quaternion) })]
        static class LineOfSight_GetVisibilityToTargetWithPositionsAndRotations
        {
            static void Prefix(AbstractActor source, Vector3 sourcePosition, ICombatant target, Vector3 targetPosition, Quaternion targetRotation)
            {
                if (source.isGarrisoned())
                {
                    ModState.CurrentGarrisonSquadForLOS = source;
                }

            }

            static void Postfix(AbstractActor source, Vector3 sourcePosition, ICombatant target, Vector3 targetPosition, Quaternion targetRotation, VisibilityLevel __result)
            {
                if (ModState.CurrentGarrisonSquadForLOS != null)
                {
                    ModState.CurrentGarrisonSquadForLOS = null;
                }
            }
        }

        // Modify the vision test to allow 'x-ray' vision through the shell building for trap turrets
        [HarmonyPatch(typeof(LineOfSight), "bresenhamVisionTest")]
        static class LineOfSight_bresenhamVisionTest
        {
            static void Postfix(LineOfSight __instance, Point p0, float height0, Point p1, float height1, Vector3 unitDelta, string targetGuid,
                ref float __result, CombatGameState ___Combat)
            {
                if (ModState.CurrentGarrisonSquadForLOS == null) return;

                if (p0.X == p1.X && p0.Z == p1.Z)
                {
                    __result = 0f;
                    return;
                }

                if (!___Combat.MapMetaData.IsWithinBounds(p0) || !___Combat.MapMetaData.IsWithinBounds(p1))
                {
                    __result = float.MaxValue;
                    return;
                }

                float numCellsX = Mathf.Abs(unitDelta.x) * (float)MapMetaDataExporter.cellSize;
                float numCellsY = Mathf.Abs(unitDelta.z) * (float)MapMetaDataExporter.cellSize;
                float lineDeltaX = (float)(p1.X - p0.X);
                float lineDeltaZ = (float)(p1.Z - p0.Z);
                float greatestDivisor = Mathf.Max(Mathf.Abs(lineDeltaX), Mathf.Abs(lineDeltaZ));
                float stepHeight = (height1 - height0) / greatestDivisor;
                float sumVisionCost = 0f;

                Traverse projectedHeightAtT = Traverse.Create(__instance).Method("getProjectedHeightAt", new Type[] { typeof(Point), typeof(float), typeof(Point), typeof(float) });
                Traverse visCostOfCellT = Traverse.Create(__instance).Method("visCostOfCell", new Type[] { typeof(MapTerrainDataCell), typeof(float) });
                string shellBuildingGUID = ModState.PositionLockGarrison[ModState.CurrentGarrisonSquadForLOS.GUID].BuildingGUID;
                EncounterLayerData encounterLayerData = ___Combat.EncounterLayerData;

                List<Point> list = BresenhamLineUtil.BresenhamLine(p0, p1);
                for (int i = 1; i < list.Count; i++)
                {
                    float stepDelta;
                    if (list[i].X != list[i - 1].X)
                    {
                        stepDelta = numCellsX;
                    }
                    else
                    {
                        stepDelta = numCellsY;
                    }

                    // Increment vision cost only slightly if it's inside our shell building
                    if (encounterLayerData.mapEncounterLayerDataCells[list[i].Z, list[i].X].HasSpecifiedBuilding(shellBuildingGUID))
                    {
                        ModInit.modLog?.Debug?.Write($" Point x={list[i].X} z={list[i].Z} is inside the shell building, adding vision cost normally.");
                        //sumVisionCost += stepDelta;
                    }
                    else
                    {
                        float projectedHeightAt = projectedHeightAtT.GetValue<float>(new object[] { p0, height0, list[i], stepHeight });
                        MapTerrainDataCell mapTerrainDataCell = ___Combat.MapMetaData.mapTerrainDataCells[list[i].Z, list[i].X];
                        if (mapTerrainDataCell.cachedHeight > projectedHeightAt)
                        {
                            if (mapTerrainDataCell.MapEncounterLayerDataCell.HasBuilding)
                            {
                                for (int j = 0; j < mapTerrainDataCell.MapEncounterLayerDataCell.buildingList.Count; j++)
                                {
                                    if (ObstructionGameLogic.GuidsMatchObjectOrRep(mapTerrainDataCell.MapEncounterLayerDataCell.buildingList[j].buildingGuid, targetGuid))
                                    {
                                        __result = sumVisionCost;
                                        return;
                                    }
                                }
                            }

                            __result = float.MaxValue;
                            return;
                        }

                        sumVisionCost += visCostOfCellT.GetValue<float>(new object[] { mapTerrainDataCell, projectedHeightAt }) * stepDelta;
                    }
                }

                __result = sumVisionCost;
                return;
            }
        }

        // When a trap turret's line of fire is calculated, give it 'x-ray' vision to see through the shell building.
        [HarmonyPatch(typeof(LOFCache), "GetLineOfFire")]
        static class LOFCache_GetLineOfFire
        {

            static void Prefix(AbstractActor source, ICombatant target, LineOfFireLevel __result)
            {
                if (source.isGarrisoned())
                {
                    ModInit.modLog?.Debug?.Write($"unit {source.DisplayName} is calculating LOF to target {target.DisplayName} at x: {target.CurrentPosition.x}, z: {target.CurrentPosition.z}");
                    ModState.CurrentGarrisonSquadForLOF = source;
                }
            }

            static void Postfix(AbstractActor source, ICombatant target, LineOfFireLevel __result)
            {
                if (ModState.CurrentGarrisonSquadForLOF != null)
                {
                    ModState.CurrentGarrisonSquadForLOF = null;
                }
            }
        }

        // Modify the vision test to allow 'x-ray' vision through the shell building for trap turrets
        [HarmonyPatch(typeof(LineOfSight), "bresenhamHeightTest")]
        static class LineOfSight_bresenhamHeightTest
        {

            static void Postfix(LineOfSight __instance, Point p0, float height0, Point p1, float height1, string targetedBuildingGuid, ref Point collisionWorldPos,
                ref bool __result, CombatGameState ___Combat)
            {

                if (ModState.CurrentGarrisonSquadForLOF == null) return;

                ModInit.modLog?.Debug?.Write($"Recalculating LOF from {ModState.CurrentGarrisonSquadForLOF.DisplayName} due to collision on building shell. " +
                                        $"CollisonWorldPos=> x={collisionWorldPos.X} z={collisionWorldPos.Z}");

                collisionWorldPos = p1;

                // If the origin and target points are the same, there is a collision
                if (p0.X == p1.X && p0.Z == p1.Z)
                {
                    __result = true;
                    return;
                }

                // If the origin or target points are outsie the bounds of the map, there is no collision (because how could there be)
                if (!___Combat.MapMetaData.IsWithinBounds(p0) || !___Combat.MapMetaData.IsWithinBounds(p1))
                {
                    __result = false;
                    return;
                }

                MapMetaData mapMetaData = ___Combat.MapMetaData;
                EncounterLayerData encounterLayerData = ___Combat.EncounterLayerData;

                bool targetIsABuilding = !string.IsNullOrEmpty(targetedBuildingGuid);
                string shellBuildingGUID = ModState.PositionLockGarrison[ModState.CurrentGarrisonSquadForLOF.GUID].BuildingGUID;

                List<Point> bresenhamLinePoints = BresenhamLineUtil.BresenhamLine(p0, p1);
                float heightDeltaPerPoint = (height1 - height0) / (float)bresenhamLinePoints.Count;
                float collisionPointHeight = height0;
                // Walk the bresenham Line, evaluation collision at a speciifc height as we go.
                for (int i = 0; i < bresenhamLinePoints.Count; i++)
                {
                    collisionPointHeight += heightDeltaPerPoint;
                    Point point = bresenhamLinePoints[i];

                    if (encounterLayerData.mapEncounterLayerDataCells[point.Z, point.X].HasSpecifiedBuilding(shellBuildingGUID))
                    {
                        ModInit.modLog?.Debug?.Write($" Point x={point.X} z={point.Z} is inside the shell building, continuing.");
                        continue;
                    }

                    if (targetIsABuilding && encounterLayerData.mapEncounterLayerDataCells[point.Z, point.X].HasSpecifiedBuilding(targetedBuildingGuid))
                    {
                        ModInit.modLog?.Debug?.Write($" Building {targetedBuildingGuid} conflicts with the LoS, collision at x={collisionWorldPos.X} z={collisionWorldPos.Z}");
                        collisionWorldPos = bresenhamLinePoints[i];
                        __result = true;
                        return;
                    }

                    if (mapMetaData.mapTerrainDataCells[point.Z, point.X].cachedHeight > collisionPointHeight)
                    {
                        ModInit.modLog?.Debug?.Write($" Collision on terrain at position x={collisionWorldPos.X} z={collisionWorldPos.Z}");
                        collisionWorldPos = bresenhamLinePoints[i];
                        __result = false;
                        return;
                    }
                }

                ModInit.modLog?.Trace?.Write($"No collision detected, changing LoF to true. CollisonWorldPos => x ={ collisionWorldPos.X} z ={ collisionWorldPos.Z}");

                __result = true;
                return;

            }
        }
    }
    public class BattleArmorPatches
    {
       [HarmonyPatch(typeof(ActivatableComponent), "activateComponent", new Type[] {typeof(MechComponent), typeof(bool), typeof(bool)})]
        public static class ActivatableComponent_activateComponent
        {
            public static void Postfix(ActivatableComponent __instance, MechComponent component, bool autoActivate, bool isInital)
            {
                if (ModInit.modSettings.BPodComponentIDs.Contains(component.defId))
                {
                    ActivatableComponent activatableComponent = component.componentDef.GetComponent<ActivatableComponent>();
                    var enemyActors = component.parent.GetAllEnemiesWithinRange(activatableComponent.Explosion.Range);
                    foreach (var enemyActor in enemyActors)
                    {
                        if (enemyActor is TrooperSquad trooperSquad)
                        {
                            if (trooperSquad.IsSwarmingUnit() && ModState.PositionLockSwarm[trooperSquad.GUID] == component.parent.GUID)
                            {
                                trooperSquad.DismountBA(component.parent, Vector3.zero, true);
                            }
                            
                            var baLoc = trooperSquad.GetPossibleHitLocations(component.parent);
                            var podDmg = activatableComponent.Explosion.Damage;
                            //var podDmg = component.parent.StatCollection.GetValue<float>("SquishumToadsAsplode");
                            //var divDmg = podDmg / baLoc.Count;

                            var clusters = BattleArmorUtils.CreateBPodDmgClusters(baLoc, podDmg);

                            for (int i = 0; i < clusters.Count; i++)
                            {
                                ModInit.modLog?.Info?.Write($"[ActivatableComponent - activateComponent] BA Armor Damage Location {baLoc}: {trooperSquad.GetStringForArmorLocation((ArmorLocation)baLoc[i])} for {clusters[i]}");
                                var hitinfo = new WeaponHitInfo(-1, -1, 0, 0, component.parent.GUID, trooperSquad.GUID, 1, new float[1], new float[1], new float[1], new bool[1], new int[baLoc[i]], new int[1], new AttackImpactQuality[1], new AttackDirection[1], new Vector3[1], new string[1], new int[baLoc[i]]);
                                trooperSquad.TakeWeaponDamage(hitinfo, baLoc[i], trooperSquad.MeleeWeapon, clusters[i], 0, 0, DamageType.ComponentExplosion);

                                var vector = trooperSquad.GameRep.GetHitPosition(baLoc[i]);
                                var message = new FloatieMessage(hitinfo.attackerId, trooperSquad.GUID, $"{(int)Mathf.Max(1f, clusters[i])}", trooperSquad.Combat.Constants.CombatUIConstants.floatieSizeMedium, FloatieMessage.MessageNature.ArmorDamage, vector.x, vector.y, vector.z);
                                trooperSquad.Combat.MessageCenter.PublishMessage(message);
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SelectionStateTargetSingleCombatantBase), "ProcessClickedCombatant", new Type[] {typeof(ICombatant)})]
        public static class SelectionStateTargetSingleCombatantBase_ProcessClickedCombatant
        {
            //private static bool Prepare() => false;
            public static void Postfix(SelectionStateTargetSingleCombatantBase __instance, ICombatant combatant)
            {
                if (__instance.FromButton.Ability.Def.Id == ModInit.modSettings.BattleArmorMountAndSwarmID)
                {
                    var cHUD = Traverse.Create(__instance).Property("HUD").GetValue<CombatHUD>();
                    var creator = cHUD.SelectedActor;
                    
                    if (creator is Mech creatorMech && combatant != null && combatant.team.IsEnemy(creator.team))
                    {
                        var chance = creator.Combat.ToHit.GetToHitChance(creator, creatorMech.MeleeWeapon, combatant, creator.CurrentPosition, combatant.CurrentPosition, 1, MeleeAttackType.Charge, false);
                        ModInit.modLog?.Trace?.Write($"[SelectionState.ShowFireButton - Swarm Success calculated as {chance}, storing in state.");
                        ModState.SwarmSuccessChance = chance;
                        var chanceDisplay = (float)Math.Round(chance, 2) * 100;
                        cHUD.AttackModeSelector.FireButton.FireText.SetText($"{chanceDisplay}% - Confirm", Array.Empty<object>());
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SelectionStateAbilityInstant), "OnAddToStack", new Type[] {})]
        public static class SelectionStateAbilityInstant_OnAddToStack
        {
            public static void Postfix(SelectionStateAbilityInstant __instance)
            {
                var cHUD = Traverse.Create(__instance).Property("HUD").GetValue<CombatHUD>();
                var creator = cHUD.SelectedActor;
                if (__instance.FromButton.Ability.Def.Id == ModInit.modSettings.BattleArmorDeSwarmRoll)
                {
                    var settings = ModInit.modSettings.DeswarmConfigs.ContainsKey(ModInit.modSettings.BattleArmorDeSwarmRoll)
                        ? ModInit.modSettings.DeswarmConfigs[ModInit.modSettings.BattleArmorDeSwarmRoll]
                        : new Classes.BA_DeswarmAbilityConfig();
                    //var parsed = float.TryParse(__instance.FromButton.Ability.Def.EffectData
                    //    .FirstOrDefault(x => x.statisticData.statName == "BattleArmorDeSwarmerRoll")
                    //    ?.statisticData
                    //    .modValue, out var baseChance);

                    var pilotSkill = creator.GetPilot().Piloting;
                    var finalChance = Mathf.Min(settings.BaseSuccessChance + (0.05f * pilotSkill), settings.MaxSuccessChance);
                    ModInit.modLog?.Info?.Write($"[SelectionStateAbilityInstant.OnAddToStack - BattleArmorDeSwarm] Deswarm chance: {finalChance} from baseChance {settings.BaseSuccessChance} + pilotSkill x 0.05 {0.05f * pilotSkill}, max {settings.MaxSuccessChance}., stored in state.");
                    ModState.DeSwarmSuccessChance = finalChance;
                    var chanceDisplay = (float)Math.Round(finalChance, 3) * 100;
                    cHUD.AttackModeSelector.FireButton.FireText.SetText($"{chanceDisplay}% - Confirm", Array.Empty<object>());
                }
                else if (__instance.FromButton.Ability.Def.Id == ModInit.modSettings.BattleArmorDeSwarmSwat)
                {
                    var settings = ModInit.modSettings.DeswarmConfigs.ContainsKey(ModInit.modSettings.BattleArmorDeSwarmSwat)
                        ? ModInit.modSettings.DeswarmConfigs[ModInit.modSettings.BattleArmorDeSwarmSwat]
                        : new Classes.BA_DeswarmAbilityConfig();
                    //var parsed = float.TryParse(__instance.FromButton.Ability.Def.EffectData
                    //    .FirstOrDefault(x => x.statisticData.statName == "BattleArmorDeSwarmerSwat")
                    //    ?.statisticData
                    //    .modValue, out var baseChance);
                    //if (!parsed) baseChance = 0.55f;

                    var pilotSkill = creator.GetPilot().Piloting;
                    var missingActuatorCount = -8;
                    foreach (var armComponent in creator.allComponents.Where(x =>
                                 x.IsFunctional && (x.Location == 2 || x.Location == 32)))
                    {
                        foreach (var CategoryID in ModInit.modSettings.ArmActuatorCategoryIDs)
                        {
                            if (armComponent.mechComponentRef.IsCategory(CategoryID))
                            {
                                missingActuatorCount += 1;
                                break;
                            }
                        }
                    }

                    var finalChance = Mathf.Min(settings.BaseSuccessChance + (0.05f * pilotSkill) - (0.05f * missingActuatorCount), settings.MaxSuccessChance);

                    ModInit.modLog?.Info?.Write($"[SelectionStateAbilityInstant.OnAddToStack - BattleArmorDeSwarm] Deswarm chance: {finalChance} from baseChance {settings.BaseSuccessChance} + pilotSkill x 0.05 {0.05f * pilotSkill} - missingActuators x 0.05 {0.05f * missingActuatorCount}, max {settings.MaxSuccessChance} stored in state.");
                    ModState.DeSwarmSuccessChance = finalChance;
                    var chanceDisplay = (float)Math.Round(finalChance, 3) * 100;
                    cHUD.AttackModeSelector.FireButton.FireText.SetText($"{chanceDisplay}% - Confirm", Array.Empty<object>());
                }
            }
        }

        [HarmonyPatch(typeof(AttackDirector.AttackSequence), "IsBreachingShot", MethodType.Getter)]
        public static class AttackDirector_AttackSequence_IsBreachingShot
        {
            static bool Prepare() => !ModInit.modSettings.UsingMechAffinityForSwarmBreach;
            public static void Postfix(AttackDirector.AttackSequence __instance, ref bool __result)
            {
                if (!__result)
                {
                    if (__instance.chosenTarget is AbstractActor targetActor)
                    {
                        if (__instance.attacker.IsSwarmingTargetUnit(targetActor))
                        {
                            __result = true;
                        }
                    }
                }
            }
        }


        [HarmonyPatch(typeof(ActorMovementSequence), "CompleteOrders")]
        public static class ActorMovementSequence_CompleteOrders
        {
            public static void Postfix(ActorMovementSequence __instance)
            {
                try
                {
                    if (__instance.owningActor == null) return;
                    if (ModState.DeSwarmMovementInfo?.Carrier?.GUID == __instance.owningActor.GUID)
                    {
                        var settings = ModInit.modSettings.DeswarmMovementConfig;

                        var baseChance = settings.BaseSuccessChance; //__instance.owningActor.getMovementDeSwarmMinChance();
                        var chanceFromPips = __instance.owningActor.EvasivePipsCurrent * settings.EvasivePipsFactor;//__instance.owningActor.getMovementDeSwarmEvasivePipsFactor();
                        var finalChance = Mathf.Min(baseChance + chanceFromPips, settings.MaxSuccessChance);
                            //__instance.owningActor.getMovementDeSwarmMaxChance());
                        var roll = ModInit.Random.NextDouble();
                        ModInit.modLog?.Info?.Write(
                            $"[ActorMovementSequence.CompleteOrders] Found DeSwarmMovementInfo for unit {__instance.owningActor.DisplayName} {__instance.owningActor.GUID}. Rolled {roll} vs finalChance {finalChance} from baseChance {baseChance} and evasive chance {chanceFromPips}");
                        if (roll <= finalChance)
                        {
                            var waypoints = Traverse.Create(__instance).Property("Waypoints")
                                .GetValue<List<WayPoint>>();
                            foreach (var swarmingUnit in ModState.DeSwarmMovementInfo?.SwarmingUnits)
                            {
                                var selectedWaypoint = waypoints.GetRandomElement();
                                ModInit.modLog?.Info?.Write(
                                    $"[ActorMovementSequence.CompleteOrders] Roll succeeded, plonking {swarmingUnit.DisplayName} at {selectedWaypoint.Position}");
                                swarmingUnit.DismountBA(__instance.owningActor, selectedWaypoint.Position, true);
                            }
                        }
                        ModState.DeSwarmMovementInfo = new Classes.BA_DeswarmMovementInfo();
                    }
                }
                catch (Exception ex)
                {
                    ModInit.modLog?.Error?.Write(ex.ToString());
                }
            }
        }

        [HarmonyPatch(typeof(MechJumpSequence), "CompleteOrders")]
        public static class MechJumpSequence_CompleteOrders
        {
            public static void Postfix(MechJumpSequence __instance)
            {
                if (__instance.OwningMech == null) return;
                var settings = ModInit.modSettings.DeswarmMovementConfig;

                if (ModState.DeSwarmMovementInfo?.Carrier?.GUID == __instance.OwningMech.GUID)
                {
                    var baseChance = settings.BaseSuccessChance; //__instance.owningActor.getMovementDeSwarmMinChance();
                    var chanceFromPips = __instance.owningActor.EvasivePipsCurrent * settings.EvasivePipsFactor;
                                         //__instance.owningActor.getMovementDeSwarmEvasivePipsFactor();
                                         var finalChance =
                                             Mathf.Min((baseChance + chanceFromPips) * settings.JumpMovementModifier,
                                                 settings.MaxSuccessChance);//__instance.owningActor.getMovementDeSwarmEvasiveJumpMovementMultiplier(), __instance.owningActor.getMovementDeSwarmMaxChance());
                    var roll = ModInit.Random.NextDouble();
                    ModInit.modLog?.Info?.Write($"[ActorMovementSequence.CompleteOrders] Found DeSwarmMovementInfo for unit {__instance.owningActor.DisplayName} {__instance.owningActor.GUID}. Rolled {roll} vs finalChance {finalChance} from (baseChance {baseChance} + evasive chance {chanceFromPips}) x JumpMovementMulti {settings.JumpMovementModifier}");
                    if (roll <= finalChance)
                    {
                        var baseDistance = Vector3.Distance(__instance.StartPos, __instance.FinalPos);

                        foreach (var swarmingUnit in ModState.DeSwarmMovementInfo.SwarmingUnits)
                        {
                            var finalDist = (float)(baseDistance * ModInit.Random.NextDouble());
                            var finalDestination =
                                Utils.LerpByDistance(__instance.StartPos, __instance.FinalPos, finalDist);
                            finalDestination.y = swarmingUnit.Combat.MapMetaData.GetLerpedHeightAt(finalDestination, false); //set proper height on ground.
                            ModInit.modLog?.Info?.Write(
                                $"[ActorMovementSequence.CompleteOrders] Roll succeeded, plonking {swarmingUnit.DisplayName} at {finalDestination}");
                            swarmingUnit.DismountBA(__instance.owningActor, finalDestination, true);
                            if (swarmingUnit is TrooperSquad swarmingUnitSquad)
                            {
                                var dmg = settings.UseDFADamage
                                    ? swarmingUnitSquad.StatCollection.GetValue<float>("DFASelfDamage")
                                    : settings.LocationDamageOverride;
                                var reduction = settings.PilotingDamageReductionFactor *
                                                swarmingUnitSquad.GetPilot().Piloting;
                                var dmgReduction = dmg * reduction;
                                dmg -= dmgReduction;
                                if (dmg > 0f)
                                {
                                    var trooperLocs = swarmingUnitSquad.GetPossibleHitLocations(__instance.owningActor);
                                    for (int i = 0; i < trooperLocs.Count; i++)
                                    {
                                        var hitinfo = new WeaponHitInfo(-1, -1, 0, 0, __instance.owningActor.GUID,
                                            swarmingUnitSquad.GUID, 1, new float[1], new float[1], new float[1],
                                            new bool[1], new int[trooperLocs[i]], new int[1],
                                            new AttackImpactQuality[1],
                                            new AttackDirection[1], new Vector3[1], new string[1],
                                            new int[trooperLocs[i]]);

                                        swarmingUnitSquad.TakeWeaponDamage(hitinfo, trooperLocs[i],
                                            swarmingUnitSquad.MeleeWeapon, dmg,
                                            0, 0, DamageType.DFASelf);
                                    }
                                }
                            }
                        }
                    }
                    ModState.DeSwarmMovementInfo = new Classes.BA_DeswarmMovementInfo();
                }
            }
        }

        [HarmonyPatch(typeof(CombatHUDMechwarriorTray), "ResetMechwarriorButtons",
                new Type[] {typeof(AbstractActor)})]
        public static class CombatHUDMechwarriorTray_ResetMechwarriorButtons
        {
            [HarmonyPriority(Priority.Last)]
            public static void Postfix(CombatHUDMechwarriorTray __instance, AbstractActor actor)
            {
                if (UnityGameInstance.BattleTechGame.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                if (actor == null) return;

                var moraleButtons = Traverse.Create(__instance).Property("MoraleButtons")
                    .GetValue<CombatHUDActionButton[]>();
                var abilityButtons = Traverse.Create(__instance).Property("AbilityButtons")
                    .GetValue<CombatHUDActionButton[]>();


                if (actor.IsAirlifted())
                {
                    ModInit.modLog?.Trace?.Write(
                        $"[CombatHUDMechwarriorTray.ResetMechwarriorButtons] Actor {actor.DisplayName} {actor.GUID} is Airlifted. Disabling movement buttons.");
                    __instance.MoveButton.DisableButton();
                    __instance.SprintButton.DisableButton();
                    __instance.JumpButton.DisableButton();
                    foreach (var moraleButton in moraleButtons)
                    {
                        moraleButton.DisableButton();
                    }
                    if (ModState.AirliftTrackers[actor.GUID].IsCarriedInternal)
                    {
                        __instance.FireButton.DisableButton();
                    }
                }

                if (actor.isGarrisoned())
                {
                    ModInit.modLog?.Trace?.Write(
                        $"[CombatHUDMechwarriorTray.ResetMechwarriorButtons] Actor {actor.DisplayName} {actor.GUID} found in garrison. Disabling buttons.");
                    
                    __instance.MoveButton.DisableButton();
                    __instance.SprintButton.DisableButton();
                    __instance.JumpButton.DisableButton();
                }

                else if (actor.IsMountedUnit())
                {
                    ModInit.modLog?.Trace?.Write(
                        $"[CombatHUDMechwarriorTray.ResetMechwarriorButtons] Actor {actor.DisplayName} {actor.GUID} found in PositionLockMount. Disabling buttons.");
                    var carrier = actor.Combat.FindActorByGUID(ModState.PositionLockMount[actor.GUID]);
                    
                    __instance.MoveButton.DisableButton();
                    __instance.SprintButton.DisableButton();
                    __instance.JumpButton.DisableButton();

                    if (!actor.IsMountedInternal() || !carrier.hasFiringPorts())
                    {
                        __instance.FireButton.DisableButton();
                        foreach (var moraleButton in moraleButtons)
                        {
                            moraleButton.DisableButton();
                        }
                        foreach (var abilityButton in abilityButtons)
                        {
                            if (abilityButton?.Ability?.Def?.Id == ModInit.modSettings.BattleArmorMountAndSwarmID)
                                abilityButton?.DisableButton();
                        }
                    }
                }
                else if (actor.IsSwarmingUnit())
                {
                    ModInit.modLog?.Trace?.Write(
                        $"[CombatHUDMechwarriorTray.ResetMechwarriorButtons] Actor {actor.DisplayName} {actor.GUID} found in PositionLockSwarm. Disabling buttons.");
                    __instance.FireButton.DisableButton();
                    __instance.MoveButton.DisableButton();
                    __instance.SprintButton.DisableButton();
                    __instance.JumpButton.DisableButton();

                    foreach (var moraleButton in moraleButtons)
                    {
                        moraleButton.DisableButton();
                    }

                    foreach (var abilityButton in abilityButtons)
                    {
                        if (abilityButton?.Ability?.Def?.Id == ModInit.modSettings.BattleArmorMountAndSwarmID)
                            abilityButton?.DisableButton();
                    }
                }
            }
        }

        //patching LOFCache.GetLineOfFire with BA to make sure its not obstructed AND that the carrier isnt obstructed. gonna be messy AF. will also probaly break LowVis.

        

        [HarmonyPatch(typeof(AbstractActor), "HasLOFToTargetUnitAtTargetPosition",
            new Type[] { typeof(ICombatant), typeof(float), typeof(Vector3), typeof(Quaternion), typeof(Vector3), typeof(Quaternion), typeof(bool) })]
        public static class AbstractActor_HasLOFToTargetUnitAtTargetPosition_Patch
        {
            static bool Prepare() => true; //disabled for now. why?
            // make sure units doing swarming or riding cannot be targeted.
            public static void Postfix(AbstractActor __instance, ICombatant targetUnit, float maxRange, Vector3 attackPosition, Quaternion attackRotation, Vector3 targetPosition, Quaternion targetRotation, bool isIndirectFireCapable, ref bool __result)
            {
                if (targetUnit is AbstractActor targetActor)
                {
                    if (targetActor.IsSwarmingUnit() || targetActor.IsMountedUnit() ) //|| targetActor.isGarrisoned()) TODO make untargetable again?
                    {
//                        ModInit.modLog?.Trace?.Write($"[AbstractActor.HasLOFToTargetUnitAtTargetPosition] {targetActor.DisplayName} is swarming or mounted, preventing LOS.");
                        __result = false;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(AbstractActor), "HasIndirectLOFToTargetUnit",
            new Type[] { typeof(Vector3), typeof(Quaternion), typeof(ICombatant), typeof(bool) })]
        public static class AbstractActor_HasIndirectLOFToTargetUnit_Patch
        {
            public static void Postfix(AbstractActor __instance, Vector3 attackPosition, Quaternion attackRotation, ICombatant targetUnit, bool enabledWeaponsOnly, ref bool __result)
            {
                if (targetUnit is AbstractActor targetActor)
                {
//                    if (__instance.IsSwarmingUnit())
//                    {
//                        if (ModState.PositionLockSwarm[__instance.GUID] == targetActor.GUID)
//                        {
//                        ModInit.modLog?.Trace?.Write($"[AbstractActor.HasIndirectLOFToTargetUnit] {__instance.DisplayName} is swarming {targetActor.DisplayName}, forcing direct LOS for weapons");
//                            __result = false;
//                        }
//                    }

                    if (targetActor.IsSwarmingUnit() || targetActor.IsMountedUnit() )//|| targetActor.isGarrisoned()) TODO Make untargetable again?
                    {
                        __result = false;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Weapon), "WillFireAtTargetFromPosition",
            new Type[] {typeof(ICombatant), typeof(Vector3), typeof(Quaternion)})]
        public static class Weapon_WillFireAtTargetFromPosition
        {
            public static void Postfix(Weapon __instance, ICombatant target, Vector3 position, Quaternion rotation, ref bool __result)
            {
                if (__instance.parent.IsSwarmingUnit() && target is AbstractActor targetActor)
                {
                    if (ModState.PositionLockSwarm[__instance.parent.GUID] == targetActor.GUID)
                    {
 //                       ModInit.modLog?.Trace?.Write($"[Weapon.WillFireAtTargetFromPosition] {__instance.parent.DisplayName} is swarming {targetActor.DisplayName}, forcing LOS for weapon {__instance.Name}");
                        __result = true;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CombatHUDButtonBase), "OnClick",
            new Type[] { })]
        public static class CombatHUDButtonBase_OnClick
        {
            static bool Prepare() => true;
            public static void Prefix(CombatHUDButtonBase __instance)
            {
                if (__instance.GUID != "BTN_DoneWithMech") return;
                var hud = Traverse.Create(__instance).Property("HUD").GetValue<CombatHUD>();
                var actor = hud.SelectedActor;
                if (!actor.IsSwarmingUnit())
                {
                    ModInit.modLog?.Debug?.Write($"[CombatHUDButtonBase.OnClick] Actor {actor.DisplayName} is not swarming, ending turn like normal.");
                    return;
                }
                var target = actor.Combat.FindActorByGUID(ModState.PositionLockSwarm[actor.GUID]);
                ModInit.modLog?.Info?.Write($"[CombatHUDButtonBase.OnClick] Actor {actor.DisplayName} has active swarm attack on {target.DisplayName}");

                var weps = actor.Weapons.Where(x => x.IsEnabled && x.HasAmmo).ToList();

                //                var baselineAccuracyModifier = actor.StatCollection.GetValue<float>("AccuracyModifier");
                //                actor.StatCollection.Set<float>("AccuracyModifier", -99999.0f);
                //                ModInit.modLog?.Trace?.Write($"[AbstractActor.DoneWithActor] Actor {actor.DisplayName} getting baselineAccuracyModifer set to {actor.AccuracyModifier}");

                var loc = ModState.BADamageTrackers[actor.GUID].BA_MountedLocations.Values.GetRandomElement();
                var attackStackSequence = new AttackStackSequence(actor, target, actor.CurrentPosition,
                    actor.CurrentRotation, weps, MeleeAttackType.NotSet, loc, -1);
                actor.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(attackStackSequence));

//                actor.StatCollection.Set<float>("AccuracyModifier", baselineAccuracyModifier);
//                ModInit.modLog?.Trace?.Write($"[AbstractActor.DoneWithActor] Actor {actor.DisplayName} resetting baselineAccuracyModifer to {actor.AccuracyModifier}");
                return;
            }
        }


        [HarmonyPatch(typeof(SelectionStateFire), "ProcessClickedCombatant",
            new Type[] {typeof(ICombatant)})]
        public static class SelectionStateFire_ProcessClickedCombatant
        {
            static bool Prepare() => false; //disable for now, try with force-end turn.
            public static void Postfix(SelectionStateFire __instance, ref ICombatant combatant)
            {
                if (__instance.SelectedActor.IsSwarmingUnit())
                {
                    var newTarget =
                        __instance.SelectedActor.Combat.FindActorByGUID(
                            ModState.PositionLockSwarm[__instance.SelectedActor.GUID]);
                    combatant = newTarget;
                }
            }
        }

        [HarmonyPatch(typeof(Mech), "OnLocationDestroyed",
            new Type[] {typeof(ChassisLocations), typeof(Vector3), typeof(WeaponHitInfo), typeof(DamageType)})]
        public static class Mech_OnLocationDestroyed
        {
            public static void Prefix(Mech __instance, ChassisLocations location, Vector3 attackDirection,
                WeaponHitInfo hitInfo, DamageType damageType)
            {
                if (!__instance.HasMountedUnits() && !__instance.HasSwarmingUnits()) return;

                foreach (var squadInfo in ModState.BADamageTrackers.Where(x =>
                    x.Value.TargetGUID == __instance.GUID && !x.Value.IsSquadInternal &&
                    x.Value.BA_MountedLocations.ContainsValue((int)location)))
                {
                    var wereSwarmingUnitsResponsible = squadInfo.Key == hitInfo.attackerId;

                    ModInit.modLog?.Trace?.Write(
                        $"[Mech.OnLocationDestroyed] Evaluating {squadInfo.Key} for {squadInfo.Value.TargetGUID}");
                    if (ModInit.Random.NextDouble() >= (double) 1 / 3 || wereSwarmingUnitsResponsible) continue;
                    if (__instance.Combat.FindActorByGUID(squadInfo.Key) is Mech BattleArmorAsMech)
                    {
                        var BattleArmorMounts =
                            squadInfo.Value.BA_MountedLocations.Where(x => x.Value == (int) location);
                        foreach (var mount in BattleArmorMounts)
                        {
                            var BALocArmor = (ArmorLocation) mount.Key;
                            var BALocStruct = MechStructureRules.GetChassisLocationFromArmorLocation(BALocArmor);
                            BattleArmorAsMech.NukeStructureLocation(hitInfo, 1, BALocStruct, attackDirection,
                                damageType);
                        }
                        BattleArmorAsMech.DismountBA(__instance, Vector3.zero, false, true);
                        BattleArmorAsMech.FlagForDeath("Killed When Mount Died", DeathMethod.VitalComponentDestroyed, DamageType.Melee, 0, -1, __instance.GUID, false);
                        BattleArmorAsMech.HandleDeath(__instance.GUID);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(BattleTech.Building), "FlagForDeath",
            new Type[] {typeof(string), typeof(DeathMethod), typeof(DamageType), typeof(int), typeof(int), typeof(string), typeof(bool)})]
        public static class Building_FlagForDeath
        {
            public static void Prefix(BattleTech.Building __instance, string reason, DeathMethod deathMethod, DamageType damageType, int location, int stackItemID, string attackerID, bool isSilent)
            {
                if (__instance.IsFlaggedForDeath) return;
                ModInit.modLog?.Trace?.Write($"[Building.FlagForDeath] Building {__instance.DisplayName} {__instance.GUID} at position {__instance.CurrentPosition} dieded.");
                if (!__instance.hasGarrisonedUnits()) return;
                var garrisons = new List<KeyValuePair<string, Classes.BA_GarrisonInfo>>(ModState.PositionLockGarrison.Where(x => x.Value.BuildingGUID == __instance.GUID).ToList());
                foreach (var garrison in garrisons)
                {
                    ModInit.modLog?.Trace?.Write($"[Building.FlagForDeath] Building {__instance.DisplayName} {__instance.GUID} dieded, has units mounted.");

                    var actor = __instance.Combat.FindActorByGUID(garrison.Key);
                    var squad = actor as TrooperSquad;

                    //TODO CLUSTER DAMAGE? or just make them take DFA

                    foreach (var garrisonEffect in ModState.OnGarrisonCollapseEffects)
                    {
                        if (garrisonEffect.TargetEffectType == Classes.BA_TargetEffectType.GARRISON)
                        {
                            foreach (var effectData in garrisonEffect.effects)
                            {
                                squad.Combat.EffectManager.CreateEffect(effectData,
                                    effectData.Description.Id,
                                    -1, squad, squad, default(WeaponHitInfo), 1);
                            }
                        }
                    }
                    squad.DismountGarrison(__instance, Vector3.zero, true);


                    var dmg = squad.StatCollection.GetValue<float>("DFASelfDamage");
                    var trooperLocs = squad.GetPossibleHitLocations(squad);
                    for (int i = 0; i < trooperLocs.Count; i++)
                    {
                        var hitinfo = new WeaponHitInfo(-1, -1, 0, 0, squad.GUID,
                            squad.GUID, 1, new float[1], new float[1], new float[1],
                            new bool[1], new int[trooperLocs[i]], new int[1], new AttackImpactQuality[1],
                            new AttackDirection[1], new Vector3[1], new string[1], new int[trooperLocs[i]]);

                        squad.TakeWeaponDamage(hitinfo, trooperLocs[i],
                            squad.MeleeWeapon, dmg,
                            0, 0, DamageType.DFASelf);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(AbstractActor), "HandleDeath",
            new Type[] {typeof(string) })]
        public static class AbstractActor_HandleDeath
        {
            public static void Prefix(AbstractActor __instance, string attackerGUID)
            {
                var dismount = false || (__instance.DeathMethod == DeathMethod.PilotEjection ||
                                         __instance.DeathMethod == DeathMethod.PilotEjectionActorDisabled ||
                                         __instance.DeathMethod == DeathMethod.PilotEjectionNoMessage ||
                                         __instance.DeathMethod == DeathMethod.DespawnedNoMessage ||
                                         __instance.DeathMethod == DeathMethod.DespawnedEscaped);

                if (__instance.HasSwarmingUnits())
                {
                    var swarmingUnits = new List<KeyValuePair<string, string>>(ModState.PositionLockSwarm.Where(x => x.Value == __instance.GUID).ToList());
                    var wereSwarmingUnitsResponsible = swarmingUnits.Any(x => x.Key == attackerGUID);
                    foreach (var swarmingUnit in swarmingUnits)
                    {
                        var actor = __instance.Combat.FindActorByGUID(swarmingUnit.Key);
                        var squad = actor as TrooperSquad;
                        if (ModInit.Random.NextDouble() <= (double)1 / 3 && !wereSwarmingUnitsResponsible && !dismount)
                        {
                            var trooperLocs = squad.GetPossibleHitLocations(__instance);
                            for (int i = 0; i < trooperLocs.Count; i++)
                            {
                                var cLoc = (ChassisLocations)trooperLocs[i];
                                var hitinfo = new WeaponHitInfo(-1, -1, 0, 0, __instance.GUID, squad.GUID, 1, new float[1], new float[1], new float[1], new bool[1], new int[trooperLocs[i]], new int[1], new AttackImpactQuality[1], new AttackDirection[1], new Vector3[1], new string[1], new int[trooperLocs[i]]);
                                squad.NukeStructureLocation(hitinfo, trooperLocs[i], cLoc, Vector3.up, DamageType.ComponentExplosion);
                            }
                            actor.DismountBA(__instance, Vector3.zero, false, true);
                            actor.FlagForDeath("Killed When Mount Died", DeathMethod.VitalComponentDestroyed, DamageType.Melee, 0, -1, __instance.GUID, false);
                            actor.HandleDeath(__instance.GUID);
                            continue;
                        }
                        ModInit.modLog?.Trace?.Write($"[AbstractActor.HandleDeath] Swarmed unit {__instance.DisplayName} destroyed, calling dismount.");
                        actor.DismountBA(__instance, Vector3.zero, false, true);
                    }
                }

                if (__instance.HasMountedUnits())
                {
                    var mountedUnits = new List<KeyValuePair<string,string>>(ModState.PositionLockMount.Where(x => x.Value == __instance.GUID).ToList());
                    foreach (var mountedUnit in mountedUnits)
                    {
                        var actor = __instance.Combat.FindActorByGUID(mountedUnit.Key);
                        var squad = actor as TrooperSquad;
                        if (ModInit.Random.NextDouble() <= (double)1 / 3 && !dismount)
                        {
                            var trooperLocs = squad.GetPossibleHitLocations(__instance);
                            for (int i = 0; i < trooperLocs.Count; i++)
                            {
                                var cLoc = (ChassisLocations)trooperLocs[i];
                                var hitinfo = new WeaponHitInfo(-1, -1, 0, 0, __instance.GUID, squad.GUID, 1, new float[1], new float[1], new float[1], new bool[1], new int[trooperLocs[i]], new int[1], new AttackImpactQuality[1], new AttackDirection[1], new Vector3[1], new string[1], new int[trooperLocs[i]]);
                                squad.NukeStructureLocation(hitinfo, trooperLocs[i], cLoc, Vector3.up, DamageType.ComponentExplosion);
                            }
                            actor.DismountBA(__instance, Vector3.zero, false, true);
                            actor.FlagForDeath("Killed When Mount Died", DeathMethod.VitalComponentDestroyed, DamageType.Melee, 0, -1, __instance.GUID, false);
                            actor.HandleDeath(__instance.GUID);
                            continue;
                        }
                        ModInit.modLog?.Trace?.Write($"[AbstractActor.HandleDeath] Mount {__instance.DisplayName} destroyed, calling dismount.");
                        actor.DismountBA(__instance, Vector3.zero, false, true);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CombatHUDEquipmentSlot), "InitButton",
            new Type[] {typeof(SelectionType), typeof(Ability), typeof(SVGAsset), typeof(string), typeof(string), typeof(AbstractActor) })]
        public static class CombatHUDEquipmentSlot_InitButton
        {
            public static void Postfix(CombatHUDEquipmentSlot __instance, SelectionType SelectionType, Ability Ability, SVGAsset Icon, string GUID, string Tooltip, AbstractActor actor)
            {
                if (actor == null) return;
                if (Ability == null || Ability.Def?.Id != ModInit.modSettings.BattleArmorMountAndSwarmID) return;
                if (actor.isGarrisoned())
                {
                    __instance.Text.SetText("DISMOUNT GARRISON", Array.Empty<object>());
                }
                else if (actor.IsMountedUnit())
                {
                    __instance.Text.SetText("DISMOUNT BATTLEARMOR", Array.Empty<object>());
                }
                else if (actor.IsSwarmingUnit())
                {
                    __instance.Text.SetText("HALT SWARM ATTACK", Array.Empty<object>());
                }
            }
        }

        [HarmonyPatch(typeof(CombatHUDEquipmentSlot), "ActivateAbility",
            new Type[] { typeof(string), typeof(string) })]
        public static class CombatHUDEquipmentSlot_ConfirmAbility
        {
            public static void Postfix(CombatHUDEquipmentSlot __instance, string creatorGUID, string targetGUID)
            {
                var HUD = Traverse.Create(__instance).Property("HUD").GetValue<CombatHUD>();
                var theActor = HUD.SelectedActor;
                if (theActor == null) return;
                if (__instance.Ability == null || __instance.Ability?.Def?.Id != ModInit.modSettings.BattleArmorMountAndSwarmID) return;
                if (theActor.isGarrisoned())
                {
                    __instance.Text.SetText("DISMOUNT GARRISON", Array.Empty<object>());
                }
                if (theActor.IsMountedUnit())
                {
                    __instance.Text.SetText("DISMOUNT BATTLEARMOR", Array.Empty<object>());
                }
                else if (theActor.IsSwarmingUnit())
                {
                    __instance.Text.SetText("HALT SWARM ATTACK", Array.Empty<object>());
                }
                else
                {
                    __instance.Text.SetText(__instance.Ability.Def?.Description.Name);
                }
            }
        }

        [HarmonyPatch(typeof(CombatSelectionHandler), "TrySelectActor",
            new Type[] {typeof(AbstractActor), typeof(bool)})]
        public static class CombatSelectionHandler_TrySelectActor
        {
            public static bool Prefix(CombatSelectionHandler __instance, AbstractActor actor, bool manualSelection,
                ref bool __result)
            {
                if (actor == null)
                {
                    return true;
                }

                if (!actor.Combat.LocalPlayerTeam.IsActive)
                {
                    return true;
                }

                if (actor.HasBegunActivation && !actor.HasActivatedThisRound)
                {
                    return true;
                }

                if (actor.IsMountedUnit())
                {
                    var carrier = actor.Combat.FindActorByGUID(ModState.PositionLockMount[actor.GUID]);
                    if (!carrier.hasFiringPorts())
                    {
                        ModInit.modLog?.Trace?.Write($"[CombatSelectionHandler.TrySelectActor] {actor.DisplayName} is mounted, does not have firing ports.");
                        return true;
                    }
                    ModInit.modLog?.Trace?.Write($"[CombatSelectionHandler.TrySelectActor] {actor.DisplayName} is mounted, DOES HAVE firing ports.");
                }

                if (!actor.isGarrisoned() && !actor.IsMountedUnit())
                {
                    ModInit.modLog?.Trace?.Write($"[CombatSelectionHandler.TrySelectActor] {actor.DisplayName}is not mounted or garrisoned.");
                    return true;
                }

                if (__instance.ActiveState is SelectionStateFire) return true;


                if ((__instance.SelectedActor == null || __instance.IsSelectedActorChangeable) &&
                    actor.team == actor.Combat.LocalPlayerTeam && actor != __instance.SelectedActor)
                {
                    if (__instance.IsCommandButtonActive)
                    {
                        __instance.UninvokeCommandTray();
                    }
                    else
                    {
                        Traverse.Create(__instance).Method("ClearSelectionStack").GetValue();
                        //__instance.ClearSelectionStack();
                    }

                    __instance.ActivatedAbilityButtons.Clear();
                    actor.Combat.MessageCenter.PublishMessage(new ActorSelectedMessage(actor.GUID));
                    if (manualSelection)
                    {
                        AudioEventManager.PlayPilotVO(VOEvents.Mech_Chosen, actor, null, null, true);
                    }
                    var SelectionStack = Traverse.Create(__instance).Property("SelectionStack").GetValue<List<SelectionState>>();
                    if (!SelectionStack.Any(x => x is SelectionStateDoneWithMech))
                    {
                        var HUD = Traverse.Create(__instance).Property("HUD").GetValue<CombatHUD>();
                        var doneState = new SelectionStateDoneWithMech(actor.Combat, HUD,
                            HUD.MechWarriorTray.DoneWithMechButton, actor);
                        var addState = Traverse.Create(__instance)
                            .Method("addNewState", new Type[] { typeof(SelectionState) });
                        addState.GetValue(doneState);
                    }
                    __instance.AddFireState(actor);
                    ModInit.modLog?.Trace?.Write($"[CombatSelectionHandler.TrySelectActor] {actor.DisplayName} should be adding fire state.");
                    if (ActiveOrDefaultSettings.CloudSettings.autoCenterOnSelection)
                    {
                        CameraControl.Instance.SetMovingToGroundPos(actor.CurrentPosition, 0.95f);
                    }
                    Traverse.Create(__instance).Method("logSelectionStack").GetValue();
                    //__instance.logSelectionStack();
                    __result = true;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(CombatSelectionHandler), "AddSprintState",
            new Type[] {typeof(AbstractActor)})]
        public static class CombatSelectionHandler_AddSprintState
        {
            public static bool Prefix(CombatSelectionHandler __instance, AbstractActor actor)
            {
                if (actor.IsMountedUnit() || actor.IsSwarmingUnit() || actor.IsAirlifted() || actor.isGarrisoned())
                {
                    ModInit.modLog?.Trace?.Write($"[CombatSelectionHandler.AddSprintState] Actor {actor.DisplayName}: Disabling SprintState");
                    var SelectionStack = Traverse.Create(__instance).Property("SelectionStack").GetValue<List<SelectionState>>();
                    if (!SelectionStack.Any(x => x is SelectionStateDoneWithMech))
                    {
                        var HUD = Traverse.Create(__instance).Property("HUD").GetValue<CombatHUD>();
                        var doneState = new SelectionStateDoneWithMech(actor.Combat, HUD,
                            HUD.MechWarriorTray.DoneWithMechButton, actor);
                        var addState = Traverse.Create(__instance)
                            .Method("addNewState", new Type[] {typeof(SelectionState)});
                        addState.GetValue(doneState);
                    }
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(CombatSelectionHandler), "AddMoveState",
            new Type[] { typeof(AbstractActor) })]
        public static class CombatSelectionHandler_AddMoveState
        {
            public static bool Prefix(CombatSelectionHandler __instance, AbstractActor actor)
            {
                if (actor.IsSwarmingUnit() || actor.IsMountedUnit() || actor.IsAirlifted() || actor.isGarrisoned())
                {
                    ModInit.modLog?.Trace?.Write($"[CombatSelectionHandler.AddMoveState] Actor {actor.DisplayName}: Disabling AddMoveState");
                    var SelectionStack = Traverse.Create(__instance).Property("SelectionStack").GetValue<List<SelectionState>>();
                    if (!SelectionStack.Any(x => x is SelectionStateDoneWithMech))
                    {
                        var HUD = Traverse.Create(__instance).Property("HUD").GetValue<CombatHUD>();
                        var doneState = new SelectionStateDoneWithMech(actor.Combat, HUD,
                            HUD.MechWarriorTray.DoneWithMechButton, actor);
                        var addState = Traverse.Create(__instance)
                            .Method("addNewState", new Type[] { typeof(SelectionState) });
                        addState.GetValue(doneState);
                    }
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Mech), "DamageLocation",
            new Type[] {typeof(int), typeof(WeaponHitInfo), typeof(ArmorLocation), typeof(Weapon), typeof(float), typeof(float), typeof(int), typeof(AttackImpactQuality), typeof(DamageType)})]
        public static class Mech_DamageLocation_Patch
        {
            public static void Prefix(Mech __instance, int originalHitLoc, WeaponHitInfo hitInfo, ArmorLocation aLoc, Weapon weapon, ref float totalArmorDamage, ref float directStructureDamage, int hitIndex, AttackImpactQuality impactQuality, DamageType damageType)
            {
                if (__instance is TrooperSquad squad)
                {
                    if (squad.isGarrisoned())
                    {
                        var bldgCombatant = squad.Combat.FindCombatantByGUID(ModState.PositionLockGarrison[squad.GUID].BuildingGUID);
                        if (bldgCombatant is BattleTech.Building building)
                        {
                            ModInit.modLog?.Trace?.Write($"[Mech.DamageLocation] Redirecting {totalArmorDamage} armor and {directStructureDamage} structure damaghe from squad {squad.DisplayName} {squad.GUID} to garrisoned building {bldgCombatant.DisplayName}");
                            building.TakeWeaponDamage(hitInfo, 1, weapon, totalArmorDamage, directStructureDamage, hitIndex, damageType);
                            totalArmorDamage = 0f;
                            directStructureDamage = 0f;
                        }
                    }
                }

                if (!__instance.HasMountedUnits() && !__instance.HasSwarmingUnits()) return;

                foreach (var squadInfo in ModState.BADamageTrackers.Where(x => x.Value.TargetGUID == __instance.GUID && !x.Value.IsSquadInternal && x.Value.BA_MountedLocations.ContainsValue((int)aLoc)))
                {
                    ModInit.modLog?.Trace?.Write($"[Mech.DamageLocation] Evaluating {squadInfo.Key} for {squadInfo.Value.TargetGUID}");
                    if (ModInit.Random.NextDouble() > (double)1 / 3) continue;
                    if (__instance.Combat.FindActorByGUID(squadInfo.Key) is Mech BattleArmorAsMech)
                    {
                        if (BattleArmorAsMech.GUID == hitInfo.attackerId) continue;
                        var BattleArmorMounts = squadInfo.Value.BA_MountedLocations.Where(x => x.Value == (int) aLoc);
                        foreach (var mount in BattleArmorMounts)
                        {
                            var BALocArmor = (ArmorLocation) mount.Key;
                            //var BALocArmorString = BattleArmorAsMech.GetStringForArmorLocation(BALocArmor);
                            var BALocStruct = MechStructureRules.GetChassisLocationFromArmorLocation(BALocArmor);
                            //var BALocStructString = BattleArmorAsMech.GetStringForStructureLocation(BALocStruct);

                            var BattleArmorLocArmor = BattleArmorAsMech.ArmorForLocation((int) BALocArmor);
                            var BattleArmorLocStruct = BattleArmorAsMech.StructureForLocation((int) BALocStruct);

                            if (directStructureDamage > 0)
                            {
                                ModInit.modLog?.Info?.Write(
                                    $"[Mech.DamageLocation] directStructureDamage: {directStructureDamage}");
                                var directStructureDiff = directStructureDamage - BattleArmorLocStruct;
                                if (directStructureDiff >= 0)
                                {
                                    directStructureDamage -= BattleArmorLocStruct;
                                    ModInit.modLog?.Info?.Write(
                                        $"[Mech.DamageLocation] directStructureDamage Diff: {directStructureDiff}. Mech directStructureDamage decremented to {directStructureDamage}");
                                    BattleArmorAsMech.TakeWeaponDamage(hitInfo, (int) BALocArmor, weapon, 0,
                                        BattleArmorLocStruct, hitIndex, damageType);
                                    ModInit.modLog?.Info?.Write(
                                        $"[Mech.DamageLocation] Battle Armor at location {BALocArmor} takes {BattleArmorLocStruct} direct structure damage");
                                    __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(BattleArmorAsMech, Strings.T("Battle Armor Damaged!"), FloatieMessage.MessageNature.CriticalHit, false)));
                                    continue;
                                }
                                
                                else if (directStructureDiff < 0)
                                {
                                    ModInit.modLog?.Info?.Write(
                                        $"[Mech.DamageLocation] directStructureDamage Diff: {directStructureDiff}. Mech directStructureDamage decremented to 0");
                                    BattleArmorAsMech.TakeWeaponDamage(hitInfo, (int) BALocArmor, weapon, 0,
                                        Mathf.Abs(directStructureDamage), hitIndex, damageType);
                                    ModInit.modLog?.Info?.Write(
                                        $"[Mech.DamageLocation] Battle Armor at location {BALocArmor} takes {directStructureDamage} direct structure damage");
                                    __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(BattleArmorAsMech, Strings.T("Battle Armor Damaged!"), FloatieMessage.MessageNature.CriticalHit, false)));
                                    directStructureDamage = 0;
                                }
                            }

                            if (totalArmorDamage > 0)
                            {
                                ModInit.modLog?.Info?.Write(
                                    $"[Mech.DamageLocation] totalArmorDamage: {totalArmorDamage}");
                                var totalArmorDamageDiff =
                                    totalArmorDamage - (BattleArmorLocArmor + BattleArmorLocStruct);
                                if (totalArmorDamageDiff > 0)
                                {
                                    totalArmorDamage -= totalArmorDamageDiff;
                                    ModInit.modLog?.Info?.Write(
                                        $"[Mech.DamageLocation] totalArmorDamageDiff Diff: {totalArmorDamageDiff}. Mech totalArmorDamage decremented to {totalArmorDamage}");
                                    BattleArmorAsMech.TakeWeaponDamage(hitInfo, (int) BALocArmor, weapon,
                                        Mathf.Abs(totalArmorDamageDiff), 0, hitIndex, damageType);
                                    ModInit.modLog?.Info?.Write(
                                        $"[Mech.DamageLocation] Battle Armor at location {BALocArmor} takes {BattleArmorLocArmor} damage");
                                    __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(BattleArmorAsMech, Strings.T("Battle Armor Damaged!"), FloatieMessage.MessageNature.CriticalHit, false)));
                                }

                                else if (totalArmorDamageDiff <= 0)
                                {
                                    ModInit.modLog?.Info?.Write(
                                        $"[Mech.DamageLocation] totalArmorDamageDiff Diff: {totalArmorDamageDiff}. Mech totalArmorDamage decremented to 0");
                                    BattleArmorAsMech.TakeWeaponDamage(hitInfo, (int) BALocArmor, weapon,
                                        Mathf.Abs(totalArmorDamage), 0, hitIndex, damageType);
                                    ModInit.modLog?.Info?.Write(
                                        $"[Mech.DamageLocation] Battle Armor at location {BALocArmor} takes {totalArmorDamage} damage");
                                    totalArmorDamage = 0;
                                    __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(BattleArmorAsMech, Strings.T("Battle Armor Damaged!"), FloatieMessage.MessageNature.CriticalHit, false)));
                                }
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Vehicle), "DamageLocation",
            new Type[] { typeof(WeaponHitInfo), typeof(int), typeof(VehicleChassisLocations), typeof(Weapon), typeof(float), typeof(float), typeof(AttackImpactQuality) })]
        public static class Vehicle_DamageLocation_Patch
        {
            public static void Prefix(Vehicle __instance, WeaponHitInfo hitInfo, int originalHitLoc, VehicleChassisLocations vLoc, Weapon weapon, ref float totalArmorDamage, ref float directStructureDamage, AttackImpactQuality impactQuality)
            {
                if (!__instance.HasMountedUnits() && !__instance.HasSwarmingUnits()) return;

                foreach (var squadInfo in ModState.BADamageTrackers.Where(x => x.Value.TargetGUID == __instance.GUID && !x.Value.IsSquadInternal && x.Value.BA_MountedLocations.ContainsValue((int)vLoc)))
                {
                    ModInit.modLog?.Trace?.Write($"[Vehicle.DamageLocation] Evaluating {squadInfo.Key} for {squadInfo.Value.TargetGUID}");
                    if (ModInit.Random.NextDouble() > (double)1 / 3) continue;
                    if (__instance.Combat.FindActorByGUID(squadInfo.Key) is Mech BattleArmorAsMech)
                    {
                        if (BattleArmorAsMech.GUID == hitInfo.attackerId) continue;
                        var BattleArmorMounts = squadInfo.Value.BA_MountedLocations.Where(x => x.Value == (int)vLoc);
                        foreach (var mount in BattleArmorMounts)
                        {
                            var BALocArmor = (ArmorLocation)mount.Key;
                            //var BALocArmorString = BattleArmorAsMech.GetStringForArmorLocation(BALocArmor);
                            var BALocStruct = MechStructureRules.GetChassisLocationFromArmorLocation(BALocArmor);
                            //var BALocStructString = BattleArmorAsMech.GetStringForStructureLocation(BALocStruct);

                            var BattleArmorLocArmor = BattleArmorAsMech.ArmorForLocation((int) BALocArmor);
                            var BattleArmorLocStruct = BattleArmorAsMech.StructureForLocation((int) BALocStruct);

                            if (directStructureDamage > 0)
                            {
                                ModInit.modLog?.Info?.Write(
                                    $"[Vehicle.DamageLocation] directStructureDamage: {directStructureDamage}");
                                var directStructureDiff = directStructureDamage - BattleArmorLocStruct;
                                if (directStructureDiff >= 0)
                                {
                                    directStructureDamage -= BattleArmorLocStruct;
                                    ModInit.modLog?.Info?.Write(
                                        $"[Vehicle.DamageLocation] directStructureDamage Diff: {directStructureDiff}. Vehicle directStructureDamage decremented to {directStructureDamage}");
                                    BattleArmorAsMech.TakeWeaponDamage(hitInfo, (int)BALocArmor, weapon, 0,
                                        BattleArmorLocStruct, 1, DamageType.Combat);
                                    ModInit.modLog?.Info?.Write(
                                        $"[Vehicle.DamageLocation] Battle Armor at location {BALocArmor} takes {BattleArmorLocStruct} direct structure damage");
                                    __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(BattleArmorAsMech, Strings.T("Battle Armor Damaged!"), FloatieMessage.MessageNature.CriticalHit, false)));
                                    continue;
                                }

                                else if (directStructureDiff < 0)
                                {
                                    ModInit.modLog?.Info?.Write(
                                        $"[Vehicle.DamageLocation] directStructureDamage Diff: {directStructureDiff}. Vehicle directStructureDamage decremented to 0");
                                    BattleArmorAsMech.TakeWeaponDamage(hitInfo, (int)BALocArmor, weapon, 0,
                                        Mathf.Abs(directStructureDamage), 1, DamageType.Combat);
                                    ModInit.modLog?.Info?.Write(
                                        $"[Vehicle.DamageLocation] Battle Armor at location {BALocArmor} takes {directStructureDamage} direct structure damage");
                                    directStructureDamage = 0;
                                    __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(BattleArmorAsMech, Strings.T("Battle Armor Damaged!"), FloatieMessage.MessageNature.CriticalHit, false)));
                                }
                            }

                            if (totalArmorDamage > 0)
                            {
                                ModInit.modLog?.Info?.Write(
                                    $"[Vehicle.DamageLocation] totalArmorDamage: {totalArmorDamage}");
                                var totalArmorDamageDiff =
                                    totalArmorDamage - (BattleArmorLocArmor + BattleArmorLocStruct);
                                if (totalArmorDamageDiff > 0)
                                {
                                    totalArmorDamage -= totalArmorDamageDiff;
                                    ModInit.modLog?.Info?.Write(
                                        $"[Vehicle.DamageLocation] totalArmorDamageDiff Diff: {totalArmorDamageDiff}. Vehicle totalArmorDamage decremented to {totalArmorDamage}");
                                    BattleArmorAsMech.TakeWeaponDamage(hitInfo, (int)BALocArmor, weapon,
                                        Mathf.Abs(totalArmorDamageDiff), 0, 1, DamageType.Combat);
                                    ModInit.modLog?.Info?.Write(
                                        $"[Vehicle.DamageLocation] Battle Armor at location {BALocArmor} takes {BattleArmorLocArmor} damage");
                                    __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(BattleArmorAsMech, Strings.T("Battle Armor Damaged!"), FloatieMessage.MessageNature.CriticalHit, false)));
                                }

                                else if (totalArmorDamageDiff <= 0)
                                {
                                    ModInit.modLog?.Info?.Write(
                                        $"[Vehicle.DamageLocation] totalArmorDamageDiff Diff: {totalArmorDamageDiff}. Vehicle totalArmorDamage decremented to 0");
                                    BattleArmorAsMech.TakeWeaponDamage(hitInfo, (int)BALocArmor, weapon,
                                        Mathf.Abs(totalArmorDamage), 0, 1, DamageType.Combat);
                                    ModInit.modLog?.Info?.Write(
                                        $"[Vehicle.DamageLocation] Battle Armor at location {BALocArmor} takes {totalArmorDamage} damage");
                                    totalArmorDamage = 0;
                                    __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(BattleArmorAsMech, Strings.T("Battle Armor Damaged!"), FloatieMessage.MessageNature.CriticalHit, false)));
                                }
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Turret), "DamageLocation",
            new Type[] { typeof(WeaponHitInfo), typeof(BuildingLocation), typeof(Weapon), typeof(float), typeof(float) })]
        public static class Turret_DamageLocation_Patch
        {
            public static void Prefix(Turret __instance, WeaponHitInfo hitInfo, BuildingLocation bLoc, Weapon weapon, ref float totalArmorDamage, ref float directStructureDamage)
            {
                if (bLoc == BuildingLocation.None || bLoc == BuildingLocation.Invalid)
                {
                    return;
                }

                if (!__instance.HasMountedUnits() && !__instance.HasSwarmingUnits()) return;

                foreach (var squadInfo in ModState.BADamageTrackers.Where(x => x.Value.TargetGUID == __instance.GUID && !x.Value.IsSquadInternal && x.Value.BA_MountedLocations.ContainsValue((int)bLoc)))
                {
                    ModInit.modLog?.Trace?.Write($"[Turret.DamageLocation] Evaluating {squadInfo.Key} for {squadInfo.Value.TargetGUID}");
                    if (ModInit.Random.NextDouble() > (double)1 / 3) continue;
                    if (__instance.Combat.FindActorByGUID(squadInfo.Key) is Mech BattleArmorAsMech)
                    {
                        if (BattleArmorAsMech.GUID == hitInfo.attackerId) continue;
                        var BattleArmorMounts = squadInfo.Value.BA_MountedLocations.Where(x => x.Value == (int)bLoc);
                        foreach (var mount in BattleArmorMounts)
                        {
                            var BALocArmor = (ArmorLocation)mount.Key;
                            //var BALocArmorString = BattleArmorAsMech.GetStringForArmorLocation(BALocArmor);
                            var BALocStruct = MechStructureRules.GetChassisLocationFromArmorLocation(BALocArmor);
                            //var BALocStructString = BattleArmorAsMech.GetStringForStructureLocation(BALocStruct);

                            var BattleArmorLocArmor = BattleArmorAsMech.ArmorForLocation((int)BALocArmor);
                            var BattleArmorLocStruct = BattleArmorAsMech.StructureForLocation((int)BALocStruct);

                            if (directStructureDamage > 0)
                            {
                                ModInit.modLog?.Info?.Write(
                                    $"[Turret.DamageLocation] directStructureDamage: {directStructureDamage}");
                                var directStructureDiff = directStructureDamage - BattleArmorLocStruct;
                                if (directStructureDiff >= 0)
                                {
                                    directStructureDamage -= BattleArmorLocStruct;
                                    ModInit.modLog?.Info?.Write(
                                        $"[Turret.DamageLocation] directStructureDamage Diff: {directStructureDiff}. Turret directStructureDamage decremented to {directStructureDamage}");
                                    BattleArmorAsMech.TakeWeaponDamage(hitInfo, (int) BALocArmor, weapon, 0,
                                        BattleArmorLocStruct, 1, DamageType.Combat);
                                    ModInit.modLog?.Info?.Write(
                                        $"[Turret.DamageLocation] Battle Armor at location {BALocArmor} takes {BattleArmorLocStruct} direct structure damage");
                                    __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(BattleArmorAsMech, Strings.T("Battle Armor Damaged!"), FloatieMessage.MessageNature.CriticalHit, false)));
                                    continue;
                                }

                                else if (directStructureDiff < 0)
                                {
                                    ModInit.modLog?.Info?.Write(
                                        $"[Turret.DamageLocation] directStructureDamage Diff: {directStructureDiff}. Turret directStructureDamage decremented to 0");
                                    BattleArmorAsMech.TakeWeaponDamage(hitInfo, (int) BALocArmor, weapon, 0,
                                        Mathf.Abs(directStructureDamage), 1, DamageType.Combat);
                                    ModInit.modLog?.Info?.Write(
                                        $"[Turret.DamageLocation] Battle Armor at location {BALocArmor} takes {directStructureDamage} direct structure damage");
                                    directStructureDamage = 0;
                                    __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(BattleArmorAsMech, Strings.T("Battle Armor Damaged!"), FloatieMessage.MessageNature.CriticalHit, false)));
                                }
                            }

                            if (totalArmorDamage > 0)
                            {
                                ModInit.modLog?.Info?.Write(
                                    $"[Turret.DamageLocation] totalArmorDamage: {totalArmorDamage}");
                                var totalArmorDamageDiff =
                                    totalArmorDamage - (BattleArmorLocArmor + BattleArmorLocStruct);
                                if (totalArmorDamageDiff > 0)
                                {
                                    totalArmorDamage -= totalArmorDamageDiff;
                                    ModInit.modLog?.Info?.Write(
                                        $"[Turret.DamageLocation] totalArmorDamageDiff Diff: {totalArmorDamageDiff}. Turret totalArmorDamage decremented to {totalArmorDamage}");
                                    BattleArmorAsMech.TakeWeaponDamage(hitInfo, (int)BALocArmor, weapon,
                                        Mathf.Abs(totalArmorDamageDiff), 0, 1, DamageType.Combat);
                                    ModInit.modLog?.Info?.Write(
                                        $"[Turret.DamageLocation] Battle Armor at location {BALocArmor} takes {BattleArmorLocArmor} damage");
                                    __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(BattleArmorAsMech, Strings.T("Battle Armor Damaged!"), FloatieMessage.MessageNature.CriticalHit, false)));
                                }

                                else if (totalArmorDamageDiff <= 0)
                                {
                                    ModInit.modLog?.Info?.Write(
                                        $"[Turret.DamageLocation] totalArmorDamageDiff Diff: {totalArmorDamageDiff}. Turret totalArmorDamage decremented to 0");
                                    BattleArmorAsMech.TakeWeaponDamage(hitInfo, (int)BALocArmor, weapon,
                                        Mathf.Abs(totalArmorDamage), 0, 1, DamageType.Combat);
                                    ModInit.modLog?.Info?.Write(
                                        $"[Turret.DamageLocation] Battle Armor at location {BALocArmor} takes {totalArmorDamage} damage");
                                    totalArmorDamage = 0;
                                    __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(new ShowActorInfoSequence(BattleArmorAsMech, Strings.T("Battle Armor Damaged!"), FloatieMessage.MessageNature.CriticalHit, false)));
                                }
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CombatHUDMechTrayArmorHover), "setToolTipInfo",
            new Type[] {typeof(Mech), typeof(ArmorLocation)})]
        public static class CombatHUDMechTrayArmorHover_setToolTipInfo
        {
            public static void Postfix(CombatHUDMechTrayArmorHover __instance, Mech mech, ArmorLocation location)
            {
                if (!mech.HasSwarmingUnits() && !mech.HasMountedUnits()) return;
                var tooltip = Traverse.Create(__instance).Property("ToolTip").GetValue<CombatHUDTooltipHoverElement>();
                foreach (var squadInfo in ModState.BADamageTrackers.Where(x =>
                    x.Value.TargetGUID == mech.GUID && !x.Value.IsSquadInternal &&
                    x.Value.BA_MountedLocations.ContainsValue((int)location)))
                {
                    ModInit.modLog?.Trace?.Write(
                        $"[CombatHUDMechTrayArmorHover.setToolTipInfo] Evaluating {squadInfo.Key} for {squadInfo.Value.TargetGUID} for tooltip infos");
                    
                    if (mech.Combat.FindActorByGUID(squadInfo.Key) is Mech BattleArmorAsMech)
                    {
                        var BattleArmorMounts = squadInfo.Value.BA_MountedLocations.Where(x => x.Value == (int) location);
                        foreach (var mount in BattleArmorMounts)
                        {

                            var BALocArmor = (ArmorLocation)mount.Key;
                            //var BALocArmorString = BattleArmorAsMech.GetStringForArmorLocation(BALocArmor);
                            var BALocStruct = MechStructureRules.GetChassisLocationFromArmorLocation(BALocArmor);
                            //var BALocStructString = BattleArmorAsMech.GetStringForStructureLocation(BALocStruct);

                            var BattleArmorLocArmor = BattleArmorAsMech.ArmorForLocation((int)BALocArmor);
                            var BattleArmorLocStruct = BattleArmorAsMech.StructureForLocation((int)BALocStruct);
                            var newText =
                                new Localize.Text(
                                    $"Battle Armor: Arm. {Mathf.RoundToInt(BattleArmorLocArmor)} / Str. {Mathf.RoundToInt(BattleArmorLocStruct)}",
                                    Array.Empty<object>());
                            if (mech.team.IsFriendly(BattleArmorAsMech.team))
                            {
                                tooltip.BuffStrings.Add(newText);
                            }
                            else
                            {
                                tooltip.DebuffStrings.Add(newText);
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CombatHUDVehicleArmorHover), "setToolTipInfo",
            new Type[] { typeof(Vehicle), typeof(VehicleChassisLocations) })]
        public static class CombatHUDVehicleArmorHover_setToolTipInfo
        {
            public static void Postfix(CombatHUDVehicleArmorHover __instance, Vehicle vehicle, VehicleChassisLocations location)
            {
                if (!vehicle.HasSwarmingUnits() && !vehicle.HasMountedUnits()) return;
                var tooltip = Traverse.Create(__instance).Property("ToolTip").GetValue<CombatHUDTooltipHoverElement>();
                foreach (var squadInfo in ModState.BADamageTrackers.Where(x =>
                    x.Value.TargetGUID == vehicle.GUID && !x.Value.IsSquadInternal &&
                    x.Value.BA_MountedLocations.ContainsValue((int)location)))
                {
                    ModInit.modLog?.Trace?.Write(
                        $"[CombatHUDMechTrayArmorHover.setToolTipInfo] Evaluating {squadInfo.Key} for {squadInfo.Value.TargetGUID} for tooltip infos");

                    if (vehicle.Combat.FindActorByGUID(squadInfo.Key) is Mech BattleArmorAsMech)
                    {
                        var BattleArmorMounts = squadInfo.Value.BA_MountedLocations.Where(x => x.Value == (int)location);
                        foreach (var mount in BattleArmorMounts)
                        {

                            var BALocArmor = (VehicleChassisLocations)mount.Key;
                            //var BALocArmorString = BattleArmorAsMech.GetStringForArmorLocation(BALocArmor);
                            //var BALocStructString = BattleArmorAsMech.GetStringForStructureLocation(BALocStruct);

                            var BattleArmorLocArmor = BattleArmorAsMech.ArmorForLocation((int)BALocArmor);
                            var BattleArmorLocStruct = BattleArmorAsMech.StructureForLocation((int)BALocArmor);
                            var newText =
                                new Localize.Text(
                                    $"Battle Armor: Arm. {Mathf.RoundToInt(BattleArmorLocArmor)} / Str. {Mathf.RoundToInt(BattleArmorLocStruct)}",
                                    Array.Empty<object>());
                            if (vehicle.team.IsFriendly(BattleArmorAsMech.team))
                            {
                                tooltip.BuffStrings.Add(newText);
                            }
                            else
                            {
                                tooltip.DebuffStrings.Add(newText);
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(LineOfSight), "GetLineOfFireUncached")]
        public static class LineOfSight_GetLineOfFireUncached
        {
            public static bool Prefix(LineOfSight __instance, AbstractActor source, Vector3 sourcePosition, ICombatant target, Vector3 targetPosition, Quaternion targetRotation, out Vector3 collisionWorldPos, ref LineOfFireLevel __result)
            {
                collisionWorldPos = new Vector3();

                if (target is BattleTech.Building building && !building.hasGarrisonedUnits()) return true;

                if (target is AbstractActor actorTarget)
                {
                    if (actorTarget.IsSwarmingUnit() || actorTarget.IsMountedUnit() )//|| actorTarget.isGarrisoned()) TODO Make untargetable again?
                    {
                        __result = LineOfFireLevel.NotSet; // added 1/11 to block all LOF to swarming/mounted units. NotSet, or should it be LOS.Blocked?
                        return false;
                    }

                    if (!actorTarget.HasSwarmingUnits() && !actorTarget.HasMountedUnits())
                    {
                        return true;
                    }
                }

                Vector3 forward = targetPosition - sourcePosition;
                forward.y = 0f;
                Quaternion rotation = Quaternion.LookRotation(forward);
                Vector3[] lossourcePositions = source.GetLOSSourcePositions(sourcePosition, rotation);
                Vector3[] lostargetPositions = target.GetLOSTargetPositions(targetPosition, targetRotation);


                List<AbstractActor> list = new List<AbstractActor>(source.Combat.AllActors);
                list.Remove(source);

                var unitGUIDs = new List<string>(ModState.PositionLockSwarm.Keys);
                unitGUIDs.AddRange(ModState.PositionLockMount.Keys);
                unitGUIDs.AddRange(ModState.PositionLockGarrison.Keys);
                foreach (var actorGUID in unitGUIDs)
                {
                    list.Remove(source.Combat.FindActorByGUID(actorGUID));
                }

                AbstractActor actorTarget2 = target as AbstractActor;
                string text = null;
                if (actorTarget2 != null)
                {
                    list.Remove(actorTarget2);
                }
                else
                {
                    text = target.GUID;
                }

                if (source.IsMountedUnit())
                {
                    var carrier = source.Combat.FindActorByGUID(ModState.PositionLockMount[source.GUID]);
                    if (carrier.hasFiringPorts())
                    {
                        list.Remove(source.Combat.FindActorByGUID(ModState.PositionLockMount[source.GUID])); // remove mound from LOS blocking (i have no idea if this will work or is even needed)
                    }
                }

                if (source.IsAirlifted())
                {
                    if (!ModState.AirliftTrackers[source.GUID].IsCarriedInternal)
                    {
                        list.Remove(source.Combat.FindActorByGUID(ModState.AirliftTrackers[source.GUID].CarrierGUID));
                    }
                }

                LineSegment lineSegment = new LineSegment(sourcePosition, targetPosition);
                list.Sort((AbstractActor x, AbstractActor y) => Vector3.SqrMagnitude(x.CurrentPosition - sourcePosition).CompareTo(Vector3.SqrMagnitude(y.CurrentPosition - sourcePosition)));
                float num = Vector3.SqrMagnitude(sourcePosition - targetPosition);
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i].IsDead || Vector3.SqrMagnitude(list[i].CurrentPosition - sourcePosition) > num || lineSegment.DistToPoint(list[i].CurrentPosition) > list[i].Radius * 5f)
                    {
                        list.RemoveAt(i);
                    }
                }
                float num2 = 0f;
                float num3 = 0f;
                float num4 = 0f;
                collisionWorldPos = targetPosition;
                float num5 = 999999.9f;
                Weapon longestRangeWeapon = source.GetLongestRangeWeapon(false, false);
                float num6 = (longestRangeWeapon == null) ? 0f : longestRangeWeapon.MaxRange;
                float adjustedSpotterRange = source.Combat.LOS.GetAdjustedSpotterRange(source, actorTarget2);
                num6 = Mathf.Max(num6, adjustedSpotterRange);
                float num7 = Mathf.Pow(num6, 2f);
                for (int j = 0; j < lossourcePositions.Length; j++)
                {
                    for (int k = 0; k < lostargetPositions.Length; k++)
                    {
                        num3 += 1f;
                        if (Vector3.SqrMagnitude(lossourcePositions[j] - lostargetPositions[k]) <= num7)
                        {
                            lineSegment = new LineSegment(lossourcePositions[j], lostargetPositions[k]);
                            bool flag = false;
                            Vector3 vector;
                            if (text == null)
                            {
                                for (int l = 0; l < list.Count; l++)
                                {
                                    if (lineSegment.DistToPoint(list[l].CurrentPosition) < list[l].Radius)
                                    {
                                        vector = NvMath.NearestPointStrict(lossourcePositions[j], lostargetPositions[k], list[l].CurrentPosition);
                                        float num8 = Vector3.Distance(vector, list[l].CurrentPosition);
                                        if (num8 < list[l].HighestLOSPosition.y)
                                        {
                                            flag = true;
                                            num4 += 1f;
                                            if (num8 < num5)
                                            {
                                                num5 = num8;
                                                collisionWorldPos = vector;
                                                break;
                                            }
                                            break;
                                        }
                                    }
                                }
                            }
                            if (__instance.HasLineOfFire(lossourcePositions[j], lostargetPositions[k], text, num6, out vector))
                            {
                                num2 += 1f;
                                if (text != null)
                                {
                                    break;
                                }
                            }
                            else
                            {
                                if (flag)
                                {
                                    num4 -= 1f;
                                }
                                float num8 = Vector3.Distance(vector, sourcePosition);
                                if (num8 < num5)
                                {
                                    num5 = num8;
                                    collisionWorldPos = vector;
                                }
                            }
                        }
                    }
                    if (text != null && num2 > 0.5f)
                    {
                        break;
                    }
                }
                float num9 = (text == null) ? (num2 / num3) : num2;
                float b = num9 - source.Combat.Constants.Visibility.MinRatioFromActors;
                float num10 = Mathf.Min(num4 / num3, b);
                if (num10 > 0.001f)
                {
                    num9 -= num10;
                }
                if (num9 >= source.Combat.Constants.Visibility.RatioFullVis)
                {
                    __result = LineOfFireLevel.LOFClear;
                    return false;
                }
                if (num9 >= source.Combat.Constants.Visibility.RatioObstructedVis)
                {
                    __result = LineOfFireLevel.LOFObstructed;
                    return false;
                }
                __result = LineOfFireLevel.LOFBlocked;
                return false;
            }
        }

       [HarmonyPatch(typeof(LOFCache), "GetLineOfFire")]
        public static class LOFCache_GetLineOfFire
        {
            //static bool Prepare() => false;
            public static void Postfix(LOFCache __instance, AbstractActor source, Vector3 sourcePosition, ICombatant target, Vector3 targetPosition, Quaternion targetRotation, out Vector3 collisionWorldPos, ref LineOfFireLevel __result)
            {
                collisionWorldPos = targetPosition;

                if (source.IsAirlifted())
                {
                    if (!ModState.AirliftTrackers[source.GUID].IsCarriedInternal)
                    {
                        var carrier = source.Combat.FindActorByGUID(ModState.AirliftTrackers[source.GUID].CarrierGUID);
                        __result = source.Combat.LOFCache.GetLineOfFire(carrier, carrier.CurrentPosition, target,
                            targetPosition, targetRotation, out collisionWorldPos);
                    }
                }

                else if (source.IsMountedUnit())
                {
                    var carrier = source.Combat.FindActorByGUID(ModState.PositionLockMount[source.GUID]);
                    if (carrier.hasFiringPorts())
                    {
                        __result = source.Combat.LOFCache.GetLineOfFire(carrier, carrier.CurrentPosition, target,
                            targetPosition, targetRotation, out collisionWorldPos);
                        //ModInit.modLog?.Debug?.Write($"[LOFCache.GetLineOfFire] returning LOF {__result} from carrier {carrier.DisplayName} for squad {source.DisplayName}");
                    }
                }

                //                else if (source.isGarrisoned())
                //                {

//                    var carrier = source.Combat.FindCombatantByGUID(ModState.PositionLockGarrison[source.GUID].BuildingGUID);
//                        __result = carrier.GetLineOfFireForGarrison(source, carrier.CurrentPosition, target,
//                            targetPosition, targetRotation, out collisionWorldPos);
//ModInit.modLog?.Debug?.Write($"[LOFCache.GetLineOfFire] returning LOF {__result} from carrier {carrier.DisplayName} for squad {source.DisplayName}");

                //                }
                //__result = LineOfFireLevel.LOFClear;
            }
        }

        [HarmonyPatch(typeof(MechRepresentation), "ToggleHeadlights")]
        public static class MechRepresentation_ToggleHeadlights
        {
            public static void Postfix(MechRepresentation __instance, bool headlightsActive, List<GameObject> ___headlightReps)
            {
                if (__instance.parentActor.IsSwarmingUnit() || __instance.parentActor.IsMountedUnit() || __instance.parentActor.IsAirlifted() || __instance.parentActor.isGarrisoned())
                {
                    var customRep = __instance as CustomMechRepresentation;
                    if (customRep != null)
                    {
                        customRep._ToggleHeadlights(false);
                    }
                    else
                    {
                        for (int i = 0; i < ___headlightReps.Count; i++)
                        {
                            ___headlightReps[i].SetActive(false);
                        }
                    }
                }
            }
        }
    }
}