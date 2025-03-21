﻿using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using BestHTTP.SocketIO;
using CustAmmoCategories;
using CustomActivatableEquipment;
using CustomComponents;
using CustomUnits;
using HBS.Math;
using UnityEngine;
using UnityEngine.UI;
using static MonoMod.Cil.RuntimeILReferenceBag.FastDelegateInvokers;
using Random = UnityEngine.Random;
using Text = Localize.Text;

namespace StrategicOperations.Framework
{
    public static class BattleArmorUtils
    {
        public const string BA_Simlink = "BA_Simlink_";

        public static void AttachToCarrier(this TrooperSquad squad, AbstractActor attachTarget, bool isFriendly)
        {
            if (isFriendly)
            {
                ModInit.modLog?.Trace?.Write($"AttachToCarrier processing on friendly.");
                if (attachTarget is CustomMech custMech && custMech.FlyingHeight() > 1.5f)
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
                    ModInit.modLog?.Trace?.Write($"AttachToCarrier call mount.");
                    //CALL DEFAULT ATTACH CODE
                    attachTarget.MountBattleArmorToChassis(squad, true, true);
                }
            }
            else
            {
                ModInit.modLog?.Trace?.Write($"AttachToCarrier call mount.");
                //CALL DEFAULT ATTACH CODE
                attachTarget.MountBattleArmorToChassis(squad, true, false);
            }
        }

        internal class AttachToCarrierDelegate
        {
            public float attachTargetDownSpeed = -20f;
            public float attachTargetUpSpeed = 5f;
            public CustomMech attachTarget { get; set; }
            public TrooperSquad squad { get; set; }

            public AttachToCarrierDelegate(TrooperSquad squad, CustomMech target)
            {
                this.squad = squad;
                this.attachTarget = target;
            }

            public void OnLandAttach()
            {
                //HIDE SQUAD REPRESENTATION
                attachTarget.MountBattleArmorToChassis(squad, true, true);
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

        public static float[] CalculateClusterDamages(float totalDamage, int clusters, List<int> possibleLocs, out int[] locs)
        {
            ModInit.modLog?.Trace?.Write($"[CalculateClusterDamages] Generating {totalDamage} total damage in {clusters} clusters");
            var dmgClusters = new float[clusters];
            locs = new int[clusters];
            var dmgSplit = totalDamage / clusters;
            for (int i = 0; i < clusters; i++)
            {
                dmgClusters[i] = dmgSplit;
                locs[i] = possibleLocs.GetRandomElement();
                ModInit.modLog?.Trace?.Write($"[CalculateClusterDamages] Assigning {dmgSplit} damage to location {locs[i]}");
            }
            return dmgClusters;
        }

        public static bool CanRideInternalOnly(this AbstractActor actor)
        {
            return actor != null && actor.StatCollection.GetValue<bool>("BattleArmorInternalMountsOnly");
        }

        public static bool CanSwarm(this AbstractActor actor)
        {
            if (actor == null) return false;
            if (actor.SwarmingDisabled && actor.team is AITeam) return false;
            return actor.StatCollection.GetValue<bool>("CanSwarm");
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
                                ModInit.modLog?.Info?.Write($"[CheckForBPodAndActivate] Auto-activating BPod {component.Name} due incoming swarm attempt");
                                ActivatableComponent.activateComponent(component, true, false);
                                break;
                            }
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
            ModInit.modLog?.Trace?.Write($"[CreateBPodDmgClusters] Generating {locs.Count} clusters of dmg from {totalDmg}");
            var unapportionedDmg = totalDmg;
            var idx = 0;
            while (unapportionedDmg > 0)
            {
                var pendingDmg = Random.Range(0f, unapportionedDmg);
                ModInit.modLog?.Trace?.Write($"[CreateBPodDmgClusters] Current damage for idx {idx} is {clusters[idx]}; {pendingDmg} to be added");
                clusters[idx] += pendingDmg;
                unapportionedDmg -= pendingDmg;
                ModInit.modLog?.Trace?.Write($"[CreateBPodDmgClusters] {unapportionedDmg} remains to be assigned");
                if (unapportionedDmg < 1f) unapportionedDmg = 0f;
                idx++;
                ModInit.modLog?.Trace?.Write($"[CreateBPodDmgClusters] Moving to idx {idx}");
                if (idx >= clusters.Count)
                {
                    ModInit.modLog?.Trace?.Write($"[CreateBPodDmgClusters] idx {idx} out of range, resetting to 0");
                    idx = 0;
                }
            }
            return clusters;
        }

        public static void DetachFromCarrier(this TrooperSquad squad, AbstractActor attachTarget, bool isFriendly)
        {
            ModState.PositionLockMount.Remove(squad.GUID);
            if (isFriendly)
            {
                ModInit.modLog?.Trace?.Write($"DetachFromCarrier processing on friendly.");
                if (attachTarget is CustomMech custMech && custMech.FlyingHeight() > 1.5f)
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
                    ModInit.modLog?.Trace?.Write($"DetachFromCarrier call dismount.");
                    //CALL DEFAULT ATTACH CODE
                    squad.DismountBA(attachTarget, Vector3.zero, true, false, false);
                }
            }
            else
            {
                ModInit.modLog?.Trace?.Write($"DetachFromCarrier call dismount.");
                //CALL DEFAULT ATTACH CODE
                squad.DismountBA(attachTarget, Vector3.zero, false, false, false);
            }
        }

        internal class DetachFromCarrierDelegate
        {
            public float detachTargetDownSpeed = -20f;
            public float detachTargetUpSpeed = 5f;
            public CustomMech detachTarget { get; set; }
            public TrooperSquad squad { get; set; }

            public DetachFromCarrierDelegate(TrooperSquad squad, CustomMech target)
            {
                this.squad = squad;
                this.detachTarget = target;
            }

            public void OnLandDetach()
            {
                squad.GameRep.transform.localScale = new Vector3(1f, 1f, 1f);
                squad.GameRep.ToggleHeadlights(true);
                squad.DismountBA(detachTarget, Vector3.zero, true, false, false);
                //ModState.SavedBAScale[squad.GUID];
                //if (ModState.SavedBAScale.ContainsKey(squad.GUID))
                //{
                //    squad.GameRep.transform.localScale = ModState.SavedBAScale[squad.GUID];
                //    ModState.SavedBAScale.Remove(squad.GUID);
                //}
            }

            public void OnRestoreHeightControl()
            {
                detachTarget.custGameRep.HeightController.UpSpeed = detachTargetUpSpeed;
                detachTarget.custGameRep.HeightController.DownSpeed = detachTargetDownSpeed;
            }
        }

