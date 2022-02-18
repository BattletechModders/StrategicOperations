using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using CustAmmoCategories;
using CustomActivatableEquipment;
using CustomComponents;
using CustomUnits;
using Harmony;
using Localize;
using UnityEngine;
using Random = UnityEngine.Random;

namespace StrategicOperations.Framework
{
    public static class BattleArmorUtils
    {
        public static void ShowBATargetsMeleeIndicator(this CombatHUDInWorldElementMgr inWorld, List<AbstractActor> targets, AbstractActor unit)
        {
            var tickMarks = Traverse.Create(inWorld).Field("WeaponTickMarks")
                .GetValue<List<CombatHUDWeaponTickMarks>>();

            for (int i = 0; i < tickMarks.Count; i++)
            {
                if (targets.Contains(tickMarks[i].Owner))
                {
                    if (tickMarks[i].Owner.team.IsEnemy(unit.team))
                    {
                        if (tickMarks[i].MeleeTweenAnimations != null)
                        {
                            tickMarks[i].MeleeTweenAnimations.SetState(ButtonState.Enabled, false);
                        }
                    }
                }
            }
        }
        public static List<float> CreateBPodDmgClusters(List<int> locs, float totalDmg)
        {
            var clusters = new List<float>();
            for (int i = 0; i < locs.Count; i++)
            {
                clusters.Add(0f);
            }
            ModInit.modLog.LogTrace($"[CreateBPodDmgClusters] Generating {locs.Count} clusters of dmg from {totalDmg}");
            var unapportionedDmg = totalDmg;
            var idx = 0;
            while (unapportionedDmg > 0)
            {
                var pendingDmg = Random.Range(0f, unapportionedDmg);
                ModInit.modLog.LogTrace($"[CreateBPodDmgClusters] Current damage for idx {idx} is {clusters[idx]}; {pendingDmg} to be added");
                clusters[idx] += pendingDmg;
                unapportionedDmg -= pendingDmg;
                ModInit.modLog.LogTrace($"[CreateBPodDmgClusters] {unapportionedDmg} remains to be assigned");
                if (unapportionedDmg < 1f) unapportionedDmg = 0f;
                idx++;
                ModInit.modLog.LogTrace($"[CreateBPodDmgClusters] Moving to idx {idx}");
                if (idx >= clusters.Count)
                {
                    ModInit.modLog.LogTrace($"[CreateBPodDmgClusters] idx {idx} out of range, resetting to 0");
                    idx = 0;
                }
            }
            return clusters;
        }
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
                        if (dm.Exists(BattleTechResourceType.MechDef, BaTypeInternal.Key) || BaTypeInternal.Key == "BA_EMPTY")
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
                        if (dm.Exists(BattleTechResourceType.MechDef, BaTypeMount.Key) || BaTypeMount.Key == "BA_EMPTY")
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
                        if (dm.Exists(BattleTechResourceType.MechDef, BaTypeHandsy.Key) || BaTypeHandsy.Key == "BA_EMPTY")
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

        internal class DetachFromCarrierDelegate
        {
            public TrooperSquad squad { get; set; }
            public CustomMech detachTarget { get; set; }
            public float detachTargetDownSpeed = -20f;
            public float detachTargetUpSpeed = 5f;

            public DetachFromCarrierDelegate(TrooperSquad squad, CustomMech target)
            {
                this.squad = squad;
                this.detachTarget = target;
            }

            public void OnRestoreHeightControl()
            {
                detachTarget.custGameRep.HeightController.UpSpeed = detachTargetUpSpeed;
                detachTarget.custGameRep.HeightController.DownSpeed = detachTargetDownSpeed;
            }
            public void OnLandDetach()
            {
                squad.GameRep.transform.localScale = new Vector3(1f, 1f, 1f);
                squad.GameRep.ToggleHeadlights(true);
                //ModState.SavedBAScale[squad.GUID];
                //if (ModState.SavedBAScale.ContainsKey(squad.GUID))
                //{
                //    squad.GameRep.transform.localScale = ModState.SavedBAScale[squad.GUID];
                //    ModState.SavedBAScale.Remove(squad.GUID);
                //}
            }
        }

