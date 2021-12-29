using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using CustomActivatableEquipment;
using CustomComponents;
using Harmony;
using UnityEngine;

namespace StrategicOperations.Framework
{
    public static class BattleArmorUtils
    {
        public static void CheckForBPodAndActivate(this AbstractActor actor)
        {
            if (!actor.team.IsLocalPlayer || actor.team.IsLocalPlayer && ModInit.modSettings.BPodsAutoActivate)
            {
                if (actor is Mech mech)
                {
                    foreach (var component in mech.allComponents)
                    {
                        if (ModInit.modSettings.BPodComponentIDs.Contains(component.defId))
                        {
                            if (ActivatableComponent.getChargesCount(component) > 0)
                            {
                                ModInit.modLog.LogMessage($"[CheckForBPodAndActivate] Auto-activating BPod {component.Name} due incoming swarm attempt");
                                ActivatableComponent.activateComponent(component, true, false);
                                break;
                            }
                        }
                    }
                }
            }
        }

        public static string ProcessBattleArmorSpawnWeights(this Classes.BA_FactionAssoc BaWgts, DataManager dm, string factionID, string type)
        {
            if (ModState.CachedFactionAssociations.ContainsKey(factionID))
            {
                if (ModState.CachedFactionAssociations[factionID][type].Count > 0)
                {
                    return ModState.CachedFactionAssociations[factionID][type].GetRandomElement();
                }
            }
            foreach (var faction in BaWgts.FactionIDs)
            {
                if (!ModState.CachedFactionAssociations.ContainsKey(faction))
                {
                    ModState.CachedFactionAssociations.Add(faction, new Dictionary<string, List<string>>());
                    ModState.CachedFactionAssociations[faction].Add("InternalBattleArmorWeight", new List<string>());
                    ModState.CachedFactionAssociations[faction].Add("MountedBattleArmorWeight", new List<string>());
                    ModState.CachedFactionAssociations[faction].Add("HandsyBattleArmorWeight", new List<string>());
                    foreach (var BaTypeInternal in BaWgts.InternalBattleArmorWeight)
                    {
                        if (dm.Exists(BattleTechResourceType.MechDef, BaTypeInternal.Key))
                        {
                            ModInit.modLog.LogTrace(
                                $"[ProcessBattleArmorSpawnWeights - InternalBattleArmorWeight] Processing spawn weights for {BaTypeInternal.Key} and weight {BaTypeInternal.Value}");
                            for (int i = 0; i < BaTypeInternal.Value; i++)
                            {
                                ModState.CachedFactionAssociations[faction]["InternalBattleArmorWeight"]
                                    .Add(BaTypeInternal.Key);
                                ModInit.modLog.LogTrace(
                                    $"[ProcessBattleArmorSpawnWeights - InternalBattleArmorWeight] spawn list has {ModState.CachedFactionAssociations[faction]["InternalBattleArmorWeight"].Count} entries");
                            }
                        }
                    }
                    foreach (var BaTypeMount in BaWgts.MountedBattleArmorWeight)
                    {
                        if (dm.Exists(BattleTechResourceType.MechDef, BaTypeMount.Key))
                        {
                            ModInit.modLog.LogTrace(
                                $"[ProcessBattleArmorSpawnWeights - MountedBattleArmorWeight] Processing spawn weights for {BaTypeMount.Key} and weight {BaTypeMount.Value}");
                            for (int i = 0; i < BaTypeMount.Value; i++)
                            {
                                ModState.CachedFactionAssociations[faction]["MountedBattleArmorWeight"]
                                    .Add(BaTypeMount.Key);
                                ModInit.modLog.LogTrace(
                                    $"[ProcessBattleArmorSpawnWeights - MountedBattleArmorWeight] spawn list has {ModState.CachedFactionAssociations[faction]["MountedBattleArmorWeight"].Count} entries");
                            }
                        }
                    }

                    foreach (var BaTypeHandsy in BaWgts.HandsyBattleArmorWeight)
                    {
                        if (dm.Exists(BattleTechResourceType.MechDef, BaTypeHandsy.Key))
                        {
                            ModInit.modLog.LogTrace(
                                $"[ProcessBattleArmorSpawnWeights - HandsyBattleArmorWeight] Processing spawn weights for {BaTypeHandsy.Key} and weight {BaTypeHandsy.Value}");
                            for (int i = 0; i < BaTypeHandsy.Value; i++)
                            {
                                ModState.CachedFactionAssociations[faction]["HandsyBattleArmorWeight"]
                                    .Add(BaTypeHandsy.Key);
                                ModInit.modLog.LogTrace(
                                    $"[ProcessBattleArmorSpawnWeights - HandsyBattleArmorWeight] spawn list has {ModState.CachedFactionAssociations[faction]["HandsyBattleArmorWeight"].Count} entries");
                            }
                        }
                    }
                }

                if (ModState.CachedFactionAssociations[faction][type].Count > 0)
                {
                    return ModState.CachedFactionAssociations[faction][type].GetRandomElement();
                }
                
            }
            ModInit.modLog.LogError($"[ProcessBattleArmorSpawnWeights] no applicable config for this unit, returning empty list.");
            return "";
        }
        public static void ReInitIndicator(this CombatHUD hud, AbstractActor actor)
        {
            var indicators = Traverse.Create(hud.InWorldMgr).Field("AttackDirectionIndicators")
                .GetValue<List<AttackDirectionIndicator>>();
            foreach (var indicator in indicators)
            {
                if (indicator.Owner.GUID == actor.GUID)
                {
                    indicator.Init(actor, hud);
                }
            }
        }