        public static void DismountBA(this AbstractActor actor, AbstractActor carrier, Vector3 locationOverride, bool isFriendly, bool calledFromDeswarm = false,
            bool calledFromHandleDeath = false, bool unShrinkRep = true)
        {
            if (actor is TrooperSquad squad)
            {
                //if (squad.StatCollection.ContainsStatistic("irbtmu_immobile_unit")) squad.StatCollection.Set("irbtmu_immobile_unit", false);
                if (ModState.BADamageTrackers.ContainsKey(actor.GUID))
                {
                    if (isFriendly)
                    {
                        if (ModState.BADamageTrackers[actor.GUID].IsSquadInternal)
                        {
                            carrier.ModifyInternalBASquads(-1);
                            ModInit.modLog?.Info?.Write(
                                $"[DismountBA] Dismounted {actor.DisplayName} from internal capacity. Capacity is now {carrier.GetAvailableInternalBASpace()}.");
                            squad.FiringArc(90f); //reset to 90?
                        }
                        else carrier.SetHasExternalMountedBattleArmor(false);
                    }

                    ModState.BADamageTrackers.Remove(actor.GUID);
                }

                var em = actor.Combat.EffectManager;
                foreach (var BA_effect in ModState.BA_MountSwarmEffects)
                {
                    foreach (var effectProper in BA_effect.effects)
                    {
                        ModInit.modLog?.Trace?.Write(
                            $"[DismountBA] DEBUGGINS Checking for {effectProper.Description.Name} with id {effectProper.Description.Id} on {actor.DisplayName}.");
                        var effects = em.GetAllEffectsTargetingWithBaseID(actor, effectProper.Description.Id);

                        for (int i = effects.Count - 1; i >= 0; i--)
                        {
                            ModInit.modLog?.Info?.Write(
                                $"[DismountBA] Cancelling effect on {actor.DisplayName}: {effects[i].EffectData.Description.Name}.");
                            em.CancelEffect(effects[i]);
                        }
                    }
                }

                actor.FiringArc(actor.GetCustomInfo().FiringArc);
                var hud = CameraControl.Instance.HUD;//SharedState.CombatHUD;
                //var hud = Traverse.Create(CameraControl.Instance).Property("HUD").GetValue<CombatHUD>();
                //actor.GameRep.IsTargetable = true;

                ModState.PositionLockMount.Remove(actor.GUID);
                ModState.PositionLockSwarm.Remove(actor.GUID);
                ModState.CachedUnitCoordinates.Remove(carrier.GUID);
                squad.SetCarrier(null, false);
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
                    ModInit.modLog?.Info?.Write($"[DismountBA] Using location override {locationOverride}.");
                }
                
                else if (calledFromDeswarm || calledFromHandleDeath)
                {
                    point = carrier.FetchRandomAdjacentHex();
;                    ModInit.modLog?.Info?.Write($"[DismountBA] Using adjacent hex {point} or fallback carrier loc {carrier.CurrentPosition}.");
                }
                point.y = actor.Combat.MapMetaData.GetLerpedHeightAt(point, false);
                actor.TeleportActor(point);
                if (actor is CustomMech customMech)
                {
                    customMech.custGameRep.j_Root.localRotation = Quaternion.identity;
                }
                actor.GameRep.thisTransform.rotation = carrier.GameRep.thisTransform.rotation;
                actor.CurrentRotation = carrier.CurrentRotation;
                if (!calledFromHandleDeath && !calledFromDeswarm)
                {
                    ModInit.modLog?.Info?.Write($"[DismountBA] Not called from HandleDeath or Deswarm, resetting pathing.");
                    if (actor.team.IsLocalPlayer)
                    {
                        ModInit.modLog?.Info?.Write($"[DismountBA] Local player unit, resetting buttons.");
                        hud.MechWarriorTray.JumpButton.ResetButtonIfNotActive(actor);
                        hud.MechWarriorTray.SprintButton.ResetButtonIfNotActive(actor);
                        hud.MechWarriorTray.MoveButton.ResetButtonIfNotActive(actor);
                        hud.SelectionHandler.AddJumpState(actor);
                        hud.SelectionHandler.AddSprintState(actor);
                        hud.SelectionHandler.AddMoveState(actor);
                    }
                    ModInit.modLog?.Info?.Write(
                        $"[DismountBA] Local player unit, Not called from HandleDeath or Deswarm, resetting buttons and pathing.");
                    actor.ResetPathing(false);
                    actor.Pathing.UpdateCurrentPath(false);
                }
                
                if (false) //(actor.HasBegunActivation)
                {
                    ModInit.modLog?.Info?.Write(
                        $"[DismountBA] Called from handledeath? {calledFromHandleDeath} or Deswarm? {calledFromDeswarm}, forcing end of activation."); // was i trying to end carrier activation maybe?

                    var sequence = actor.DoneWithActor();
                    actor.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(sequence));
                    //actor.OnActivationEnd(actor.GUID, -1);
                }

                //actor.VisibilityCache.UpdateCacheReciprocal(actor.Combat.GetAllLivingCombatants());