        internal class AttachToCarrierDelegate
        {
            public TrooperSquad squad { get; set; }
            public CustomMech attachTarget { get; set; }
            public float attachTargetDownSpeed = -20f;
            public float attachTargetUpSpeed = 5f;

            public AttachToCarrierDelegate(TrooperSquad squad, CustomMech target)
            {
                this.squad = squad;
                this.attachTarget = target;
            }

            public void OnLandAttach()
            {
                //HIDE SQUAD REPRESENTATION
                attachTarget.MountBattleArmorToChassis(squad, true);
                //attachTarget.HideBattleArmorOnChassis(squad);
            }

            public void OnRestoreHeightControl()
            {
                attachTarget.custGameRep.HeightController.UpSpeed = attachTargetUpSpeed;
                attachTarget.custGameRep.HeightController.DownSpeed = attachTargetDownSpeed;
                var pos = attachTarget.CurrentPosition +
                          Vector3.up * attachTarget.custGameRep.HeightController.CurrentHeight;
                squad.TeleportActor(pos);
            }
        }

        public static void AttachToCarrier(this TrooperSquad squad, AbstractActor attachTarget)
        {
            if (attachTarget is CustomMech custMech && attachTarget.team.IsFriendly(squad.team))
            {
                ModInit.modLog.LogTrace($"AttachToCarrier processing on friendly.");
                if (custMech.FlyingHeight() > 1.5f)
                {
                    //Check if actually flying unit
                    //CALL ATTACH CODE BUT WITHOUT SQUAD REPRESENTATION HIDING
                    //custMech.MountBattleArmorToChassis(squad, false);

                    custMech.custGameRep.HeightController.UpSpeed = 50f;
                    custMech.custGameRep.HeightController.DownSpeed = -50f;

                    var attachDel = new AttachToCarrierDelegate(squad, custMech);
                    custMech.DropOffAnimation(attachDel.OnLandAttach, attachDel.OnRestoreHeightControl);
                }
                else
                {
                    ModInit.modLog.LogTrace($"AttachToCarrier call mount.");
                    //CALL DEFAULT ATTACH CODE
                    custMech.MountBattleArmorToChassis(squad, true);
                }
            }
            else
            {
                ModInit.modLog.LogTrace($"AttachToCarrier call mount.");
                //CALL DEFAULT ATTACH CODE
                attachTarget.MountBattleArmorToChassis(squad, true);
            }
        }

        public static void DetachFromCarrier(this TrooperSquad squad, AbstractActor attachTarget)
        {
            ModState.PositionLockMount.Remove(squad.GUID);
            if (attachTarget is CustomMech custMech && attachTarget.team.IsFriendly(squad.team))
            {
                ModInit.modLog.LogTrace($"DetachFromCarrier processing on friendly.");
                if (custMech.FlyingHeight() > 1.5f)
                {
                    //Check if actually flying unit
                    //CALL ATTACH CODE BUT WITHOUT SQUAD REPRESENTATION HIDING
                    //custMech.DismountBA(squad, false, false, false);
                    custMech.custGameRep.HeightController.UpSpeed = 50f;
                    custMech.custGameRep.HeightController.DownSpeed = -50f;

                    var detachDel = new DetachFromCarrierDelegate(squad, custMech);
                    custMech.DropOffAnimation(detachDel.OnLandDetach, detachDel.OnRestoreHeightControl);
                }
                else
                {
                    ModInit.modLog.LogTrace($"DetachFromCarrier call dismount.");
                    //CALL DEFAULT ATTACH CODE
                    squad.DismountBA(custMech, Vector3.zero, false, false, true);
                }
            }
            else
            {
                ModInit.modLog.LogTrace($"DetachFromCarrier call dismount.");
                //CALL DEFAULT ATTACH CODE
                squad.DismountBA(attachTarget, Vector3.zero, false, false, true);
            }
        }