        public static bool IsAvailableBAAbility(this Ability ability)
        {
            var flag = true;
            if (ability.parentComponent != null)
            {
                flag = ability.parentComponent.IsFunctional;
                ModInit.modLog.LogTrace($"[IsAvailableBAAbility] - {ability.parentComponent.parent.DisplayName} has parentComponent for ability {ability.Def.Description.Name}. Component functional? {flag}.");
                if (!flag)
                {
                    if (ability.parentComponent.parent.ComponentAbilities.Any(x =>
                        x.parentComponent.IsFunctional && x.Def.Id == ability.Def.Id))
                    {
                        flag = true;
                        ModInit.modLog.LogTrace($"[IsAvailableBAAbility] - {ability.parentComponent.parent.DisplayName} has other component with same ability {ability.Def.Description.Name}. Component functional? {flag}.");
                    }
                }
            }
            return ability.CurrentCooldown < 1 && (ability.Def.NumberOfUses < 1 || ability.NumUsesLeft > 0) && flag;
        }// need to redo Ability.Activate from start, completely override for BA? Or just put ability on hidden componenet and ignore this shit.

        public static void MountBattleArmorToChassis(this AbstractActor carrier, AbstractActor battleArmor)
        {
            if (battleArmor is Mech battleArmorAsMech)
            {
                var baseScale = battleArmor.GameRep.transform.localScale;
                ModState.SavedBAScale.Add(battleArmor.GUID, baseScale);
                battleArmor.GameRep.transform.localScale = new Vector3(.01f, .01f, .01f);

                if (!ModState.BADamageTrackers.ContainsKey(battleArmorAsMech.GUID))
                {
                    ModState.BADamageTrackers.Add(battleArmorAsMech.GUID, new Classes.BA_DamageTracker(carrier.GUID, false, new Dictionary<int, int>()));
                }

                var tracker = ModState.BADamageTrackers[battleArmorAsMech.GUID];
                if (tracker.TargetGUID != carrier.GUID)
                {
                    tracker.TargetGUID = carrier.GUID;
                    tracker.BA_MountedLocations = new Dictionary<int, int>();
                }

                if (carrier.team.IsFriendly(battleArmorAsMech.team))
                {
                    var internalCap = carrier.getInternalBACap();
                    var currentInternalSquads = carrier.getInternalBASquads();
                    if (currentInternalSquads < internalCap)
                    {
                        ModInit.modLog.LogMessage($"[MountBattleArmorToChassis] - target unit {carrier.DisplayName} has internal BA capacity of {internalCap}. Currently used: {currentInternalSquads}, mounting squad internally.");
                        carrier.modifyInternalBASquads(1);
                        tracker.IsSquadInternal = true;
                        return;
                    }
                }

                foreach (ChassisLocations BattleArmorChassisLoc in Enum.GetValues(typeof(ChassisLocations)))
                {
                    var locationDef = battleArmorAsMech.MechDef.Chassis.GetLocationDef(BattleArmorChassisLoc);

                    var statname = battleArmorAsMech.GetStringForStructureDamageLevel(BattleArmorChassisLoc);
                    if (string.IsNullOrEmpty(statname) || !battleArmorAsMech.StatCollection.ContainsStatistic(statname)) continue;
                    if (battleArmorAsMech.GetLocationDamageLevel(BattleArmorChassisLoc) == LocationDamageLevel.Destroyed) continue; // why did i do this?

                    if (locationDef.MaxArmor > 0f || locationDef.InternalStructure > 1f)
                    {
                        if (carrier is Mech mech)
                        {
                            if (carrier.team.IsEnemy(battleArmorAsMech.team))
                            {
                                var hasLoopedOnce = false;
                                loopagain:
                                foreach (ArmorLocation tgtMechLoc in ModState.MechArmorSwarmOrder)
                                {
                                    if (tracker.BA_MountedLocations.ContainsValue((int)tgtMechLoc) && !hasLoopedOnce)
                                    {
                                        continue;
                                    }
                                    var chassisLocSwarm =
                                        MechStructureRules.GetChassisLocationFromArmorLocation(tgtMechLoc);
                                    if (mech.GetLocationDamageLevel(chassisLocSwarm) > LocationDamageLevel.Penalized) continue;
                                    if (!tracker.BA_MountedLocations.ContainsKey((int)BattleArmorChassisLoc))
                                    {
                                        ModInit.modLog.LogMessage($"[MountBattleArmorToChassis] - mounting BA {BattleArmorChassisLoc} to enemy mech location {tgtMechLoc}.");
                                        tracker.BA_MountedLocations.Add((int)BattleArmorChassisLoc, (int)tgtMechLoc);
                                        break;
                                    }
                                    hasLoopedOnce = true;
                                    goto loopagain;
                                }
                            }
                            else if (carrier.team.IsFriendly(battleArmorAsMech.team))
                            {
                                var hasLoopedOnce = false;
                                loopagain:
                                foreach (ArmorLocation tgtMechLoc in ModState.MechArmorMountOrder)
                                {
                                    if (tracker.BA_MountedLocations.ContainsValue((int)tgtMechLoc) && !hasLoopedOnce)
                                    {
                                        continue;
                                    }
                                    var chassisLocSwarm =
                                        MechStructureRules.GetChassisLocationFromArmorLocation(tgtMechLoc);
                                    if (mech.GetLocationDamageLevel(chassisLocSwarm) > LocationDamageLevel.Penalized) continue;
                                    if (!tracker.BA_MountedLocations.ContainsKey((int)BattleArmorChassisLoc))
                                    {
                                        ModInit.modLog.LogMessage($"[MountBattleArmorToChassis] - mounting BA {BattleArmorChassisLoc} to friendly mech location {tgtMechLoc}.");
                                        tracker.BA_MountedLocations.Add((int)BattleArmorChassisLoc, (int)tgtMechLoc);
                                        break;
                                    }
                                    hasLoopedOnce = true;
                                    goto loopagain;
                                }
                            }
                        }
                        else if (carrier is Vehicle vehicle)
                        {
                            var hasLoopedOnce = false;
                            loopagain:
                            foreach (VehicleChassisLocations tgtVicLoc in ModState.VehicleMountOrder)
                            {
                                if (tracker.BA_MountedLocations.ContainsValue((int)tgtVicLoc) && !hasLoopedOnce)
                                {
                                    continue;
                                }

                                if (vehicle.GetLocationDamageLevel(tgtVicLoc) > LocationDamageLevel.Penalized) continue;
                                if (!tracker.BA_MountedLocations.ContainsKey((int)BattleArmorChassisLoc))
                                {
                                    ModInit.modLog.LogMessage($"[MountBattleArmorToChassis] - mounting BA {BattleArmorChassisLoc} to vehicle location {tgtVicLoc}.");
                                    tracker.BA_MountedLocations.Add((int)BattleArmorChassisLoc, (int)tgtVicLoc);
                                    break;
                                }
                                hasLoopedOnce = true;
                                goto loopagain;
                            }
                        }
                        else if (carrier is Turret turret)
                        {
                            ModInit.modLog.LogMessage($"[MountBattleArmorToChassis] - mounting BA {BattleArmorChassisLoc} to turret location {BuildingLocation.Structure}.");
                            tracker.BA_MountedLocations.Add((int)BattleArmorChassisLoc, (int)BuildingLocation.Structure);
                        }
                        else
                        {
                            //borken
                        }
                    }
                }
            }
        }