                ModInit.modLog?.Info?.Write(
                    $"[DismountBA] Removing PositionLock with rider  {actor.DisplayName} {actor.GUID} and carrier {carrier.DisplayName} {carrier.GUID} and rebuilding visibility cache.");
            }
        }

        public static void DismountGarrison(this TrooperSquad squad, BattleTech.Building building, Vector3 locationOverride,
            bool calledFromHandleDeath = false)
        {
            var em = squad.Combat.EffectManager;
            foreach (var BA_effect in ModState.BA_MountSwarmEffects)
            {
                if (BA_effect.TargetEffectType != Classes.ConfigOptions.BA_TargetEffectType.GARRISON &&
                    BA_effect.TargetEffectType != Classes.ConfigOptions.BA_TargetEffectType.BOTH) continue;

                foreach (var effectProper in BA_effect.effects)
                {
                    var effects = em.GetAllEffectsTargetingWithBaseID(squad, effectProper.Description.Id);
                    for (int i = effects.Count - 1; i >= 0; i--)
                    {
                        ModInit.modLog?.Info?.Write(
                            $"[DismountGarrison] Cancelling effect on {squad.DisplayName}: {effects[i].EffectData.Description.Name}.");
                        em.CancelEffect(effects[i]);
                    }
                }
            }
            squad.FiringArc(squad.GetCustomInfo().FiringArc);
            var hud = CameraControl.Instance.HUD;//SharedState.CombatHUD;
            //var hud = Traverse.Create(CameraControl.Instance).Property("HUD").GetValue<CombatHUD>();
            //actor.GameRep.IsTargetable = true;

            if (ModState.GarrisonFriendlyTeam.ContainsKey(building.GUID))
            {
                if (!ModState.GarrisonFriendlyTeam[building.GUID])
                {
                    building.AddToTeam(squad.Combat.Teams?.FirstOrDefault(x => x?.GUID == "421027ec-8480-4cc6-bf01-369f84a22012")); // add back to world team.
                }
            }
            
            if (!calledFromHandleDeath)
            {
                locationOverride = ModState.PositionLockGarrison[squad.GUID].OriginalSquadPos;
            }
            squad.GameRep.transform.localScale = new Vector3(1f, 1f, 1f);
            squad.GameRep.ToggleHeadlights(true);
            ModState.PositionLockGarrison.Remove(squad.GUID);

            var point = building.CurrentPosition;

            var hk = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            if (!hk)
            {
                if (locationOverride != Vector3.zero)
                {
                    point = locationOverride;
                    ModInit.modLog?.Info?.Write($"[DismountGarrison] Using location override {locationOverride}.");
                }
            }

            point.y = squad.Combat.MapMetaData.GetLerpedHeightAt(point, false);
            squad.TeleportActor(point);

            if (!calledFromHandleDeath)
            {
                ModInit.modLog?.Info?.Write($"[DismountGarrison] Not called from HandleDeath or Deswarm, resetting pathing.");
                if (squad.team.IsLocalPlayer)
                {
                    ModInit.modLog?.Info?.Write($"[DismountGarrison] Local player unit, resetting buttons.");
                    hud.MechWarriorTray.JumpButton.ResetButtonIfNotActive(squad);
                    hud.MechWarriorTray.SprintButton.ResetButtonIfNotActive(squad);
                    hud.MechWarriorTray.MoveButton.ResetButtonIfNotActive(squad);
                    hud.SelectionHandler.AddJumpState(squad);
                    hud.SelectionHandler.AddSprintState(squad);
                    hud.SelectionHandler.AddMoveState(squad);
                }
                squad.ResetPathing(false);
                squad.Pathing.UpdateCurrentPath(false);
            }

            //squad.VisibilityCache.UpdateCacheReciprocal(squad.Combat.GetAllLivingCombatants());

            ModInit.modLog?.Info?.Write(
                $"[DismountGarrison] Removing PositionLock with rider  {squad.DisplayName} {squad.GUID} and carrier {building.DisplayName} {building.GUID} and rebuilding visibility cache.");
            
        }

        public static Vector3 FetchRandomAdjacentHex(this AbstractActor actor)
        {
            var points = actor.Combat.HexGrid.GetAdjacentPointsOnGrid(actor.CurrentPosition);
            var validPoints = new List<Vector3>();
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 resultPos  = Vector3.zero;
                var walkGrid = actor.Pathing.WalkingGrid;//Traverse.Create(actor.Pathing).Property("WalkingGrid").GetValue<PathNodeGrid>();
                var pathNode = walkGrid.GetClosestPathNode(points[i], 0f, 1000f, points[i], ref resultPos,
                    out var resultAngle, false, false);
                if (pathNode != null)
                {
                    var list = walkGrid.BuildPathFromEnd(pathNode, 1000f, resultPos, points[i], null, out var costLeft, out resultPos, out resultAngle);
                    if (list != null && list.Count > 0)
                    {
                        validPoints.Add(resultPos);
                    }
                }
            }
            var point = actor.CurrentPosition;
            point.y = actor.Combat.MapMetaData.GetLerpedHeightAt(point, false);
            if (validPoints.Count > 0)
            {
                point = validPoints.GetRandomElement();
                point.y = actor.Combat.MapMetaData.GetLerpedHeightAt(point, false);
                if (Mathf.Approximately(point.x, actor.CurrentPosition.x) && Mathf.Approximately(point.z, actor.CurrentPosition.z))
                {
                    ModInit.modLog?.Warn?.Write($"[FetchRandomAdjacentHex] Picked same position {point} as current position {actor.CurrentPosition}");
                }
            }
            else
            {
                ModInit.modLog?.Warn?.Write("[FetchRandomAdjacentHex] No valid nearby position found, will plonk on same hex causing a stacked unit.");
                ModInit.modLog?.Warn?.Write($"  Current position is {point}. Adjacent points were: ");
                foreach (Vector3 vector3 in points)
                {
                    ModInit.modLog?.Warn?.Write($"    {vector3}");
                }
            }
            return point;
        }

        public static int GetAvailableInternalBASpace(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<int>("InternalBattleArmorSquadCap") - actor.StatCollection.GetValue<int>("InternalBattleArmorSquads");
        }

        public static string GetBASimLink(this MechDef mechDef)
        {
            foreach (var tag in mechDef.MechTags)
            {
                if (tag.StartsWith(BA_Simlink)) return tag;
            }
            return null;
        }

        public static Ability GetDeswarmerAbilityForAI(this AbstractActor actor, bool UseMovement = false)
        {
            var list = new List<Ability>();

            if (UseMovement)
            {
                if (!string.IsNullOrEmpty(ModInit.modSettings.DeswarmMovementConfig.AbilityDefID))
                {
                    var move = actor.GetPilot().Abilities
                        .FirstOrDefault(x => x.Def.Id == ModInit.modSettings.DeswarmMovementConfig.AbilityDefID) ?? actor.ComponentAbilities
                        .FirstOrDefault(x => x.Def.Id == ModInit.modSettings.DeswarmMovementConfig.AbilityDefID);
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

        public static Vector3 GetEvacBuildingLocation(this AbstractActor squad, BattleTech.Building building)
        {
            //var ogl = ObstructionGameLogic.GetObstructionFromBuilding(building, squad.Combat.ItemRegistry);

            var posToCheck =
                squad.Combat.HexGrid.GetGridPointsAroundPointWithinRadius(building.CurrentPosition,
                    4);
            var evacLoc = squad.team.OffScreenPosition;
            foreach (var pos in posToCheck)
            {
                ModInit.modLog?.Trace?.Write($"[GetEvacBuildingLocation] Checking position {pos}, is {Vector3.Distance(pos, building.CurrentPosition)} from building source {building.CurrentPosition}.");
                var encounterLayerDataCell =
                    squad.Combat.EncounterLayerData.GetCellAt(pos);
                if (encounterLayerDataCell == null) continue;
                
                var eldcBldgs = encounterLayerDataCell.buildingList;
                var foundBuilding = false;
                foreach (var bldg in eldcBldgs)
                {
                    if (bldg.obstructionGameLogic.IsRealBuilding)
                    {
                        foundBuilding = true;
                        ModInit.modLog?.Trace?.Write($"[GetEvacBuildingLocation] Found building {bldg.obstructionGameLogic.buildingDefId}.");
                        break;
                    }
                }

                if (foundBuilding)
                {
                    ModInit.modLog?.Trace?.Write($"[GetEvacBuildingLocation] Found building, continuing.");
                    continue;
                }
                
                if (Vector3.Distance(pos, building.CurrentPosition) <
                    Vector3.Distance(evacLoc, building.CurrentPosition))
                {
                    evacLoc = pos;
                }
            }

            if (evacLoc == squad.team.OffScreenPosition)
            {
                ModInit.modLog?.Trace?.Write($"[GetEvacBuildingLocation] No location found, lerping to roof.");
                evacLoc = building.CurrentPosition;
                evacLoc.y = squad.Combat.MapMetaData.GetLerpedHeightAt(evacLoc);
            }
            
            return evacLoc;
        }

        public static Vector3[] GetGarrisionLOSSourcePositions(this BattleTech.Building sourceBuilding)
        {
            Point buildingPoint = new Point(
                sourceBuilding.Combat.MapMetaData.GetXIndex(sourceBuilding.CurrentPosition.x),
                sourceBuilding.Combat.MapMetaData.GetZIndex(sourceBuilding.CurrentPosition.z));
            MapEncounterLayerDataCell encounterLayerDataCell =
                sourceBuilding.Combat.EncounterLayerData.mapEncounterLayerDataCells[buildingPoint.Z, buildingPoint.X];
            float buildingHeight = encounterLayerDataCell.GetBuildingHeight() * 2f;

            Vector3[] positions = new Vector3[5];
            var pos1 = new Vector3(-10f, buildingHeight, -10f);
            var pos2 = new Vector3(10f, buildingHeight, -10f);
            var pos3 = new Vector3(0f, buildingHeight, 0f);
            var pos4 = new Vector3(-10f, buildingHeight, 10f);
            var pos5 = new Vector3(10f, buildingHeight, 10f);

            positions[0] = pos1;
            positions[1] = pos2;
            positions[2] = pos3;
            positions[3] = pos4;
            positions[4] = pos5;

            return positions;
        }

        public static bool GetHasBattleArmorMounts(this AbstractActor actor)
        {
            return actor != null && actor.StatCollection.GetValue<bool>("HasBattleArmorMounts");
        }

        public static bool GetHasExternalMountedBattleArmor(this AbstractActor actor)
        {
            return actor != null && actor.StatCollection.GetValue<bool>("HasExternalMountedBattleArmor");
        }

        public static int GetInternalBACap(this AbstractActor actor)
        {
            return actor == null ? 0 : actor.StatCollection.GetValue<int>("InternalBattleArmorSquadCap");
        }

        public static int GetInternalBASquads(this AbstractActor actor)
        {
            return actor == null ? 0 : actor.StatCollection.GetValue<int>("InternalBattleArmorSquads");
        }

        public static bool GetIsBattleArmorHandsy(this AbstractActor actor)
        {
            return actor != null && actor.StatCollection.GetValue<bool>("IsBattleArmorHandsy");
        }

        public static bool GetIsUnMountable(this AbstractActor actor)
        {
            return actor != null && actor.StatCollection.GetValue<bool>("IsUnmountableBattleArmor");
        }

        public static bool GetIsUnSwarmable(this AbstractActor actor)
        {
            return actor != null && (actor is TrooperSquad || actor.StatCollection.GetValue<bool>("IsUnswarmableBattleArmor"));
        }

        public static LineOfFireLevel GetLineOfFireForGarrison(this ICombatant source, AbstractActor garrisonSquad,
            Vector3 sourcePosition,
            ICombatant target, Vector3 targetPosition, Quaternion targetRotation, out Vector3 collisionWorldPos)
        {
            collisionWorldPos = targetPosition;
            if (source is BattleTech.Building building)
            {
                Vector3 forward = targetPosition - sourcePosition;
                forward.y = 0f;
                Quaternion rotation = Quaternion.LookRotation(forward);
                //Vector3[] lossourcePositions = source.GetLOSTargetPositions(sourcePosition, rotation);

                Vector3[] lossourcePositions = building.GetGarrisionLOSSourcePositions();

                    Vector3[] lostargetPositions = target.GetLOSTargetPositions(targetPosition, targetRotation);
                List<AbstractActor> list = new List<AbstractActor>(source.Combat.GetAllLivingActors());
                list.Remove(garrisonSquad);
                AbstractActor abstractActor = target as AbstractActor;
                string text = null;
                if (abstractActor != null)
                {
                    list.Remove(abstractActor);
                }
                else
                {
                    text = target.GUID;
                }

                LineSegment lineSegment = new LineSegment(sourcePosition, targetPosition);
                list.Sort((AbstractActor x, AbstractActor y) => Vector3.SqrMagnitude(x.CurrentPosition - sourcePosition)
                    .CompareTo(Vector3.SqrMagnitude(y.CurrentPosition - sourcePosition)));
                float num = Vector3.SqrMagnitude(sourcePosition - targetPosition);
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i].IsDead || Vector3.SqrMagnitude(list[i].CurrentPosition - sourcePosition) > num ||
                        lineSegment.DistToPoint(list[i].CurrentPosition) > list[i].Radius * 5f)
                    {
                        list.RemoveAt(i);
                    }
                }

                float num2 = 0f;
                float num3 = 0f;
                float num4 = 0f;
                float num5 = 999999.9f;
                Weapon longestRangeWeapon = garrisonSquad.GetLongestRangeWeapon(false, false);
                float num6 = (longestRangeWeapon == null) ? 0f : longestRangeWeapon.MaxRange;
                float adjustedSpotterRange =
                    garrisonSquad.Combat.LOS.GetAdjustedSpotterRange(garrisonSquad, abstractActor);
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
                                        vector = NvMath.NearestPointStrict(lossourcePositions[j], lostargetPositions[k],
                                            list[l].CurrentPosition);
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

                            if (source.Combat.LOS.HasLineOfFire(lossourcePositions[j], lostargetPositions[k], text,
                                    num6, out vector))
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
                    return LineOfFireLevel.LOFClear;
                }

                if (num9 >= source.Combat.Constants.Visibility.RatioObstructedVis)
                {
                    return LineOfFireLevel.LOFObstructed;
                }

                return LineOfFireLevel.LOFBlocked;
            }
            return LineOfFireLevel.LOFBlocked;
        }

        public static float GetMovementDeSwarmEvasiveJumpMovementMultiplier(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<float>("MovementDeSwarmEvasiveJumpMovementMultiplier");
        }

        public static float GetMovementDeSwarmEvasivePipsFactor(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<float>("MovementDeSwarmEvasivePipsFactor");
        }

        public static float GetMovementDeSwarmMaxChance(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<float>("MovementDeSwarmMaxChance");
        }

        public static float GetMovementDeSwarmMinChance(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<float>("MovementDeSwarmMinChance");
        }

        public static int GetTotalBASpaceMechDef(this MechDef mechDef)
        {
            var capacity = 0;
            var parsedExt = false;
            foreach (var item in mechDef.Inventory)
            {
                foreach (var effectData in item.Def.statusEffects)
                {
                    if (effectData?.statisticData?.statName == "InternalBattleArmorSquadCap")
                    {
                        if (int.TryParse(effectData.statisticData?.modValue, out var space)) capacity += space;
                    }

                    if (effectData?.statisticData?.statName == "HasBattleArmorMounts" || (ModState.PendingPairBAUnit != null && ModState.PendingPairBAUnit.SelectedMech.MechDef.CanMountBADef()))
                    {
                        if (parsedExt) continue;
                        capacity += 1;
                        parsedExt = true;
                    }
                }
            }
            return capacity;
        }
        public static bool HasBattleArmorMounts(this MechDef mechDef, Contract contract)
        {
            if(contract != null) if (ModInit.modSettings.forbidCarrierContractTypes.Contains(contract.ContractTypeValue.Name)) { return false; }
            foreach (var item in mechDef.Inventory)
            {
                foreach (var effectData in item.Def.statusEffects)
                {

                    if (effectData?.statisticData?.statName == "HasBattleArmorMounts")
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static int CargoCapacity(this MechDef mechDef, Contract contract)
        {
            if (contract != null) if (ModInit.modSettings.forbidCarrierContractTypes.Contains(contract.ContractTypeValue.Name)) { return 0; }
            var capacity = 0;
            foreach (var item in mechDef.Inventory)
            {
                foreach (var effectData in item.Def.statusEffects)
                {

                    if (effectData?.statisticData?.statName == "InternalBattleArmorSquadCap")
                    {
                        if (int.TryParse(effectData.statisticData?.modValue, out var space)) capacity += space;
                    }
                }
            }
            return capacity;
        }
        public static bool CanMountBAExternally(this MechDef mechDef, Contract contract)
        {
            if (contract != null) if (ModInit.modSettings.forbidCarrierContractTypes.Contains(contract.ContractTypeValue.Name)) { return false; }
            UnitCustomInfo info = mechDef.GetCustomInfo();
            if (info == null) { return true; }
            if (info.SquadInfo.Troopers > 1) { return false; }
            return true;
        }
        public static bool isBattleArmorInternalMountsOnly(this MechDef mechDef, Contract contract)
        {
            if (contract != null) if (ModInit.modSettings.forbidCarrierContractTypes.Contains(contract.ContractTypeValue.Name)) { return true; }
            var internalOnly = false;
            foreach (var item in mechDef.Inventory)
            {
                foreach (var effectData in item.Def.statusEffects)
                {
                    if (effectData?.statisticData?.statName == "BattleArmorInternalMountsOnly")
                    {
                        if (bool.TryParse(effectData.statisticData?.modValue, out internalOnly)) ;
                    }
                }
            }
            return internalOnly;
        }

        public static bool CanMountBADef(this MechDef mechDef)
        {
            var canMount = false;
            foreach (var item in mechDef.Inventory)
            {
                foreach (var effectData in item.Def.statusEffects) 
                {
                    if (effectData?.statisticData?.statName == "IsBattleArmorHandsy")
                    {
                        if (bool.TryParse(effectData.statisticData?.modValue, out canMount)) ;
                    }
                }
            }
            return canMount;
        }
        public static bool HaveMountAbility(this MechDef mechDef)
        {
            foreach (var item in mechDef.Inventory)
            {
                foreach (var effectData in item.Def.statusEffects)
                {
                    if (effectData == null) { continue; }
                    if (effectData.effectType != EffectType.ActiveAbility) { continue; }
                    if (effectData.activeAbilityEffectData == null) { continue; }
                    if (effectData.activeAbilityEffectData.abilityName == ModInit.modSettings.BattleArmorMountAndSwarmID) { return true; }
                }
            }
            return false;
        }

        public static bool CanTransportSquad(MechDef transport, MechDef squad, out string error)
        {
            var capacity = 0;
            var parsedExt = false;
            foreach (var item in transport.Inventory)
            {
                foreach (var effectData in item.Def.statusEffects)
                {
                    if (effectData?.statisticData?.statName == "InternalBattleArmorSquadCap")
                    {
                        if (int.TryParse(effectData.statisticData?.modValue, out var space)) capacity += space;
                    }

                    if (effectData?.statisticData?.statName == "HasBattleArmorMounts" && squad.CanMountBADef())
                    {
                        if (parsedExt) continue;
                        capacity += 1;
                        parsedExt = true;
                    }
                }
            }

            if (capacity > 0)
            {
                error = "";
                return true;
            }
            error = ModInit.modSettings.SimBattleArmorMountError;
            return false;
        }

        public static void HandleBattleArmorFallingDamage(this TrooperSquad squad)
        {
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

        public static bool HasFiringPorts(this AbstractActor actor)
        {
            return actor != null && actor.StatCollection.GetValue<bool>("HasFiringPorts");
        }

        public static bool HasGarrisonedUnits(this BattleTech.Building building)
        {
            return building != null && ModState.PositionLockGarrison.Any(x=>x.Value.BuildingGUID == building.GUID);
        }

        public static bool HasMountedUnits(this AbstractActor actor)
        {
            return actor != null && ModState.PositionLockMount.ContainsValue(actor.GUID);
        }

        public static bool HasSwarmingUnits(this AbstractActor actor)
        {
            return actor != null && ModState.PositionLockSwarm.ContainsValue(actor.GUID);
        }

        public static bool IsAvailableBAAbility(this Ability ability)
        {
            var flag = true;
            if (ability.parentComponent != null)
            {
                flag = ability.parentComponent.IsFunctional;
                ModInit.modLog?.Trace?.Write($"[IsAvailableBAAbility] - {ability.parentComponent.parent.DisplayName} has parentComponent for ability {ability.Def.Description.Name}. Component functional? {flag}.");
                if (!flag)
                {
                    if (ability.parentComponent.parent.ComponentAbilities.Any(x =>
                        x.parentComponent.IsFunctional && x.Def.Id == ability.Def.Id))
                    {
                        flag = true;
                        ModInit.modLog?.Trace?.Write($"[IsAvailableBAAbility] - {ability.parentComponent.parent.DisplayName} has other component with same ability {ability.Def.Description.Name}. Component functional? {flag}.");
                    }
                }
            }
            return ability.CurrentCooldown < 1 && (ability.Def.NumberOfUses < 1 || ability.NumUsesLeft > 0) && flag;
        } // need to redo Ability.Activate from start, completely override for BA? Or just put ability on hidden componenet and ignore this shit.

        public static bool IsGarrisoned(this AbstractActor actor)
        {
            return actor != null && ModState.PositionLockGarrison.ContainsKey(actor.GUID);
        }

        public static bool IsGarrisonedInTargetBuilding(this AbstractActor actor, BattleTech.Building building)
        {
            return actor != null && building != null && ModState.PositionLockGarrison.ContainsKey(actor.GUID) && ModState.PositionLockGarrison[actor.GUID].BuildingGUID == building.GUID;
        }

        public static bool IsMountedInternal(this AbstractActor actor)
        {
            return actor != null && ModState.BADamageTrackers.ContainsKey(actor.GUID) && ModState.BADamageTrackers[actor.GUID].IsSquadInternal;
        }

        public static bool IsMountedToUnit(this AbstractActor actor, AbstractActor target)
        {
            return actor != null && target != null && ModState.PositionLockMount.ContainsKey(actor.GUID) && ModState.PositionLockMount[actor.GUID] == target.GUID;
        }

        public static bool IsMountedUnit(this AbstractActor actor)
        {
            return actor != null && ModState.PositionLockMount.ContainsKey(actor.GUID);
        }

        public static bool IsSwarmingTargetUnit(this AbstractActor actor, AbstractActor target)
        {
            return actor != null && target != null && ModState.PositionLockSwarm.ContainsKey(actor.GUID) && ModState.PositionLockSwarm[actor.GUID] == target.GUID;
        }

        public static bool IsSwarmingUnit(this AbstractActor actor)
        {
            return actor != null && ModState.PositionLockSwarm.ContainsKey(actor.GUID);
        }

        public static void ModifyInternalBASquads(this AbstractActor actor, int value)
        {
            actor.StatCollection.ModifyStat("BAMountDismount", -1, "InternalBattleArmorSquads", StatCollection.StatOperation.Int_Add, value);
        }

        public static void MountBattleArmorToChassis(this AbstractActor carrier, AbstractActor battleArmor, bool shrinkRep, bool isFriendly)
        {
            battleArmor.TeleportActor(carrier.CurrentPosition);
            var isPlayer = battleArmor.team.IsLocalPlayer;
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

                if (isFriendly)
                {
                    ModState.PositionLockMount.Add(battleArmor.GUID, carrier.GUID);

                    var internalCap = carrier.GetInternalBACap();
                    var currentInternalSquads = carrier.GetInternalBASquads();
                    var hud = CameraControl.Instance.HUD;
                    if (currentInternalSquads < internalCap)
                    {
                        ModInit.modLog?.Info?.Write($"[MountBattleArmorToChassis] - target unit {carrier.DisplayName} has internal BA capacity of {internalCap}. Currently used: {currentInternalSquads}, mounting squad internally.");
                        carrier.ModifyInternalBASquads(1);
                        tracker.IsSquadInternal = true;
                        // try and set firing arc to 360?
                        battleArmor.FiringArc(360f);
                        //refresh firing button state to make sure firing ports are respected
                        if (isPlayer && battleArmor == hud.selectedUnit && !carrier.HasFiringPorts())
                        {
                            CameraControl.Instance.HUD.MechWarriorTray.FireButton.DisableButton();
                        }
                    }

                    foreach (var BA_Effect in ModState.BA_MountSwarmEffects)
                    {
                        if (BA_Effect.TargetEffectType == Classes.ConfigOptions.BA_TargetEffectType.BOTH)
                        {
                            foreach (var effectData in BA_Effect.effects)
                            {
                                battleArmor.CreateEffect(effectData, null, effectData.Description.Id, -1, battleArmor);
                            }
                        }
                        if (BA_Effect.TargetEffectType == Classes.ConfigOptions.BA_TargetEffectType.MOUNT_EXT && !tracker.IsSquadInternal)
                        {
                            foreach (var effectData in BA_Effect.effects)
                            {
                                battleArmor.CreateEffect(effectData, null, effectData.Description.Id, -1, battleArmor);
                            }
                        }
                        if (BA_Effect.TargetEffectType == Classes.ConfigOptions.BA_TargetEffectType.MOUNT_INT && tracker.IsSquadInternal)
                        {
                            foreach (var effectData in BA_Effect.effects)
                            {
                                battleArmor.CreateEffect(effectData, null, effectData.Description.Id, -1, battleArmor);
                            }
                        }
                        if (BA_Effect.TargetEffectType == Classes.ConfigOptions.BA_TargetEffectType.MOUNTTARGET ||
                            BA_Effect.TargetEffectType == Classes.ConfigOptions.BA_TargetEffectType.BOTHTARGET)
                        {
                            foreach (var effectData in BA_Effect.effects)
                            {
                                carrier.CreateEffect(effectData, null, effectData.Description.Id, -1, battleArmor);
                            }
                        }
                    }

                    if (battleArmor is TrooperSquad squad) { squad.SetCarrier(carrier, !tracker.IsSquadInternal); }
                    if (tracker.IsSquadInternal) return;
                    carrier.SetHasExternalMountedBattleArmor(true);
                }
                else
                {
                    if (battleArmor is TrooperSquad squad) { squad.SetCarrier(carrier, true); }
                    ModState.PositionLockSwarm.Add(battleArmor.GUID, carrier.GUID);
                }


                foreach (ChassisLocations battleArmorChassisLoc in Enum.GetValues(typeof(ChassisLocations)))
                {
                    var locationDef = battleArmorAsMech.MechDef.Chassis.GetLocationDef(battleArmorChassisLoc);

                    var statname = battleArmorAsMech.GetStringForStructureDamageLevel(battleArmorChassisLoc);
                    if (string.IsNullOrEmpty(statname) || !battleArmorAsMech.StatCollection.ContainsStatistic(statname)) continue;
                    if (battleArmorAsMech.GetLocationDamageLevel(battleArmorChassisLoc) == LocationDamageLevel.Destroyed) continue; // why did i do this?

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
                                    if (!tracker.BA_MountedLocations.ContainsKey((int)battleArmorChassisLoc))
                                    {
                                        ModInit.modLog?.Info?.Write($"[MountBattleArmorToChassis] - mounting BA {battleArmorChassisLoc} to enemy mech location {tgtMechLoc}.");
                                        tracker.BA_MountedLocations.Add((int)battleArmorChassisLoc, (int)tgtMechLoc);
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
                                    if (!tracker.BA_MountedLocations.ContainsKey((int)battleArmorChassisLoc))
                                    {
                                        ModInit.modLog?.Info?.Write($"[MountBattleArmorToChassis] - mounting BA {battleArmorChassisLoc} to friendly mech location {tgtMechLoc}.");
                                        tracker.BA_MountedLocations.Add((int)battleArmorChassisLoc, (int)tgtMechLoc);
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
                                if (!tracker.BA_MountedLocations.ContainsKey((int)battleArmorChassisLoc))
                                {
                                    ModInit.modLog?.Info?.Write($"[MountBattleArmorToChassis] - mounting BA {battleArmorChassisLoc} to vehicle location {tgtVicLoc}.");
                                    tracker.BA_MountedLocations.Add((int)battleArmorChassisLoc, (int)tgtVicLoc);
                                    break;
                                }
                                hasLoopedOnce = true;
                                goto loopagain;
                            }
                        }
                        else if (carrier is Turret turret)
                        {
                            ModInit.modLog?.Info?.Write($"[MountBattleArmorToChassis] - mounting BA {battleArmorChassisLoc} to turret location {BuildingLocation.Structure}.");
                            tracker.BA_MountedLocations.Add((int)battleArmorChassisLoc, (int)BuildingLocation.Structure);
                        }
                        else
                        {
                            //borken
                        }
                    }
                }
                if (ModInit.modSettings.ReworkedCarrierEvasion) battleArmor.MountedEvasion(carrier);
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
                            ModInit.modLog?.Trace?.Write(
                                $"[ProcessBattleArmorSpawnWeights - InternalBattleArmorWeight] Processing spawn weights for {BaTypeInternal.Key} and weight {BaTypeInternal.Value}");
                            for (int i = 0; i < BaTypeInternal.Value; i++)
                            {
                                ModState.CachedFactionAssociations[faction]["InternalBattleArmorWeight"]
                                    .Add(BaTypeInternal.Key);
                                ModInit.modLog?.Trace?.Write(
                                    $"[ProcessBattleArmorSpawnWeights - InternalBattleArmorWeight] spawn list has {ModState.CachedFactionAssociations[faction]["InternalBattleArmorWeight"].Count} entries");
                            }
                        }
                    }
                    foreach (var BaTypeMount in BaWgts.MountedBattleArmorWeight)
                    {
                        if (dm.Exists(BattleTechResourceType.MechDef, BaTypeMount.Key) || BaTypeMount.Key == "BA_EMPTY")
                        {
                            ModInit.modLog?.Trace?.Write(
                                $"[ProcessBattleArmorSpawnWeights - MountedBattleArmorWeight] Processing spawn weights for {BaTypeMount.Key} and weight {BaTypeMount.Value}");
                            for (int i = 0; i < BaTypeMount.Value; i++)
                            {
                                ModState.CachedFactionAssociations[faction]["MountedBattleArmorWeight"]
                                    .Add(BaTypeMount.Key);
                                ModInit.modLog?.Trace?.Write(
                                    $"[ProcessBattleArmorSpawnWeights - MountedBattleArmorWeight] spawn list has {ModState.CachedFactionAssociations[faction]["MountedBattleArmorWeight"].Count} entries");
                            }
                        }
                    }

                    foreach (var BaTypeHandsy in BaWgts.HandsyBattleArmorWeight)
                    {
                        if (dm.Exists(BattleTechResourceType.MechDef, BaTypeHandsy.Key) || BaTypeHandsy.Key == "BA_EMPTY")
                        {
                            ModInit.modLog?.Trace?.Write(
                                $"[ProcessBattleArmorSpawnWeights - HandsyBattleArmorWeight] Processing spawn weights for {BaTypeHandsy.Key} and weight {BaTypeHandsy.Value}");
                            for (int i = 0; i < BaTypeHandsy.Value; i++)
                            {
                                ModState.CachedFactionAssociations[faction]["HandsyBattleArmorWeight"]
                                    .Add(BaTypeHandsy.Key);
                                ModInit.modLog?.Trace?.Write(
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
            ModInit.modLog?.Error?.Write($"[ProcessBattleArmorSpawnWeights] no applicable config for this unit, returning empty list.");
            return "";
        }

        public static void ProcessDeswarmMovement(this AbstractActor creator, List<KeyValuePair<string, string>> swarmingUnits)
        {
            var DeswarmMovementInfo = new Classes.BA_DeswarmMovementInfo(creator);
            ModInit.modLog?.Trace?.Write($"[ProcessDeswarmMovement] - {DeswarmMovementInfo.Carrier.DisplayName} set to {creator.DisplayName}.");
            foreach (var swarmingUnit in swarmingUnits)
            {
                var swarmingUnitActor = creator.Combat.FindActorByGUID(swarmingUnit.Key);
                DeswarmMovementInfo.SwarmingUnits.Add(swarmingUnitActor);
                ModInit.modLog?.Trace?.Write($"[ProcessDeswarmMovement] - Added {swarmingUnitActor.DisplayName} to list of swarming.");
            }
            ModState.DeSwarmMovementInfo = DeswarmMovementInfo;
            ModInit.modLog?.Trace?.Write($"[ProcessDeswarmMovement] - Set modstate.");
        }

        public static void ProcessDeswarmRoll(this AbstractActor creator, List<KeyValuePair<string, string>> swarmingUnits)
        {
            var finalChance = 0f;

            var settings = ModInit.modSettings.DeswarmConfigs.ContainsKey(ModInit.modSettings.BattleArmorDeSwarmRoll)
                ? ModInit.modSettings.DeswarmConfigs[ModInit.modSettings.BattleArmorDeSwarmRoll]
                : new Classes.ConfigOptions.BA_DeswarmAbilityConfig();
            //var rollInitPenalty = creator.StatCollection.GetValue<int>("BattleArmorDeSwarmerRollInitPenalty");
            var rollInitPenalty = settings.InitPenalty;
            if (!creator.team.IsLocalPlayer)
            {
                //var baseChance = creator.StatCollection.GetValue<float>("BattleArmorDeSwarmerRoll");//0.5f;
                var baseChance = settings.BaseSuccessChance;
                var pilotSkill = creator.GetPilot().Piloting;
                finalChance = Mathf.Min(baseChance + (0.05f * pilotSkill), settings.MaxSuccessChance);
                ModInit.modLog?.Info?.Write($"[Ability.Activate - BattleArmorDeSwarm] Deswarm chance: {finalChance} from baseChance {baseChance} + pilotSkill x 0.05 {0.05f * pilotSkill}, max 0.95.");
            }
            else
            {
                finalChance = ModState.DeSwarmSuccessChance;
                ModInit.modLog?.Info?.Write($"[Ability.Activate - BattleArmorDeSwarm] restored deswarm roll chance from state: {ModState.DeSwarmSuccessChance}");
            }
            var roll = ModInit.Random.NextDouble();
            foreach (var swarmingUnit in swarmingUnits)
            {
                var swarmingUnitActor = creator.Combat.FindActorByGUID(swarmingUnit.Key);
                if (swarmingUnitActor is TrooperSquad swarmingUnitSquad)
                {
                    if (roll <= finalChance)
                    {
                        ModInit.modLog?.Info?.Write(
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
                            swarmingUnitActor.DismountBA(creator, Vector3.zero, false, true);
                            ModInit.modLog?.Info?.Write(
                                $"[Ability.Activate - DestroyBA on Roll] SUCCESS: {destroyBARoll} <= {finalChance}.");
                            var trooperLocs = swarmingUnitActor.GetPossibleHitLocations(creator);
                            var damages = BattleArmorUtils.CalculateClusterDamages(settings.TotalDamage,
                                settings.Clusters, trooperLocs,
                                out var locs);

                            for (int i = 0; i < damages.Length; i++)
                            {
                                var hitinfo = new WeaponHitInfo(-1, -1, 0, 0, creator.GUID, swarmingUnitActor.GUID, 1,
                                    new float[1], new float[1], new float[1], new bool[1], new int[locs[i]], new int[1],
                                    new AttackImpactQuality[1], new AttackDirection[1], new Vector3[1], new string[1],
                                    new int[locs[i]]);
                                swarmingUnitActor.TakeWeaponDamage(hitinfo, locs[i], swarmingUnitSquad.MeleeWeapon,
                                    damages[i], 0, 0, DamageType.Melee);
                            }

                            if (swarmingUnitSquad.IsFlaggedForDeath)
                                //swarmingUnitActor.FlagForDeath("Squished", DeathMethod.VitalComponentDestroyed, DamageType.Melee, 0, -1, creator.GUID, false);
                                swarmingUnitActor.HandleDeath(creator.GUID);
                        }
                        else
                        {
                            ModInit.modLog?.Info?.Write(
                                $"[Ability.Activate - DestroyBA on Roll] FAILURE: {destroyBARoll} > {finalChance}.");
                            swarmingUnitActor.DismountBA(creator, Vector3.zero, false, true);
                        }
                    }
                }
                else
                {
                    var txt = new Text("Remove Swarming Battle Armor: FAILURE");
                    creator.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(
                        new ShowActorInfoSequence(creator, txt, FloatieMessage.MessageNature.Buff,
                            false)));
                    ModInit.modLog?.Info?.Write(
                            $"[Ability.Activate - BattleArmorDeSwarm] Deswarm FAILURE: {roll} > {finalChance}.");
                }
            }
        }

        public static void ProcessDeswarmSwat(this AbstractActor creator,
            List<KeyValuePair<string, string>> swarmingUnits)
        {
            var finalChance = 0f;
            //var swatInitPenalty = creator.StatCollection.GetValue<int>("BattleArmorDeSwarmerSwatInitPenalty");
            var settings = ModInit.modSettings.DeswarmConfigs.ContainsKey(ModInit.modSettings.BattleArmorDeSwarmRoll)
                ? ModInit.modSettings.DeswarmConfigs[ModInit.modSettings.BattleArmorDeSwarmRoll]
                : new Classes.ConfigOptions.BA_DeswarmAbilityConfig();
            var swatInitPenalty = settings.InitPenalty;
            if (!creator.team.IsLocalPlayer)
            {
                //var baseChance = creator.StatCollection.GetValue<float>("BattleArmorDeSwarmerSwat"); //0.5f;//0.3f;
                var baseChance = settings.BaseSuccessChance;
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

                finalChance = Mathf.Min(baseChance + (0.05f * pilotSkill) - (0.05f * missingActuatorCount), settings.MaxSuccessChance);
                ModInit.modLog?.Info?.Write(
                    $"[Ability.Activate - BattleArmorDeSwarm] Deswarm chance: {finalChance} from baseChance {baseChance} + pilotSkill x 0.05 {0.05f * pilotSkill} - missingActuators x 0.05 {0.05f * missingActuatorCount}.");
            }
            else
            {
                finalChance = ModState.DeSwarmSuccessChance;
                ModInit.modLog?.Info?.Write($"[Ability.Activate - BattleArmorDeSwarm] restored deswarm swat chance from state: {ModState.DeSwarmSuccessChance}");
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
                    ModInit.modLog?.Info?.Write($"[Ability.Activate - BattleArmorDeSwarm] Deswarm SUCCESS: {roll} <= {finalChance}.");
                    for (int i = 0; i < swatInitPenalty; i++)
                    {
                        swarmingUnitActor.ForceUnitOnePhaseDown(creator.GUID, -1, false);
                    }
                    swarmingUnitActor.DismountBA(creator, Vector3.zero, false, true);
                    var dmgRoll = ModInit.Random.NextDouble();
                    if (dmgRoll <= finalChance)
                    {
                        if (swarmingUnitActor is TrooperSquad swarmingUnitAsSquad)
                        {
                            var trooperLocs = swarmingUnitActor.GetPossibleHitLocations(creator);
                            var damages = BattleArmorUtils.CalculateClusterDamages(settings.TotalDamage, settings.Clusters, trooperLocs,
                                out var locs);

                            //var baLoc = swarmingUnitAsSquad.GetPossibleHitLocations(creator).GetRandomElement();
                            for (int i = 0; i < damages.Length; i++)
                            {
                                ModInit.modLog?.Info?.Write(
                                    $"[Ability.Activate - BattleArmorDeSwarm] BA Armor Damage Location {locs[i]}: {swarmingUnitAsSquad.GetStringForArmorLocation((ArmorLocation) locs[i])}");
                                //var swatDmg = creator.StatCollection.GetValue<float>("BattleArmorDeSwarmerSwatDamage");

                                var hitinfo = new WeaponHitInfo(-1, -1, 0, 0, creator.GUID, swarmingUnitActor.GUID, 1,
                                    new float[1], new float[1], new float[1], new bool[1], new int[locs[i]], new int[1],
                                    new AttackImpactQuality[1], new AttackDirection[1], new Vector3[1], new string[1],
                                    new int[locs[i]]);
                                swarmingUnitActor.TakeWeaponDamage(hitinfo, locs[i], swarmingUnitAsSquad.MeleeWeapon,
                                    damages[i], 0, 0, DamageType.Melee);
                            }
                            if (swarmingUnitActor.IsFlaggedForDeath) swarmingUnitActor.HandleDeath(creator.GUID);
                        }
                    }
                }
                else
                {
                    var txt = new Text("Remove Swarming Battle Armor: FAILURE");
                    creator.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(
                        new ShowActorInfoSequence(creator, txt, FloatieMessage.MessageNature.Buff,
                            false)));
                    ModInit.modLog?.Info?.Write(
                        $"[Ability.Activate - BattleArmorDeSwarm] Deswarm FAILURE: {roll} > {finalChance}. Doing nothing and ending turn!");
                }
            }
        }

        public static void ProcessGarrisonBuilding(this TrooperSquad creator, BattleTech.Building targetBuilding)
        {
            var creatorActor = creator as AbstractActor;
            ModState.PositionLockGarrison.Add(creator.GUID, new Classes.BA_GarrisonInfo(targetBuilding, creator));
            foreach (var BA_Effect in ModState.BA_MountSwarmEffects)
            {
                if (BA_Effect.TargetEffectType == Classes.ConfigOptions.BA_TargetEffectType.BOTH)
                {
                    foreach (var effectData in BA_Effect.effects)
                    {
                        creator.CreateEffect(effectData, null,
                            effectData.Description.Id,
                            -1, creatorActor);
                    }
                }
                if (BA_Effect.TargetEffectType == Classes.ConfigOptions.BA_TargetEffectType.GARRISON)
                {
                    foreach (var effectData in BA_Effect.effects)
                    {
                        creator.CreateEffect(effectData, null,
                            effectData.Description.Id,
                            -1, creatorActor);
                    }
                }
            }
            var pos = targetBuilding.CurrentPosition;
            var buildingLerpedHeight = creator.Combat.MapMetaData.GetLerpedHeightAt(pos);
            var buildingHeight = buildingLerpedHeight - creator.Combat.MapMetaData.GetLerpedHeightAt(pos, true);
            var setHeight = buildingHeight * 0.3f;
            pos.y = buildingLerpedHeight - setHeight;
            creator.TeleportActor(pos);

            creator.GameRep.transform.localScale = new Vector3(.01f, .01f, .01f);
            creator.FiringArc(360f);
           // var alliedTeam = 
            //squad.team.SupportTeam.AddCombatant(targetBuilding);

            if (!targetBuilding.team.IsFriendly(creator.team))
            {
                targetBuilding.AddToTeam(creator.team.SupportTeam);
                ModState.GarrisonFriendlyTeam.Add(targetBuilding.GUID, false);
            }
            //targetBuilding.BuildingRep.IsTargetable = true;
            //targetBuilding.BuildingRep.SetHighlightColor(creator.Combat, creator.team);
            //targetBuilding.BuildingRep.RefreshEdgeCache();
            creator.GameRep.ToggleHeadlights(false);

            var additionalBldgStructure = (creator.SummaryArmorCurrent + creator.SummaryStructureCurrent) *
                                          ModInit.modSettings.GarrisonBuildingArmorFactor;
            targetBuilding.StatCollection.Set("Structure", targetBuilding.CurrentStructure + additionalBldgStructure);

            ModInit.modLog?.Info?.Write(
                $"[ProcessGarrisonBuilding] Added garrision info with unit {creator.DisplayName} {creator.GUID} and building {targetBuilding.DisplayName} {targetBuilding.GUID} at position {targetBuilding.CurrentPosition}. Target building gained {additionalBldgStructure} structure due to garrison");

            if (creator.team.IsLocalPlayer)
            {
                var sequence = creator.DoneWithActor();
                creator.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(sequence));
            }
        }

        public static void ProcessMountFriendly(this AbstractActor creator, AbstractActor targetActor)
        {
            if (creator is TrooperSquad squad)
            {
                squad.AttachToCarrier(targetActor, true);
                ModInit.modLog?.Trace?.Write($"[Ability.Activate - BattleArmorMountID] Called AttachToCarrier.");
            }
            ModInit.modLog?.Info?.Write(
                $"[Ability.Activate - BattleArmorMountID] Added PositionLockMount with rider  {creator.DisplayName} {creator.GUID} and carrier {targetActor.DisplayName} {targetActor.GUID}.");

            if (creator.team.IsLocalPlayer && false)
            {
                var sequence = creator.DoneWithActor();
                creator.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(sequence));
            }
        }

        public static void ProcessSwarmEnemy(this Mech creatorMech, AbstractActor targetActor)
        {
            var creatorActor = creatorMech as AbstractActor;
            if (!creatorMech.CanSwarm() && creatorMech.team.IsLocalPlayer)
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
            ModInit.modLog?.Info?.Write($"[Ability.Activate - BattleArmorSwarmID] Rolling simplified melee: roll {roll} vs hitChance {meleeChance}; chance in Modstate was {ModState.SwarmSuccessChance}.");
            if (roll <= meleeChance)
            {
                foreach (var BA_Effect in ModState.BA_MountSwarmEffects)
                {
                    if (BA_Effect.TargetEffectType == Classes.ConfigOptions.BA_TargetEffectType.SWARM ||
                        BA_Effect.TargetEffectType == Classes.ConfigOptions.BA_TargetEffectType.BOTH)
                    {
                        foreach (var effectData in BA_Effect.effects)
                        {
                            creatorActor.CreateEffect(effectData, null, effectData.Description.Id, -1, creatorActor);
                        }
                    }
                    if (BA_Effect.TargetEffectType == Classes.ConfigOptions.BA_TargetEffectType.SWARMTARGET || 
                        BA_Effect.TargetEffectType == Classes.ConfigOptions.BA_TargetEffectType.BOTHTARGET)
                    {
                        foreach (var effectData in BA_Effect.effects)
                        {
                            targetActor.CreateEffect(effectData, null, effectData.Description.Id, -1, creatorActor);
                        }
                    }
                }

                var txt = new Text("Swarm Attack: SUCCESS");
                creatorMech.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(
                    new ShowActorInfoSequence(creatorMech, txt, FloatieMessage.MessageNature.Buff,
                        false)));

                ModInit.modLog?.Info?.Write(
                    $"[Ability.Activate - BattleArmorSwarmID] Cleaning up dummy attacksequence.");

                //creator.GameRep.IsTargetable = false;
                creatorMech.TeleportActor(targetActor.CurrentPosition);

                //creator.GameRep.enabled = false;
                //creator.GameRep.gameObject.SetActive(false); //this might be the problem with attacking.
                //creator.GameRep.gameObject.Despawn();
                //UnityEngine.Object.Destroy(creator.GameRep.gameObject);
                //CombatMovementReticle.Instance.RefreshActor(creator);

                //ModState.PositionLockSwarm.Add(creatorMech.GUID, targetActor.GUID);
                if (creatorMech is TrooperSquad squad)
                {
                    squad.AttachToCarrier(targetActor, false);
                    ModInit.modLog?.Trace?.Write($"[Ability.Activate - BattleArmorMountID] Called AttachToCarrier.");
                }
                ModInit.modLog?.Info?.Write(
                    $"[Ability.Activate - BattleArmorSwarmID] Added PositionLockSwarm with rider  {creatorMech.DisplayName} {creatorMech.GUID} and carrier {targetActor.DisplayName} {targetActor.GUID}.");
                creatorMech.ResetPathing(false);
                creatorMech.Pathing.UpdateCurrentPath(false);

                if (ModInit.modSettings.AttackOnSwarmSuccess && creatorMech.team.IsLocalPlayer && false)
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
                        ModInit.modLog?.Info?.Write(
                            $"[Ability.Activate - BattleArmorSwarmID] Creating attack sequence on successful swarm attack targeting location {loc}.");
                    }
                }
                if (creatorMech.team.IsLocalPlayer && false)
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
                ModInit.modLog?.Info?.Write(
                    $"[Ability.Activate - BattleArmorSwarmID] Cleaning up dummy attacksequence.");
                ModInit.modLog?.Info?.Write(
                    $"[Ability.Activate - BattleArmorSwarmID] No hits in HitInfo, plonking unit at adjacent hex.");
                Vector3 fetchRandomAdjacentHex = targetActor.FetchRandomAdjacentHex();
                ModInit.modLog?.Debug?.Write($"[Ability.Activate - BattleArmorSwarmID]   Swarming Position: {targetActor.CurrentPosition}  Selected Random Adjacent Hex: {fetchRandomAdjacentHex}");
                creatorMech.TeleportActor(fetchRandomAdjacentHex);
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

        public static void ReInitIndicator(this CombatHUD hud, AbstractActor actor)
        {
            var indicators = hud.InWorldMgr.AttackDirectionIndicators;//Traverse.Create(hud.InWorldMgr).Field("AttackDirectionIndicators").GetValue<List<AttackDirectionIndicator>>();
            foreach (var indicator in indicators)
            {
                if (indicator.Owner.GUID == actor.GUID)
                {
                    indicator.Init(actor, hud);
                }
            }
        }

        public static void SetHasExternalMountedBattleArmor(this AbstractActor actor, bool value)
        {
            actor.StatCollection.ModifyStat("BAMountDismount", -1, "HasExternalMountedBattleArmor", StatCollection.StatOperation.Set, value);
        }

        public static void SetPairingOverlay(this LanceConfiguratorPanel lanceConfiguratorPanel, LanceLoadoutSlot baLanceLoadoutSlot, bool showOverlay,
            LanceLoadoutSlot carrierLanceLoadoutSlot = null)
        {
            if (ModState.DefaultOverlay == new Color())
            {
                var overlayChildren = baLanceLoadoutSlot.SelectedMech.UnavailableOverlay.gameObject.GetComponentsInChildren<Image>();
                foreach (var overlayChild in overlayChildren)
                {
                    if (overlayChild.name == "stripes")
                    {
                        var overlayChildImage = overlayChild.GetComponentInChildren<Image>();
                        ModState.DefaultOverlay = new Color(overlayChildImage.color.r, overlayChildImage.color.g, overlayChildImage.color.b, overlayChildImage.color.a);
                        ModInit.modLog?.Trace?.Write(
                            $"[SetPairingOverlay] - set default overlay color to {ModState.DefaultOverlay} {ModState.DefaultOverlay.r} {ModState.DefaultOverlay.b} {ModState.DefaultOverlay.g} {ModState.DefaultOverlay.a}");
                    }
                }
            }
            if (!showOverlay)
            {
                baLanceLoadoutSlot.SelectedMech.UnavailableOverlay.SetActive(false);
                return;
            }
            
            var baOverlayChildren = baLanceLoadoutSlot.SelectedMech.UnavailableOverlay.gameObject.GetComponentsInChildren<Image>();

            Image BAOverlayChildImage = null;
            foreach (var baOverlayChild in baOverlayChildren)
            {
                if (baOverlayChild.name == "stripes")
                {
                    BAOverlayChildImage = baOverlayChild.GetComponent<Image>();
                }
            }
            
            if (carrierLanceLoadoutSlot == null)
            {
                baLanceLoadoutSlot.SelectedMech.UnavailableOverlay.SetActive(true);
                if (BAOverlayChildImage != null)
                    BAOverlayChildImage.color = ModState.PendingSelectionColor;//new Color(0, 0, 0, ModState.DefaultOverlay.a);
                lanceConfiguratorPanel.ToggleOverlayPotentialCarriers(baLanceLoadoutSlot, true);
                return;
            }
            baLanceLoadoutSlot.SelectedMech.UnavailableOverlay.SetActive(true);
            carrierLanceLoadoutSlot.SelectedMech.UnavailableOverlay.SetActive(true);
            carrierLanceLoadoutSlot.SelectedMech.UnavailableOverlay.SetActive(false);
            var carrierOverlayChildren = carrierLanceLoadoutSlot.SelectedMech.UnavailableOverlay.gameObject.GetComponentsInChildren<Image>();
            var carrierPilotID = carrierLanceLoadoutSlot.SelectedPilot.Pilot.pilotDef.Description.Id;
            foreach (var carrierOverlayChild in carrierOverlayChildren)
            {
                if (carrierOverlayChild.name == "stripes")
                {
                    var carrierOverlayChildImage = carrierOverlayChild.GetComponent<Image>();
                    if (!ModState.UsedOverlayColorsByCarrier.ContainsKey(carrierPilotID))
                    {
                        //initialize new overlay color
                        var foundUnused = false;

                        foreach (var potentialColor in ModState.ProcessedOverlayColors)
                        {
                            if (!ModState.UsedOverlayColors.Contains(potentialColor))
                            {
                                ModState.UsedOverlayColors.Add(potentialColor);
                                ModState.UsedOverlayColorsByCarrier.Add(carrierPilotID, potentialColor);
                                lanceConfiguratorPanel.ToggleOverlayPotentialCarriers(baLanceLoadoutSlot, false, carrierLanceLoadoutSlot);
                                carrierLanceLoadoutSlot.SelectedMech.UnavailableOverlay.SetActive(true);
                                carrierOverlayChildImage.color = potentialColor;
                                ModInit.modLog?.Trace?.Write($"[SetPairingOverlay] - carrier overlay color set to {carrierOverlayChildImage.color.r} {carrierOverlayChildImage.color.g} {carrierOverlayChildImage.color.b}");
                                if (BAOverlayChildImage != null)
                                {
                                    BAOverlayChildImage.color = potentialColor;
                                    ModInit.modLog?.Trace?.Write($"[SetPairingOverlay] - BA overlay color set to {BAOverlayChildImage.color.r} {BAOverlayChildImage.color.g} {BAOverlayChildImage.color.b}");
                                }
                                foundUnused = true;
                                break;
                            }
                        }

                        if (!foundUnused)
                        {
                            var chosenColor = ModState.UsedOverlayColors.GetRandomElement();
                            ModState.UsedOverlayColorsByCarrier.Add(carrierPilotID, chosenColor);
                            lanceConfiguratorPanel.ToggleOverlayPotentialCarriers(baLanceLoadoutSlot, false, carrierLanceLoadoutSlot);
                            carrierLanceLoadoutSlot.SelectedMech.UnavailableOverlay.SetActive(true);
                            carrierOverlayChildImage.color = chosenColor;
                            ModInit.modLog?.Trace?.Write($"[SetPairingOverlay] - no unused colors, chose one at random to duplicate. carrier overlay color set to {carrierOverlayChildImage.color.r} {carrierOverlayChildImage.color.g} {carrierOverlayChildImage.color.b}");
                            if (BAOverlayChildImage != null)
                            {
                                BAOverlayChildImage.color = chosenColor;
                                ModInit.modLog?.Trace?.Write($"[SetPairingOverlay] - no unused colors, chose one at random to duplicate. BA overlay color set to {BAOverlayChildImage.color.r} {BAOverlayChildImage.color.g} {BAOverlayChildImage.color.b}");
                            }
                        }
                    }
                    else
                    {
                        if (BAOverlayChildImage != null)
                        {
                            lanceConfiguratorPanel.ToggleOverlayPotentialCarriers(baLanceLoadoutSlot, false, carrierLanceLoadoutSlot);
                            carrierOverlayChildImage.color = ModState.UsedOverlayColorsByCarrier[carrierPilotID];
                            carrierLanceLoadoutSlot.SelectedMech.UnavailableOverlay.SetActive(true);
                            BAOverlayChildImage.color = ModState.UsedOverlayColorsByCarrier[carrierPilotID];
                            ModInit.modLog?.Trace?.Write(
                                $"[SetPairingOverlay] - Carrier already has non-default color. Setting BA color to match: {BAOverlayChildImage.color.r}, {BAOverlayChildImage.color.g}, {BAOverlayChildImage.color.b}");
                        }
                    }
                }
            }
        }

        public static void ShowBATargetsMeleeIndicator(this CombatHUDInWorldElementMgr inWorld, List<AbstractActor> targets, AbstractActor unit)
        {
            var tickMarks = inWorld.WeaponTickMarks;//Traverse.Create(inWorld).Field("WeaponTickMarks").GetValue<List<CombatHUDWeaponTickMarks>>();

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

        public static void ToggleOverlayPotentialCarriers(this LanceConfiguratorPanel lanceConfiguratorPanel, LanceLoadoutSlot baLanceLoadoutSlot, bool toggleOn = true, LanceLoadoutSlot carrierLanceLoadoutSlot = null)
        {
            if (baLanceLoadoutSlot?.SelectedMech == null) return;
            foreach (var loadOutSlot in lanceConfiguratorPanel.loadoutSlots)
            {
                if (loadOutSlot.SelectedMech != null && loadOutSlot.SelectedPilot != null)
                {
                    if (loadOutSlot.SelectedMech != baLanceLoadoutSlot.SelectedMech)
                    {
                        var hasSpace = false;
                        var hasPx = false;
                        var pilotID = loadOutSlot.SelectedPilot.Pilot.Description.Id;
                        
                        if (!ModState.PairingInfos.ContainsKey(pilotID))
                        {
                            if (loadOutSlot.SelectedMech.mechDef.GetTotalBASpaceMechDef() > 0) hasSpace = true;
                        }
                        else
                        {
                            var pairInfo = ModState.PairingInfos[pilotID];
                            if (pairInfo.PairedBattleArmor.Count < pairInfo.CapacityInitial)
                            {
                                hasSpace = true;
                                if (pairInfo.PairedBattleArmor.Count > 0) hasPx = true;
                            }
                        }

                        if (hasSpace)// && !hasPx)
                        {
                            var carrierOverlayChildren = loadOutSlot.SelectedMech.UnavailableOverlay.gameObject
                                .GetComponentsInChildren<Image>();
                            foreach (var carrierOverlayChild in carrierOverlayChildren)
                            {
                                if (carrierOverlayChild.name == "stripes")
                                {
                                    var carrierOverlayChildImage = carrierOverlayChild.GetComponent<Image>();
                                    
                                    if (toggleOn)
                                    {
                                        carrierOverlayChildImage.color = ModState.PendingSelectionColor;//new Color(0, 0, 0, ModState.DefaultOverlay.a);
                                        loadOutSlot.SelectedMech.UnavailableOverlay.SetActive(true);
                                    }
                                    else if (carrierLanceLoadoutSlot?.SelectedMech != null &&
                                             loadOutSlot.SelectedMech != carrierLanceLoadoutSlot.SelectedMech)
                                    {
                                        if (ModState.UsedOverlayColorsByCarrier.TryGetValue(pilotID, out var color))
                                        {
                                            carrierOverlayChildImage.color = color;//new Color(0, 0, 0, ModState.DefaultOverlay.a);
                                        }
                                        loadOutSlot.SelectedMech.UnavailableOverlay.SetActive(false);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