        public static void MountBattleArmorToChassis(this AbstractActor carrier, AbstractActor battleArmor, bool shrinkRep)
        {
            if (battleArmor is Mech battleArmorAsMech)
            {
                //add irbtu immobile tag?
                //Statistic irbtmu_immobile_unit = battleArmor.StatCollection.GetStatistic("irbtmu_immobile_unit");
                //if (!battleArmor.StatCollection.ContainsStatistic("irbtmu_immobile_unit"))
                //{
                //    battleArmor.StatCollection.AddStatistic<bool>("irbtmu_immobile_unit", false);
                //}

                //battleArmor.StatCollection.Set("irbtmu_immobile_unit", true);
                if (shrinkRep)
                {
                    //var baseScale = battleArmor.GameRep.transform.localScale;
                    //ModState.SavedBAScale.Add(battleArmor.GUID, baseScale);
                    battleArmor.GameRep.transform.localScale = new Vector3(.01f, .01f, .01f);
                    battleArmorAsMech.GameRep.ToggleHeadlights(false);
                }

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
                        // try and set firing arc to 360?
                        battleArmor.FiringArc(360f);
                        return;
                    }

                    carrier.setHasExternalMountedBattleArmor(true);
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

        public static float getMovementDeSwarmMinChance(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<float>("MovementDeSwarmMinChance");
        }
        public static float getMovementDeSwarmMaxChance(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<float>("MovementDeSwarmMaxChance");
        }
        public static float getMovementDeSwarmEvasivePipsFactor(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<float>("MovementDeSwarmEvasivePipsFactor");
        }
        public static float getMovementDeSwarmEvasiveJumpMovementMultiplier(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<float>("MovementDeSwarmEvasiveJumpMovementMultiplier");
        }

        public static bool hasFiringPorts(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<bool>("HasFiringPorts");
        }

        public static bool canSwarm(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<bool>("CanSwarm");
        }

        public static bool canRideInternalOnly(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<bool>("BattleArmorInternalMountsOnly");
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

        public static bool getHasExternalMountedBattleArmor(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<bool>("HasExternalMountedBattleArmor");
        }

        public static void setHasExternalMountedBattleArmor(this AbstractActor actor, bool value)
        {
            actor.StatCollection.ModifyStat("BAMountDismount", -1, "HasExternalMountedBattleArmor", StatCollection.StatOperation.Set, value);
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

        public static bool IsMountedInternal(this AbstractActor actor)
        {
            return (ModState.BADamageTrackers.ContainsKey(actor.GUID) && ModState.BADamageTrackers[actor.GUID].IsSquadInternal);
        }

        public static bool IsMountedToUnit(this AbstractActor actor, AbstractActor target)
        {
            return ModState.PositionLockMount.ContainsKey(actor.GUID) && ModState.PositionLockMount[actor.GUID] == target.GUID;
        }

        public static bool HasSwarmingUnits(this AbstractActor actor)
        {
            return ModState.PositionLockSwarm.ContainsValue(actor.GUID);
        }

        public static bool IsSwarmingUnit(this AbstractActor actor)
        {
            return ModState.PositionLockSwarm.ContainsKey(actor.GUID);
        }
        public static bool IsSwarmingTargetUnit(this AbstractActor actor, AbstractActor target)
        {
            return ModState.PositionLockSwarm.ContainsKey(actor.GUID) && ModState.PositionLockSwarm[actor.GUID] == target.GUID;
        }

        public static Vector3 FetchAdjacentHex(AbstractActor actor)
        {
            var points = actor.Combat.HexGrid.GetAdjacentPointsOnGrid(actor.CurrentPosition);
            var point = points.GetRandomElement();
            point.y = actor.Combat.MapMetaData.GetLerpedHeightAt(point, false);
            return point;
        }

        public static void DismountBA(this AbstractActor actor, AbstractActor carrier, Vector3 locationOverride, bool calledFromDeswarm = false,
            bool calledFromHandleDeath = false, bool unShrinkRep = true)
        {
            if (actor is TrooperSquad squad)
            {
                //if (squad.StatCollection.ContainsStatistic("irbtmu_immobile_unit")) squad.StatCollection.Set("irbtmu_immobile_unit", false);
                if (ModState.BADamageTrackers.ContainsKey(actor.GUID))
                {
                    if (squad.team.IsFriendly(carrier.team))
                    {
                        if (ModState.BADamageTrackers[actor.GUID].IsSquadInternal)
                        {
                            carrier.modifyInternalBASquads(-1);
                            ModInit.modLog.LogMessage(
                                $"[DismountBA] Dismounted {actor.DisplayName} from internal capacity. Capacity is now {carrier.getAvailableInternalBASpace()}.");
                            squad.FiringArc(90f); //reset to 90?
                        }
                        else carrier.setHasExternalMountedBattleArmor(false);
                    }

                    ModState.BADamageTrackers.Remove(actor.GUID);
                }

                var em = actor.Combat.EffectManager;
                foreach (var BA_effect in ModState.BA_MountSwarmEffects)
                {
                    var effects = em.GetAllEffectsTargetingWithUniqueID(actor, BA_effect.ID);
                    for (int i = effects.Count - 1; i >= 0; i--)
                    {
                        ModInit.modLog.LogMessage(
                            $"[DismountBA] Cancelling effect on {actor.DisplayName}: {effects[i].EffectData.Description.Name}.");
                        em.CancelEffect(effects[i]);
                    }
                }

                var hud = Traverse.Create(CameraControl.Instance).Property("HUD").GetValue<CombatHUD>();
                //actor.GameRep.IsTargetable = true;

                ModState.PositionLockMount.Remove(actor.GUID);
                ModState.PositionLockSwarm.Remove(actor.GUID);
                ModState.CachedUnitCoordinates.Remove(carrier.GUID);

                if (unShrinkRep)
                {
                    actor.GameRep.transform.localScale = new Vector3(1f, 1f, 1f);
                    //actor.GameRep.transform.localScale = ModState.SavedBAScale[actor.GUID];
                    //ModState.SavedBAScale.Remove(actor.GUID);
                    squad.GameRep.ToggleHeadlights(true);
                }
                var point = carrier.CurrentPosition;
                if (locationOverride != Vector3.zero)
                {
                    point = locationOverride;
                    ModInit.modLog.LogMessage($"[DismountBA] Using location override {locationOverride}.");
                }
                point.y = actor.Combat.MapMetaData.GetLerpedHeightAt(point, false);
                actor.TeleportActor(point);

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
                if (false) //(actor.HasBegunActivation)
                {
                    ModInit.modLog.LogMessage(
                        $"[DismountBA] Called from handledeath? {calledFromHandleDeath} or Deswarm? {calledFromDeswarm}, forcing end of activation."); // was i trying to end carrier activation maybe?

                    var sequence = actor.DoneWithActor();
                    actor.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(sequence));
                    //actor.OnActivationEnd(actor.GUID, -1);
                }

                actor.VisibilityCache.UpdateCacheReciprocal(actor.Combat.GetAllLivingCombatants());

                ModInit.modLog.LogMessage(
                    $"[DismountBA] Removing PositionLock with rider  {actor.DisplayName} {actor.GUID} and carrier {carrier.DisplayName} {carrier.GUID} and rebuilding visibility cache.");
            }
        }

        public static Ability GetDeswarmerAbilityForAI(this AbstractActor actor, bool UseMovement = false)
        {
            var list = new List<Ability>();

            if (UseMovement)
            {
                if (!string.IsNullOrEmpty(ModInit.modSettings.BattleArmorDeSwarmMovement))
                {
                    var move = actor.GetPilot().Abilities
                        .FirstOrDefault(x => x.Def.Id == ModInit.modSettings.BattleArmorDeSwarmMovement) ?? actor.ComponentAbilities
                        .FirstOrDefault(x => x.Def.Id == ModInit.modSettings.BattleArmorDeSwarmMovement);
                    if (move != null) return move;
                }
            }

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

        public static void ProcessDeswarmRoll(this AbstractActor creator, List<KeyValuePair<string, string>> swarmingUnits)
        {
            var finalChance = 0f;
            var rollInitPenalty = creator.StatCollection.GetValue<int>("BattleArmorDeSwarmerRollInitPenalty");
            if (!creator.team.IsLocalPlayer)
            {
                var baseChance = creator.StatCollection.GetValue<float>("BattleArmorDeSwarmerRoll");//0.5f;
                var pilotSkill = creator.GetPilot().Piloting;
                finalChance = Mathf.Min(baseChance + (0.05f * pilotSkill), 0.95f);
                ModInit.modLog.LogMessage($"[Ability.Activate - BattleArmorDeSwarm] Deswarm chance: {finalChance} from baseChance {baseChance} + pilotSkill x 0.05 {0.05f * pilotSkill}, max 0.95.");
            }
            else
            {
                finalChance = ModState.DeSwarmSuccessChance;
                ModInit.modLog.LogMessage($"[Ability.Activate - BattleArmorDeSwarm] restored deswarm roll chance from state: {ModState.DeSwarmSuccessChance}");
            }
            var roll = ModInit.Random.NextDouble();
            foreach (var swarmingUnit in swarmingUnits)
            {
                var swarmingUnitActor = creator.Combat.FindActorByGUID(swarmingUnit.Key);
                var swarmingUnitSquad = swarmingUnitActor as TrooperSquad;
                if (roll <= finalChance)
                {
                    ModInit.modLog.LogMessage(
                        $"[Ability.Activate - BattleArmorDeSwarm] Deswarm SUCCESS: {roll} <= {finalChance}.");
                    var txt = new Text("Remove Swarming Battle Armor: SUCCESS");
                    creator.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(
                        new ShowActorInfoSequence(creator, txt, FloatieMessage.MessageNature.Buff,
                            false)));
                    for (int i = 0; i < rollInitPenalty; i++)
                    {
                        swarmingUnitActor.ForceUnitOnePhaseDown(creator.GUID, -1, false);
                    }
                    var destroyBARoll = ModInit.Random.NextDouble();
                    if (destroyBARoll <= .3f)
                    {
                        ModInit.modLog.LogMessage(
                            $"[Ability.Activate - DestroyBA on Roll] SUCCESS: {destroyBARoll} <= {finalChance}.");
                        var trooperLocs = swarmingUnitActor.GetPossibleHitLocations(creator);
                        for (int i = 0; i < trooperLocs.Count; i++)
                        {
                            var cLoc = (ChassisLocations)trooperLocs[i];
                            var hitinfo = new WeaponHitInfo(-1, -1, 0, 0, creator.GUID, swarmingUnitActor.GUID, 1, new float[1], new float[1], new float[1], new bool[1], new int[trooperLocs[i]], new int[1], new AttackImpactQuality[1], new AttackDirection[1], new Vector3[1], new string[1], new int[trooperLocs[i]]);
                            swarmingUnitSquad?.NukeStructureLocation(hitinfo, trooperLocs[i], cLoc, Vector3.up, DamageType.ComponentExplosion);
                        }
                        swarmingUnitActor.DismountBA(creator, Vector3.zero, false, true);
                        swarmingUnitActor.FlagForDeath("Squished", DeathMethod.VitalComponentDestroyed, DamageType.Melee, 0, -1, creator.GUID, false);
                        swarmingUnitActor.HandleDeath(creator.GUID);
                    }
                    else
                    {
                        ModInit.modLog.LogMessage(
                            $"[Ability.Activate - DestroyBA on Roll] FAILURE: {destroyBARoll} > {finalChance}.");
                        swarmingUnitActor.DismountBA(creator, Vector3.zero, true);
                    }
                }
                else
                {
                    var txt = new Text("Remove Swarming Battle Armor: FAILURE");
                    creator.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(
                        new ShowActorInfoSequence(creator, txt, FloatieMessage.MessageNature.Buff,
                            false)));
                    ModInit.modLog.LogMessage(
                            $"[Ability.Activate - BattleArmorDeSwarm] Deswarm FAILURE: {roll} > {finalChance}.");
                }
            }
        }

        public static void ProcessDeswarmSwat(this AbstractActor creator,
            List<KeyValuePair<string, string>> swarmingUnits)
        {
            var finalChance = 0f;
            var swatInitPenalty =
                creator.StatCollection.GetValue<int>("BattleArmorDeSwarmerSwatInitPenalty");
            if (!creator.team.IsLocalPlayer)
            {
                var baseChance =
                    creator.StatCollection.GetValue<float>(
                        "BattleArmorDeSwarmerSwat"); //0.5f;//0.3f;
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

                finalChance = baseChance + (0.05f * pilotSkill) - (0.05f * missingActuatorCount);
                ModInit.modLog.LogMessage(
                    $"[Ability.Activate - BattleArmorDeSwarm] Deswarm chance: {finalChance} from baseChance {baseChance} + pilotSkill x 0.05 {0.05f * pilotSkill} - missingActuators x 0.05 {0.05f * missingActuatorCount}.");
            }
            else
            {
                finalChance = ModState.DeSwarmSuccessChance;
                ModInit.modLog.LogMessage($"[Ability.Activate - BattleArmorDeSwarm] restored deswarm swat chance from state: {ModState.DeSwarmSuccessChance}");
            }
            var roll = ModInit.Random.NextDouble();
            foreach (var swarmingUnit in swarmingUnits)
            {
                var swarmingUnitActor = creator.Combat.FindActorByGUID(swarmingUnit.Key);
                if (roll <= finalChance)
                {
                    var txt = new Text("Remove Swarming Battle Armor: SUCCESS");
                    creator.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(
                        new ShowActorInfoSequence(creator, txt, FloatieMessage.MessageNature.Buff,
                            false)));
                    ModInit.modLog.LogMessage($"[Ability.Activate - BattleArmorDeSwarm] Deswarm SUCCESS: {roll} <= {finalChance}.");
                    for (int i = 0; i < swatInitPenalty; i++)
                    {
                        swarmingUnitActor.ForceUnitOnePhaseDown(creator.GUID, -1, false);
                    }
                    var dmgRoll = ModInit.Random.NextDouble();
                    if (dmgRoll <= finalChance)
                    {
                        if (swarmingUnitActor is TrooperSquad swarmingUnitAsSquad)
                        {
                            var baLoc = swarmingUnitAsSquad.GetPossibleHitLocations(creator).GetRandomElement();
                            ModInit.modLog.LogMessage($"[Ability.Activate - BattleArmorDeSwarm] BA Armor Damage Location {baLoc}: {swarmingUnitAsSquad.GetStringForArmorLocation((ArmorLocation)baLoc)}");
                            var swatDmg = creator.StatCollection.GetValue<float>("BattleArmorDeSwarmerSwatDamage");
                            var hitinfo = new WeaponHitInfo(-1, -1, 0, 0, creator.GUID, swarmingUnitActor.GUID, 1, new float[1], new float[1], new float[1], new bool[1], new int[baLoc], new int[1], new AttackImpactQuality[1], new AttackDirection[1], new Vector3[1], new string[1], new int[baLoc]);

                            swarmingUnitActor.TakeWeaponDamage(hitinfo, baLoc, swarmingUnitAsSquad.MeleeWeapon, swatDmg, 0, 0, DamageType.Melee);
                        }
                    }
                    swarmingUnitActor.DismountBA(creator, Vector3.zero, true);
                }
                else
                {
                    var txt = new Text("Remove Swarming Battle Armor: FAILURE");
                    creator.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(
                        new ShowActorInfoSequence(creator, txt, FloatieMessage.MessageNature.Buff,
                            false)));
                    ModInit.modLog.LogMessage(
                        $"[Ability.Activate - BattleArmorDeSwarm] Deswarm FAILURE: {roll} > {finalChance}. Doing nothing and ending turn!");
                }
            }
        }