        public static bool canSwarm(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<bool>("CanSwarm");
        }

        public static int getInternalBACap(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<int>("InternalBattleArmorSquadCap");
        }
        public static int getInternalBASquads(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<int>("InternalBattleArmorSquads");
        }

        public static void modifyInternalBASquads(this AbstractActor actor, int value)
        {
            actor.StatCollection.ModifyStat("BAMountDismount", -1, "InternalBattleArmorSquads", StatCollection.StatOperation.Int_Add, value);
        }

        public static int getAvailableInternalBASpace(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<int>("InternalBattleArmorSquadCap") - actor.StatCollection.GetValue<int>("InternalBattleArmorSquads");
        }

        public static bool getHasBattleArmorMounts(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<bool>("HasBattleArmorMounts");
        }
        public static bool getIsBattleArmorHandsy(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<bool>("IsBattleArmorHandsy");
        }

        public static bool getIsUnMountable(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<bool>("IsUnmountableBattleArmor");
        }
        public static bool getIsUnSwarmable(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<bool>("IsUnswarmableBattleArmor");
        }

        public static bool HasMountedUnits(this AbstractActor actor)
        {
            return ModState.PositionLockMount.ContainsValue(actor.GUID);
        }
        public static bool IsMountedUnit(this AbstractActor actor)
        {
            return ModState.PositionLockMount.ContainsKey(actor.GUID);
        }

        public static bool HasSwarmingUnits(this AbstractActor actor)
        {
            return ModState.PositionLockSwarm.ContainsValue(actor.GUID);
        }
        public static bool IsSwarmingUnit(this AbstractActor actor)
        {
            return ModState.PositionLockSwarm.ContainsKey(actor.GUID);
        }

        public static Vector3 FetchAdjacentHex(AbstractActor actor)
        {
            var points = actor.Combat.HexGrid.GetAdjacentPointsOnGrid(actor.CurrentPosition);
            var point = points.GetRandomElement();
            point.y = actor.Combat.MapMetaData.GetLerpedHeightAt(point, false);
            return point;
        }

        public static void DismountBA (this AbstractActor actor, AbstractActor carrier, bool calledFromDeswarm = false, bool calledFromHandleDeath = false)
        {

            if (ModState.BADamageTrackers.ContainsKey(actor.GUID))
            {
                if (ModState.BADamageTrackers[actor.GUID].IsSquadInternal)
                {
                    carrier.modifyInternalBASquads(-1);
                    ModInit.modLog.LogMessage(
                        $"[DismountBA] Dismounted {actor.DisplayName} from internal capacity. Capacity is now {carrier.getAvailableInternalBASpace()}.");
                }
                ModState.BADamageTrackers.Remove(actor.GUID);
            }

            var em = actor.Combat.EffectManager;
            var effects = em.GetAllEffectsTargetingWithUniqueID(actor, ModState.BAUnhittableEffect.ID);
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                ModInit.modLog.LogMessage(
                    $"[DismountBA] Cancelling effect on {actor.DisplayName}: {effects[i].EffectData.Description.Name}.");
                em.CancelEffect(effects[i]);
            }
            var hud = Traverse.Create(CameraControl.Instance).Property("HUD").GetValue<CombatHUD>();
            //actor.GameRep.IsTargetable = true;
            if (ModState.SavedBAScale.ContainsKey(actor.GUID))
            {
                actor.GameRep.transform.localScale = ModState.SavedBAScale[actor.GUID];
                ModState.SavedBAScale.Remove(actor.GUID);
            }