        public static void ProcessDeswarmMovement(this AbstractActor creator, List<KeyValuePair<string, string>> swarmingUnits)
        {
            var DeswarmMovementInfo = new Classes.BA_DeswarmMovementInfo(creator);
            ModInit.modLog.LogTrace($"[ProcessDeswarmMovement] - {DeswarmMovementInfo.Carrier.DisplayName} set to {creator.DisplayName}.");
            foreach (var swarmingUnit in swarmingUnits)
            {
                var swarmingUnitActor = creator.Combat.FindActorByGUID(swarmingUnit.Key);
                DeswarmMovementInfo.SwarmingUnits.Add(swarmingUnitActor);
                ModInit.modLog.LogTrace($"[ProcessDeswarmMovement] - Added {swarmingUnitActor.DisplayName} to list of swarming.");
            }
            ModState.DeSwarmMovementInfo = DeswarmMovementInfo;
            ModInit.modLog.LogTrace($"[ProcessDeswarmMovement] - Set modstate.");
        }

        public static void ProcessMountFriendly(this AbstractActor creator, AbstractActor targetActor)
        {
            foreach (var BA_Effect in ModState.BA_MountSwarmEffects)
            {
                if (BA_Effect.TargetEffectType == Classes.BA_TargetEffectType.BOTH)
                {
                    foreach (var effectData in BA_Effect.effects)
                    {
                        creator.Combat.EffectManager.CreateEffect(effectData,
                            BA_Effect.ID,
                            -1, creator, creator, default(WeaponHitInfo), 1);
                    }
                }
                if (BA_Effect.TargetEffectType == Classes.BA_TargetEffectType.MOUNT_EXT && !creator.IsMountedInternal())
                {
                    foreach (var effectData in BA_Effect.effects)
                    {
                        creator.Combat.EffectManager.CreateEffect(effectData,
                            BA_Effect.ID,
                            -1, creator, creator, default(WeaponHitInfo), 1);
                    }
                }
                if (BA_Effect.TargetEffectType == Classes.BA_TargetEffectType.MOUNT_INT && creator.IsMountedInternal())
                {
                    foreach (var effectData in BA_Effect.effects)
                    {
                        creator.Combat.EffectManager.CreateEffect(effectData,
                            BA_Effect.ID,
                            -1, creator, creator, default(WeaponHitInfo), 1);
                    }
                }
            }

            creator.TeleportActor(targetActor.CurrentPosition);

            ModState.PositionLockMount.Add(creator.GUID, targetActor.GUID);
            if (creator is TrooperSquad squad)
            {
                squad.GameRep.transform.localScale = new Vector3(.01f, .01f, .01f);
                squad.AttachToCarrier(targetActor);
                ModInit.modLog.LogTrace($"[Ability.Activate - BattleArmorMountID] Called AttachToCarrier.");
            }
            ModInit.modLog.LogMessage(
                $"[Ability.Activate - BattleArmorMountID] Added PositionLockMount with rider  {creator.DisplayName} {creator.GUID} and carrier {targetActor.DisplayName} {targetActor.GUID}.");

            if (creator.team.IsLocalPlayer)
            {
                var sequence = creator.DoneWithActor();
                creator.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(sequence));
            }
        }

        public static void ProcessSwarmEnemy(this Mech creatorMech, AbstractActor targetActor)
        {
            if (!creatorMech.canSwarm())
            {
                var popup = GenericPopupBuilder.Create(GenericPopupType.Info, $"Unit {creatorMech.DisplayName} is unable to make swarming attacks!");
                popup.AddButton("Confirm", null, true, null);
                popup.IsNestedPopupWithBuiltInFader().CancelOnEscape().Render();
                return;
            }

            targetActor.CheckForBPodAndActivate();
            if (creatorMech.IsFlaggedForDeath)
            {
                creatorMech.HandleDeath(targetActor.GUID);
                return;
            }

            var meleeChance = creatorMech.team.IsLocalPlayer ? ModState.SwarmSuccessChance : creatorMech.Combat.ToHit.GetToHitChance(creatorMech, creatorMech.MeleeWeapon, targetActor, creatorMech.CurrentPosition, targetActor.CurrentPosition, 1, MeleeAttackType.Charge, false);

            var roll = ModInit.Random.NextDouble();
            ModInit.modLog.LogMessage($"[Ability.Activate - BattleArmorSwarmID] Rolling simplified melee: roll {roll} vs hitChance {meleeChance}; chance in Modstate was {ModState.SwarmSuccessChance}.");
            if (roll <= meleeChance)
            {
                foreach (var BA_Effect in ModState.BA_MountSwarmEffects)
                {
                    if (BA_Effect.TargetEffectType == Classes.BA_TargetEffectType.SWARM ||
                        BA_Effect.TargetEffectType == Classes.BA_TargetEffectType.BOTH)
                    {
                        foreach (var effectData in BA_Effect.effects)
                        {
                            creatorMech.Combat.EffectManager.CreateEffect(effectData,
                                BA_Effect.ID,
                                -1, creatorMech, creatorMech, default(WeaponHitInfo), 1);
                        }
                    }
                }

                var txt = new Text("Swarm Attack: SUCCESS");
                creatorMech.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(
                    new ShowActorInfoSequence(creatorMech, txt, FloatieMessage.MessageNature.Buff,
                        false)));

                ModInit.modLog.LogMessage(
                    $"[Ability.Activate - BattleArmorSwarmID] Cleaning up dummy attacksequence.");

                //creator.GameRep.IsTargetable = false;
                creatorMech.TeleportActor(targetActor.CurrentPosition);

                //creator.GameRep.enabled = false;
                //creator.GameRep.gameObject.SetActive(false); //this might be the problem with attacking.
                //creator.GameRep.gameObject.Despawn();
                //UnityEngine.Object.Destroy(creator.GameRep.gameObject);
                //CombatMovementReticle.Instance.RefreshActor(creator);

                ModState.PositionLockSwarm.Add(creatorMech.GUID, targetActor.GUID);
                if (creatorMech is TrooperSquad squad)
                {
                    squad.AttachToCarrier(targetActor);
                    ModInit.modLog.LogTrace($"[Ability.Activate - BattleArmorMountID] Called AttachToCarrier.");
                }
                ModInit.modLog.LogMessage(
                    $"[Ability.Activate - BattleArmorSwarmID] Added PositionLockSwarm with rider  {creatorMech.DisplayName} {creatorMech.GUID} and carrier {targetActor.DisplayName} {targetActor.GUID}.");
                creatorMech.ResetPathing(false);
                creatorMech.Pathing.UpdateCurrentPath(false);

                if (ModInit.modSettings.AttackOnSwarmSuccess && creatorMech.team.IsLocalPlayer)
                {
                    var weps = creatorMech.Weapons.Where(x => x.IsEnabled && x.HasAmmo).ToList();
                    var loc = ModState.BADamageTrackers[creatorMech.GUID].BA_MountedLocations.Values
                        .GetRandomElement();
                    if (true)
                    {
                        var attackStackSequence = new AttackStackSequence(creatorMech, targetActor,
                            creatorMech.CurrentPosition,
                            creatorMech.CurrentRotation, weps, MeleeAttackType.NotSet, loc, -1);
                        creatorMech.Combat.MessageCenter.PublishMessage(
                            new AddSequenceToStackMessage(attackStackSequence));
                        ModInit.modLog.LogMessage(
                            $"[Ability.Activate - BattleArmorSwarmID] Creating attack sequence on successful swarm attack targeting location {loc}.");
                    }
                }
                if (creatorMech.team.IsLocalPlayer)
                {
                    var sequence = creatorMech.DoneWithActor();
                    creatorMech.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(sequence));
                    //creator.OnActivationEnd(creator.GUID, -1);
                }
            }
            else
            {
                var txt = new Text("Swarm Attack: FAILURE");
                creatorMech.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(
                    new ShowActorInfoSequence(creatorMech, txt, FloatieMessage.MessageNature.Buff,
                        false)));
                ModInit.modLog.LogMessage(
                    $"[Ability.Activate - BattleArmorSwarmID] Cleaning up dummy attacksequence.");
                ModInit.modLog.LogMessage(
                    $"[Ability.Activate - BattleArmorSwarmID] No hits in HitInfo, plonking unit at target hex.");
                creatorMech.TeleportActor(targetActor.CurrentPosition);
                creatorMech.ResetPathing(false);
                creatorMech.Pathing.UpdateCurrentPath(false);
                if (creatorMech.team.IsLocalPlayer)
                {
                    var sequence = creatorMech.DoneWithActor();
                    creatorMech.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(sequence));
                    //creator.OnActivationEnd(creator.GUID, -1);
                }
            }
        }
    }
}