            ModState.PositionLockMount.Remove(actor.GUID);
            ModState.PositionLockSwarm.Remove(actor.GUID);
            ModState.CachedUnitCoordinates.Remove(carrier.GUID);

            if (!calledFromHandleDeath && !calledFromDeswarm)
            {
                ModInit.modLog.LogMessage(
                    $"[DismountBA] Not called from HandleDeath or Deswarm, resetting buttons and pathing.");
                hud.MechWarriorTray.JumpButton.ResetButtonIfNotActive(actor);
                hud.MechWarriorTray.SprintButton.ResetButtonIfNotActive(actor);
                hud.MechWarriorTray.MoveButton.ResetButtonIfNotActive(actor);
                hud.SelectionHandler.AddJumpState(actor);
                hud.SelectionHandler.AddSprintState(actor);
                hud.SelectionHandler.AddMoveState(actor);
                actor.ResetPathing(false);
                actor.Pathing.UpdateCurrentPath(false);
            }
            else// if (actor.HasBegunActivation)
            {
                ModInit.modLog.LogMessage(
                    $"[DismountBA] Called from handledeath? {calledFromHandleDeath} or Deswarm? {calledFromDeswarm}, forcing end of activation.");
                actor.DoneWithActor();
                actor.OnActivationEnd(actor.GUID, -1);
            }
            ModInit.modLog.LogMessage(
                $"[DismountBA] Removing PositionLock with rider  {actor.DisplayName} {actor.GUID} and carrier {carrier.DisplayName} {carrier.GUID}.");
        }

        public static Ability GetDeswarmerAbility(this AbstractActor actor)
        {
            var list = new List<Ability>();

            if (!string.IsNullOrEmpty(ModInit.modSettings.BattleArmorDeSwarmSwat))
            {
                var swat = actor.GetPilot().Abilities
                    .FirstOrDefault(x => x.Def.Id == ModInit.modSettings.BattleArmorDeSwarmSwat) ?? actor.ComponentAbilities
                    .FirstOrDefault(x => x.Def.Id == ModInit.modSettings.BattleArmorDeSwarmSwat);
                if (swat != null) list.Add(swat);
            }

            if (!string.IsNullOrEmpty(ModInit.modSettings.BattleArmorDeSwarmRoll))
            {
                var roll = actor.GetPilot().Abilities
                    .FirstOrDefault(x => x.Def.Id == ModInit.modSettings.BattleArmorDeSwarmRoll) ?? actor.ComponentAbilities
                    .FirstOrDefault(x => x.Def.Id == ModInit.modSettings.BattleArmorDeSwarmRoll);
                if (roll != null) list.Add(roll);
            }

            if (list.Count > 0)
            {
                return list.GetRandomElement();
            }
            return new Ability(new AbilityDef());
        }
    }
}
