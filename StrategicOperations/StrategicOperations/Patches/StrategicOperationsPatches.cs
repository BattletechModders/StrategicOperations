using System;
using System.Collections.Generic;
using System.Linq;
using Abilifier.Patches;
using BattleTech;
using BattleTech.Data;
using BattleTech.Rendering;
using BattleTech.UI;
using CustomActivatableEquipment;
using CustomUnits;
using Harmony;
using HBS;
using HBS.Collections;
using Localize;
using StrategicOperations.Framework;
using UnityEngine;
using static StrategicOperations.Framework.Classes;
using Random = System.Random;

namespace StrategicOperations.Patches
{
    public class StrategicOperationsPatches
    {
        [HarmonyPatch(typeof(MechStartupInvocation), "Invoke")]
        [HarmonyPriority(Priority.First)]
        public static class MechStartupInvocation_Invoke
        {
            public static bool Prefix(MechStartupInvocation __instance, CombatGameState combatGameState)
            {
                Mech mech = combatGameState.FindActorByGUID(__instance.MechGUID) as Mech;
                if (mech == null)
                {
                    //InvocationMessage.logger.LogError("MechStartupInvocation.Invoke failed! Unable to Mech!");
                    return false;
                }

                if (ModState.ResupplyShutdownPhases.ContainsKey(mech.GUID))
                {
                    var txt = new Text("RESUPPLY IN PROGRESS: ABORTING RESTART AND SINKING HEAT");
                    mech.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(
                        new ShowActorInfoSequence(mech, txt, FloatieMessage.MessageNature.Buff,
                            false)));

                    DoneWithActorSequence doneWithActorSequence = (DoneWithActorSequence) mech.GetDoneWithActorOrders();
                    MechHeatSequence mechHeatSequence = new MechHeatSequence(OwningMech: mech,
                        performHeatSinkStep: true, applyStartupHeatSinks: false, instigatorID: "STARTUP");
                    doneWithActorSequence.AddChildSequence(mechHeatSequence, mechHeatSequence.MessageIndex);

                    InvocationStackSequenceCreated message =
                        new InvocationStackSequenceCreated(doneWithActorSequence, __instance);
                    combatGameState.MessageCenter.PublishMessage(message);
                    AddSequenceToStackMessage.Publish(combatGameState.MessageCenter, doneWithActorSequence);

                    return false;
                }
                return true;
            }
        }


        [HarmonyPatch(typeof(Team), "AddUnit", new Type[] { typeof(AbstractActor) })]
        public static class Team_AddUnit_Patch
        {
            public static void Postfix(Team __instance, AbstractActor unit)
            {
                if (__instance.Combat.TurnDirector.CurrentRound > 1)
                {
                    __instance.Combat.UpdateResupplyTeams();
                    __instance.Combat.UpdateResupplyAbilitiesGetAllLivingActors();
                }

                if (__instance.IsLocalPlayer)
                {
                    if (unit is Mech && !(unit is TrooperSquad) && !unit.IsCustomUnitVehicle())
                    {
                        if (!string.IsNullOrEmpty(ModInit.modSettings.BattleArmorDeSwarmSwat))
                        {
                            if (unit.GetPilot().Abilities
                                    .All(x => x.Def.Id != ModInit.modSettings.BattleArmorDeSwarmSwat) &&
                                unit.ComponentAbilities.All(y =>
                                    y.Def.Id != ModInit.modSettings.BattleArmorDeSwarmSwat))
                            {
                                unit.Combat.DataManager.AbilityDefs.TryGet(ModInit.modSettings.BattleArmorDeSwarmSwat,
                                    out var def);
                                var ability = new Ability(def);
                                ModInit.modLog?.Trace?.Write(
                                    $"[Team.AddUnit] Adding {ability.Def?.Description?.Id} to {unit.Description?.Name}.");
                                ability.Init(unit.Combat);
                                unit.GetPilot().Abilities.Add(ability);
                                unit.GetPilot().ActiveAbilities.Add(ability);
                            }
                        }

                        if (!string.IsNullOrEmpty(ModInit.modSettings.BattleArmorDeSwarmRoll))
                        {
                            if (unit.GetPilot().Abilities
                                    .All(x => x.Def.Id != ModInit.modSettings.BattleArmorDeSwarmRoll) &&
                                unit.ComponentAbilities.All(y =>
                                    y.Def.Id != ModInit.modSettings.BattleArmorDeSwarmRoll))
                            {
                                unit.Combat.DataManager.AbilityDefs.TryGet(ModInit.modSettings.BattleArmorDeSwarmRoll,
                                    out var def);
                                var ability = new Ability(def);
                                ModInit.modLog?.Trace?.Write(
                                    $"[Team.AddUnit] Adding {ability.Def?.Description?.Id} to {unit.Description?.Name}.");
                                ability.Init(unit.Combat);
                                unit.GetPilot().Abilities.Add(ability);
                                unit.GetPilot().ActiveAbilities.Add(ability);
                            }
                        }

                    }

                    if (!string.IsNullOrEmpty(ModInit.modSettings.DeswarmMovementConfig.AbilityDefID))
                    {
                        if (unit.GetPilot().Abilities
                                .All(x => x.Def.Id != ModInit.modSettings.DeswarmMovementConfig.AbilityDefID) &&
                            unit.ComponentAbilities.All(y =>
                                y.Def.Id != ModInit.modSettings.DeswarmMovementConfig.AbilityDefID))
                        {
                            unit.Combat.DataManager.AbilityDefs.TryGet(ModInit.modSettings.DeswarmMovementConfig.AbilityDefID,
                                out var def);
                            var ability = new Ability(def);
                            ModInit.modLog?.Trace?.Write(
                                $"[Team.AddUnit] Adding {ability.Def?.Description?.Id} to {unit.Description?.Name}.");
                            ability.Init(unit.Combat);
                            unit.GetPilot().Abilities.Add(ability);
                            unit.GetPilot().ActiveAbilities.Add(ability);
                        }
                    }


                    return;
                }

                if (unit is Mech mech)
                {
                    if (mech.EncounterTags.Contains("SpawnedFromAbility")) return;
                }

                AI_Utils.GenerateAIStrategicAbilities(unit);
            }
        }

        [HarmonyPatch(typeof(AbstractActor), "InitEffectStats",
            new Type[] { })]
        public static class AbstractActor_InitEffectStats
        {
            public static void Postfix(AbstractActor __instance)
            {
                __instance.StatCollection.AddStatistic<bool>("CanSwarm", false);
                __instance.StatCollection.AddStatistic<bool>("CanAirliftHostiles", false);
                __instance.StatCollection.AddStatistic<bool>("BattleArmorInternalMountsOnly", false);
                __instance.StatCollection.AddStatistic<int>("InternalBattleArmorSquadCap", 0);
                __instance.StatCollection.AddStatistic<int>("InternalBattleArmorSquads", 0);
                __instance.StatCollection.AddStatistic<bool>("HasBattleArmorMounts", false);
                __instance.StatCollection.AddStatistic<bool>("HasExternalMountedBattleArmor", false);
                __instance.StatCollection.AddStatistic<bool>("IsBattleArmorHandsy", false);
                __instance.StatCollection.AddStatistic<bool>("IsUnmountableBattleArmor", false);
                __instance.StatCollection.AddStatistic<bool>("IsUnswarmableBattleArmor", false);
                __instance.StatCollection.AddStatistic<bool>("HasFiringPorts", false);
                //__instance.StatCollection.AddStatistic<bool>("BattleArmorMount", false);
                //__instance.StatCollection.AddStatistic<float>("BattleArmorDeSwarmerSwat", 0.3f);
                //__instance.StatCollection.AddStatistic<int>("BattleArmorDeSwarmerRollInitPenalty", 0);
                //__instance.StatCollection.AddStatistic<int>("BattleArmorDeSwarmerSwatInitPenalty", 0);
                //__instance.StatCollection.AddStatistic<float>("BattleArmorDeSwarmerSwatDamage", 0f);
                //__instance.StatCollection.AddStatistic<float>("BattleArmorDeSwarmerRoll", 0.5f);
                //__instance.StatCollection.AddStatistic<float>("MovementDeSwarmMinChance", 0.0f);
                //__instance.StatCollection.AddStatistic<float>("MovementDeSwarmMaxChance", 1.0f);
                //__instance.StatCollection.AddStatistic<float>("MovementDeSwarmEvasivePipsFactor", 0f);
                //__instance.StatCollection.AddStatistic<float>("MovementDeSwarmEvasiveJumpMovementMultiplier", 1.0f);
                //__instance.StatCollection.AddStatistic<bool>("Airlifting", false);
                __instance.StatCollection.AddStatistic<int>("InternalLiftCapacity", 0);
                __instance.StatCollection.AddStatistic<int>("InternalLiftCapacityUsed", 0);
                __instance.StatCollection.AddStatistic<int>("ExternalLiftCapacity", 0);
                __instance.StatCollection.AddStatistic<int>("ExternalLiftCapacityUsed", 0);
                __instance.StatCollection.AddStatistic<float>("AAAFactor", 0f);
            }
        }

        [HarmonyPatch(typeof(SimGameState), "RequestDataManagerResources")]
        public static class SimGameState_RequestDataManagerResources_Patch
        {
            public static void Postfix(SimGameState __instance)
            {
                LoadRequest loadRequest = __instance.DataManager.CreateLoadRequest();
                loadRequest.AddAllOfTypeBlindLoadRequest(BattleTechResourceType.TurretDef,
                    new bool?(true)); //but TurretDefs
                loadRequest.ProcessRequests(10U);
            }
        }

        [HarmonyPatch(typeof(TurnDirector), "OnInitializeContractComplete")]
        public static class TurnDirector_OnInitializeContractComplete
        {
            public static void Postfix(TurnDirector __instance, MessageCenterMessage message)
            {
                if (__instance.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                var dm = __instance.Combat.DataManager;
                LoadRequest loadRequest = dm.CreateLoadRequest();

                loadRequest.AddBlindLoadRequest(BattleTechResourceType.PilotDef, "pilot_sim_starter_dekker");
                ModInit.modLog?.Info?.Write($"Added loadrequest for PilotDef: pilot_sim_starter_dekker (hardcoded)");
                if (!string.IsNullOrEmpty(ModInit.modSettings.customSpawnReticleAsset))
                {
                    loadRequest.AddBlindLoadRequest(BattleTechResourceType.Texture2D, ModInit.modSettings.customSpawnReticleAsset);
                    ModInit.modLog?.Info?.Write($"Added loadrequest for Texture2D: {ModInit.modSettings.customSpawnReticleAsset}");
                }
                if (!string.IsNullOrEmpty(ModInit.modSettings.MountIndicatorAsset))
                {
                    loadRequest.AddBlindLoadRequest(BattleTechResourceType.Texture2D, ModInit.modSettings.MountIndicatorAsset);
                    ModInit.modLog?.Info?.Write($"Added loadrequest for Texture2D: {ModInit.modSettings.MountIndicatorAsset}");
                }
                
                foreach (var abilityDef in dm.AbilityDefs.Where(x => x.Key.StartsWith("AbilityDefCMD_")))
                {
                    var ability = new Ability(abilityDef.Value);
                    if (string.IsNullOrEmpty(ability.Def?.ActorResource)) continue;
                    if (!string.IsNullOrEmpty(ability.Def.getAbilityDefExtension().CMDPilotOverride))
                    {
                        var pilotID = ability.Def.getAbilityDefExtension().CMDPilotOverride;
                        ModInit.modLog?.Info?.Write($"Added loadrequest for PilotDef: {pilotID}");
                        loadRequest.AddBlindLoadRequest(BattleTechResourceType.PilotDef, pilotID);
                    }

                    if (ability.Def.ActorResource.StartsWith("mechdef_"))
                    {
                        ModInit.modLog?.Info?.Write($"Added loadrequest for MechDef: {ability.Def.ActorResource}");
                        loadRequest.AddBlindLoadRequest(BattleTechResourceType.MechDef, ability.Def.ActorResource);
                    }

                    if (ability.Def.ActorResource.StartsWith("vehicledef_"))
                    {
                        ModInit.modLog?.Info?.Write($"Added loadrequest for VehicleDef: {ability.Def.ActorResource}");
                        loadRequest.AddBlindLoadRequest(BattleTechResourceType.VehicleDef, ability.Def.ActorResource);
                    }

                    if (ability.Def.ActorResource.StartsWith("turretdef_"))
                    {
                        ModInit.modLog?.Info?.Write($"Added loadrequest for TurretDef: {ability.Def.ActorResource}");
                        loadRequest.AddBlindLoadRequest(BattleTechResourceType.TurretDef, ability.Def.ActorResource);
                    }
                }

                foreach (var beacon in Utils.GetOwnedDeploymentBeacons())
                {
                    var pilotID = beacon.Def.ComponentTags.FirstOrDefault(x =>
                            x.StartsWith("StratOpsPilot_"))
                        ?.Remove(0, 14);
                    if (!string.IsNullOrEmpty(pilotID))
                    {
                        ModInit.modLog?.Info?.Write($"Added loadrequest for PilotDef: {pilotID}");
                        loadRequest.AddBlindLoadRequest(BattleTechResourceType.PilotDef, pilotID);
                    }

                    var id = beacon.Def.ComponentTags.FirstOrDefault(x =>
                        x.StartsWith("mechdef_") || x.StartsWith("vehicledef_") ||
                        x.StartsWith("turretdef_"));
                    if (string.IsNullOrEmpty(id)) continue;

                    if (id.StartsWith("mechdef_"))
                    {
                        ModInit.modLog?.Info?.Write($"Added loadrequest for MechDef: {id}");
                        loadRequest.AddBlindLoadRequest(BattleTechResourceType.MechDef, id);
                    }
                    else if (id.StartsWith("vehicledef_"))
                    {
                        ModInit.modLog?.Info?.Write($"Added loadrequest for VehicleDef: {id}");
                        loadRequest.AddBlindLoadRequest(BattleTechResourceType.VehicleDef, id);
                    }
                    else if (id.StartsWith("turretdef_"))
                    {
                        ModInit.modLog?.Info?.Write($"Added loadrequest for TurretDef: {id}");
                        loadRequest.AddBlindLoadRequest(BattleTechResourceType.TurretDef, id);
                    }

                }
                loadRequest.ProcessRequests(1000U);
            }
        }

        [HarmonyPatch(typeof(CustomMechRepresentation), "GameRepresentation_Update")]
        public static class CustomMechRepresentation_GameRepresentation_Update
        {
            public static bool Prefix(CustomMechRepresentation __instance)
            {
                if (__instance.__parentActor == null) return true;
                var combat = UnityGameInstance.BattleTechGame.Combat;
                if (combat == null) return true;
                if (combat.ActiveContract.ContractTypeValue.IsSkirmish) return true;
                var registry = combat.ItemRegistry;

                if (__instance.__parentActor?.spawnerGUID == null)
                {
                    //ModInit.modLog?.Info?.Write($"Couldn't find UnitSpawnPointGameLogic for {____parentActor?.DisplayName}. Should be CMD Ability actor; skipping safety teleport!");
                    return false;
                }

                return registry.GetItemByGUID<UnitSpawnPointGameLogic>(__instance.__parentActor?.spawnerGUID) != null;
                //ModInit.modLog?.Info?.Write($"Couldn't find UnitSpawnPointGameLogic for {____parentActor?.DisplayName}. Should be CMD Ability actor; skipping safety teleport!");
            }
        }


        [HarmonyPatch(typeof(GameRepresentation), "Update")]
        public static class GameRepresentation_Update
        {
            public static bool Prefix(GameRepresentation __instance, AbstractActor ____parentActor)
            {
                if (____parentActor == null) return true;
                var combat = UnityGameInstance.BattleTechGame.Combat;
                if (combat == null) return true;
                if (combat.ActiveContract.ContractTypeValue.IsSkirmish) return true;
                var registry = combat.ItemRegistry;

                if (____parentActor?.spawnerGUID == null)
                {
                    //ModInit.modLog?.Info?.Write($"Couldn't find UnitSpawnPointGameLogic for {____parentActor?.DisplayName}. Should be CMD Ability actor; skipping safety teleport!");
                    return false;
                }

                return registry.GetItemByGUID<UnitSpawnPointGameLogic>(____parentActor?.spawnerGUID) != null;
                //ModInit.modLog?.Info?.Write($"Couldn't find UnitSpawnPointGameLogic for {____parentActor?.DisplayName}. Should be CMD Ability actor; skipping safety teleport!");
            }

            public static void Postfix(GameRepresentation __instance, AbstractActor ____parentActor)
            {
                if (____parentActor == null) return;

                if (ModState.CachedUnitCoordinates.ContainsKey(____parentActor.GUID))
                {
                    if (____parentActor is CustomMech mech)
                    {
                        if (ModState.CachedUnitCoordinates[____parentActor.GUID] == ____parentActor.CurrentPosition && !mech.custGameRep.HeightController.isInChangeHeight) return;
                    }
                    else if (ModState.CachedUnitCoordinates[____parentActor.GUID] == ____parentActor.CurrentPosition)
                    {
                        return;
                    }
                }
                var combat = ____parentActor.Combat;
                if (____parentActor.HasAirliftedEnemy() || ____parentActor.HasAirliftedFriendly())
                {
                    var airliftedUnits= ModState.AirliftTrackers.Where(x =>
                        x.Value.CarrierGUID == ____parentActor.GUID);
                    foreach (var trackerInfo in airliftedUnits)
                    {
                        var targetActor = combat.FindActorByGUID(trackerInfo.Key);
                        if (targetActor == null) continue;
                        var pos = Vector3.zero;
                        if (____parentActor is CustomMech mech)
                        {
                            pos = ____parentActor.CurrentPosition + Vector3.down * trackerInfo.Value.Offset + Vector3.up * mech.custGameRep.HeightController.CurrentHeight;
                            targetActor.TeleportActor(pos);
                        }
                        else
                        {
                            pos = ____parentActor.CurrentPosition + Vector3.down * trackerInfo.Value.Offset;
                            targetActor.TeleportActor(pos);
                        }
                        targetActor.MountedEvasion(____parentActor);
                        ModInit.modLog?.Debug?.Write($"PositionLockMount- Setting airlifted unit {targetActor.DisplayName} position to same as carrier unit {____parentActor.DisplayName}");

                        if (!ModState.CachedUnitCoordinates.ContainsKey(____parentActor.GUID))
                        {
                            ModState.CachedUnitCoordinates.Add(____parentActor.GUID, ____parentActor.CurrentPosition);
                        }
                        else
                        {
                            ModState.CachedUnitCoordinates[____parentActor.GUID] = ____parentActor.CurrentPosition;
                        }
                    }
                }


                if (____parentActor.HasMountedUnits())
                {
                    var targetActorGUIDs = ModState.PositionLockMount.Where(x=>x.Value == ____parentActor.GUID);
                    foreach (var targetActorGUID in targetActorGUIDs)
                    {
                        var targetActor = combat.FindActorByGUID(targetActorGUID.Key);
                        if (targetActor == null) continue;
                        var pos = Vector3.zero;
                        if (____parentActor is CustomMech mech)
                        {
                            pos = ____parentActor.CurrentPosition +
                                      Vector3.up * mech.custGameRep.HeightController.CurrentHeight;
                            targetActor.TeleportActor(pos);
                        }
                        else
                        {
                            targetActor.TeleportActor(____parentActor.CurrentPosition);
                        }
                        targetActor.MountedEvasion(____parentActor);
                        ModInit.modLog?.Debug?.Write($"PositionLockMount- Setting riding unit {targetActor.DisplayName} position to same as carrier unit {____parentActor.DisplayName}");

                        if (!ModState.CachedUnitCoordinates.ContainsKey(____parentActor.GUID))
                        {
                            ModState.CachedUnitCoordinates.Add(____parentActor.GUID, ____parentActor.CurrentPosition);
                        }
                        else
                        {
                            ModState.CachedUnitCoordinates[____parentActor.GUID] = ____parentActor.CurrentPosition;
                        }
                    }
                }
                // removed return/else so swarming units are locked to carrier even if carrier has mounted units. derp.
                if (____parentActor.HasSwarmingUnits())
                {
                    var targetActorGUIDs = ModState.PositionLockSwarm.Where(x => x.Value == ____parentActor.GUID);
                    foreach (var targetActorGUID in targetActorGUIDs)
                    {
                        var targetActor = combat.FindActorByGUID(targetActorGUID.Key);
                        if (targetActor == null) continue;
                        targetActor.TeleportActor(____parentActor.CurrentPosition);
                        targetActor.MountedEvasion(____parentActor);
                        ModInit.modLog?.Debug?.Write($"PositionLockMount- Setting riding unit {targetActor.DisplayName} position to same as carrier unit {____parentActor.DisplayName}");

                        if (!ModState.CachedUnitCoordinates.ContainsKey(____parentActor.GUID))
                        {
                            ModState.CachedUnitCoordinates.Add(____parentActor.GUID, ____parentActor.CurrentPosition);
                        }
                        else
                        {
                            ModState.CachedUnitCoordinates[____parentActor.GUID] = ____parentActor.CurrentPosition;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Team), "ActivateAbility")]
        public static class Team_ActivateAbility
        {
            public static bool Prefix(Team __instance, AbilityMessage msg)
            {
                if (__instance.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return true;
                Ability ability = ModState.CommandAbilities.Find((Ability x) => x.Def.Id == msg.abilityID);
                if (ability == null)
                {
                    ModInit.modLog?.Info?.Write(
                        $"Tried to use a CommandAbility the team doesnt have?");
                    return false;
                }

                switch (ability.Def.Targeting)
                {
                    case AbilityDef.TargetingType.CommandInstant:
                        ability.Activate(__instance, null);
                        goto publishAbilityConfirmed;
                    case AbilityDef.TargetingType.CommandTargetSingleEnemy:
                    {
                        ICombatant combatant = __instance.Combat.FindCombatantByGUID(msg.affectedObjectGuid, false);
                        if (combatant == null)
                        {
                            ModInit.modLog?.Info?.Write(
                                $"Team.ActivateAbility couldn't find target with guid {msg.affectedObjectGuid}");
                            return false;
                        }

                        ability.Activate(__instance, combatant);
                        goto publishAbilityConfirmed;
                    }
                    case AbilityDef.TargetingType.CommandTargetSinglePoint:
                        ability.Activate(__instance, msg.positionA);
                        goto publishAbilityConfirmed;
                    case AbilityDef.TargetingType.CommandTargetTwoPoints:
                    case AbilityDef.TargetingType.CommandSpawnPosition:
                        ability.Activate(__instance, msg.positionA, msg.positionB);
                        goto publishAbilityConfirmed;
                    case AbilityDef.TargetingType.NotSet:
                        break;
                    case AbilityDef.TargetingType.ActorSelf:
                        break;
                    case AbilityDef.TargetingType.ActorTarget:
                        break;
                    case AbilityDef.TargetingType.SensorLock:
                        break;
                    case AbilityDef.TargetingType.CommandTargetSingleAlly:
                        break;
                    case AbilityDef.TargetingType.ShadowMove:
                        break;
                    case AbilityDef.TargetingType.MultiFire:
                        break;
                    case AbilityDef.TargetingType.ConfirmCoolantVent:
                        break;
                    case AbilityDef.TargetingType.ActiveProbe:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                ModInit.modLog?.Info?.Write(
                    $"Team.ActivateAbility needs to add handling for targetingtype {ability.Def.Targeting}");
                return false;
                publishAbilityConfirmed:
                __instance.Combat.MessageCenter.PublishMessage(new AbilityConfirmedMessage(msg.actingObjectGuid,
                    msg.affectedObjectGuid, msg.abilityID, msg.positionA, msg.positionB));
                return false;
            }
        }


        [HarmonyPatch(typeof(AbstractActor), "ActivateAbility",
            new Type[] {typeof(AbstractActor), typeof(string), typeof(string), typeof(Vector3), typeof(Vector3)})]
        public static class AbstractActor_ActivateAbility
        {
            public static bool Prefix(AbstractActor __instance, AbstractActor pilotedActor, string abilityName,
                string targetGUID, Vector3 posA, Vector3 posB)
            {
                if (__instance.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return true;
                Ability ability = __instance.ComponentAbilities.Find((Ability x) => x.Def.Id == abilityName);
                if (ability.Def.Targeting == AbilityDef.TargetingType.CommandSpawnPosition)
                {
                    ability.Activate(__instance, posA, posB);
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Ability), "Activate",
            new Type[] { typeof(AbstractActor), typeof(ICombatant) })]
        public static class Ability_Activate_ICombatant
        {
            public static void Postfix(Ability __instance, AbstractActor creator, ICombatant target)
            {
                if (creator == null) return;
                if (UnityGameInstance.BattleTechGame.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;

                if (__instance.IsAvailable)
                {
                    if (target is AbstractActor targetActor)
                    {
                        if (__instance.Def.Id == ModInit.modSettings.ResupplyConfig.ResupplyAbilityID)
                        {
                            ModInit.modLog?.Trace?.Write($"[Ability.Activate] Activating resupply from unit {creator.DisplayName} and resupplier {targetActor.DisplayName}.");
                            var phases = creator.ProcessResupplyUnit(targetActor);
                            creator.InitiateShutdownForPhases(phases);
                            targetActor.InitiateShutdownForPhases(phases);
                        }

                        if (creator.HasSwarmingUnits() && creator.GUID == targetActor.GUID)
                        {
                            ModInit.modLog?.Trace?.Write($"[Ability.Activate - Unit has sawemers].");
                            var swarmingUnits = ModState.PositionLockSwarm.Where(x => x.Value == creator.GUID).ToList();

                            if (__instance.Def.Id == ModInit.modSettings.BattleArmorDeSwarmRoll)
                            {
                                creator.ProcessDeswarmRoll(swarmingUnits);
                                return;
                            }

                            else if (__instance.Def.Id == ModInit.modSettings.BattleArmorDeSwarmSwat)
                            {
                                creator.ProcessDeswarmSwat(swarmingUnits);
                                return;
                            }

                            else if (__instance.Def.Id == ModInit.modSettings.DeswarmMovementConfig.AbilityDefID)
                            {
                                ModInit.modLog?.Trace?.Write($"[Ability.Activate - BattleArmorDeSwarm Movement].");
                                creator.ProcessDeswarmMovement(
                                    swarmingUnits); // need to patch ActorMovementSequence complete AND JumpSequence complete AND DFASequencecomplete, and then do magic logic in there. or just do it on
                                return; //return to avoid ending turn for player below. making AI use this properly is gonna suck hind tit.
                            }

                            if (creator is Mech mech)
                            {
                                mech.GenerateAndPublishHeatSequence(-1, true, false, mech.GUID);
                            }

                            if (__instance.Def.Id == ModInit.modSettings.BattleArmorDeSwarmRoll)
                            {
                                creator.FlagForKnockdown();
                                creator.HandleKnockdown(-1, creator.GUID, Vector2.one, null);
                            }

                            if (creator.team.IsLocalPlayer)
                            {
                                var sequence = creator.DoneWithActor();
                                creator.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(sequence));
                                //creator.OnActivationEnd(creator.GUID, -1);
                            }

                            return;
                        }
                        if (__instance.Def.Id == ModInit.modSettings.BattleArmorMountAndSwarmID)
                        {
                            if (!creator.IsSwarmingUnit() && !creator.IsMountedUnit())
                            {
                                if (target.team.IsFriendly(creator.team))
                                {
                                    //creator.ProcessMountFriendly(targetActor); // old handling, now try movement invocation.
                                    MessageCenterMessage invocation =
                                        new StrategicMovementInvocation(creator, true, targetActor, true, true);
                                    creator.Combat.MessageCenter.PublishInvocationExternal(invocation);
                                }

                                else if (target.team.IsEnemy(creator.team) && creator is Mech creatorMech &&
                                         creatorMech.canSwarm())
                                {
                                    //creatorMech.ProcessSwarmEnemy(targetActor);
                                    MessageCenterMessage invocation =
                                        new StrategicMovementInvocation(creator, true, targetActor, false, true);
                                    creator.Combat.MessageCenter.PublishInvocationExternal(invocation);
                                }
                            }

                            else if (creator.IsSwarmingUnit())
                            {
                                creator.DismountBA(targetActor, Vector3.zero, false);
                            }
                            else if (creator.IsMountedUnit())
                            {
                                if (creator is TrooperSquad squad)
                                {
                                    //ModInit.modLog?.Trace?.Write($"[Ability.Activate] Called DetachFromCarrier.");
                                    //squad.DismountBA(targetActor, Vector3.zero, true, false, false);
                                    squad.DetachFromCarrier(targetActor, true);
                                }
                                //creator.DismountBA(targetActor);

                            }
                        }
                        else if (__instance.Def.Id == ModInit.modSettings.AirliftAbilityID)
                        {
                            ModInit.modLog?.Trace?.Write($"[Ability.Activate] - Creating airlift invocation for carrier {creator.DisplayName} and target {targetActor.DisplayName}.");
                            if (target.team.IsFriendly(creator.team))
                            {
                                MessageCenterMessage invocation =
                                    new StrategicMovementInvocation(creator, true, targetActor, true, false);
                                creator.Combat.MessageCenter.PublishInvocationExternal(invocation);
                            }

                            else if (target.team.IsEnemy(creator.team))
                            {
                                MessageCenterMessage invocation =
                                    new StrategicMovementInvocation(creator, true, targetActor, false, false);
                                creator.Combat.MessageCenter.PublishInvocationExternal(invocation);
                            }
                            //do airlifty things here
                        }
                    }
                    if (target is BattleTech.Building building &&
                        __instance.Def.Id == ModInit.modSettings.BattleArmorMountAndSwarmID && creator is TrooperSquad squad2)
                    {
                        if (!building.hasGarrisonedUnits())
                        {
                            MessageCenterMessage invocation =
                                new StrategicMovementInvocation(squad2, true, building, true, true);
                            squad2.Combat.MessageCenter.PublishInvocationExternal(invocation);
                        }
                        else
                        {
                            if (squad2.isGarrisonedInTargetBuilding(building))
                            {
                                squad2.DismountGarrison(building, Vector3.zero, false);
                            }
                        }
                    }
                }
            }
        }


        [HarmonyPatch(typeof(Ability), "Activate",
            new Type[] {typeof(AbstractActor), typeof(Vector3), typeof(Vector3)})]
        public static class Ability_Activate_TwoPoints
        {
            public static bool Prefix(Ability __instance, AbstractActor creator, Vector3 positionA, Vector3 positionB)
            {
                ModInit.modLog?.Info?.Write($"[Ability.Activate - 2pts] Running Ability.Activate; check if skirmish."); // need to add blocks for self-apply EffectData
                if (__instance.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return true;
                if (!__instance.IsAvailable)
                {
                    ModInit.modLog?.Info?.Write(
                        $"[Ability.Activate - 2pts] Ability {__instance.Def.Description.Name} was unavailable, continuing to vanilla handling.");
                    return true;
                }

                if (ModInit.modSettings.BeaconExcludedContractIDs.Contains(__instance.Combat.ActiveContract.Override.ID) || ModInit.modSettings.BeaconExcludedContractTypes.Contains(__instance.Combat.ActiveContract.ContractTypeValue.Name))
                {
                    var popup = GenericPopupBuilder.Create(GenericPopupType.Info, $"Ability {__instance.Def.Description.Name} is unavailable during this contract!");
                    popup.AddButton("Confirm", null, true, null);
                    popup.IsNestedPopupWithBuiltInFader().CancelOnEscape().Render();
                    ModInit.modLog?.Info?.Write($"[Ability.Activate - 2pts] Ability {__instance.Def.Description.Name} unavailable due to exclusion settings. Aborting.");
                    return false;
                }

                AbilityDef.SpecialRules specialRules = __instance.Def.specialRules;
                if (specialRules == AbilityDef.SpecialRules.Strafe)
                {
                    var cancelChance = 0f;
                    if (!creator.team.IsLocalPlayer && ModState.startUnitFromInvocation != null)
                    {
                        cancelChance = ModState.startUnitFromInvocation.GetAvoidStrafeChanceForTeam();
                        ModInit.modLog?.Trace?.Write($"[Ability.Activate - 2pts] {creator.DisplayName}: ActivateStrafe processing cancelChance {cancelChance} from AI invocation, using target unit {ModState.startUnitFromInvocation.DisplayName} as base.");
                        ModState.startUnitFromInvocation = null;
                    }
                    else if (creator.team.IsLocalPlayer)
                    {
                        cancelChance = ModState.cancelChanceForPlayerStrafe;
                        ModInit.modLog?.Trace?.Write($"[Ability.Activate - 2pts] {creator.DisplayName}: ActivateStrafe processing cancelChance {cancelChance} from player invocation.");
                        ModState.cancelChanceForPlayerStrafe = 0f;
                    }
                    var roll = (float)ModInit.Random.NextDouble();
                    if (roll <= cancelChance)
                    {
                        ModInit.modLog?.Trace?.Write($"[Ability.Activate - 2pts] roll {roll} <= cancelChance {cancelChance}, nostrafe - return true and let vanilla sort it out.");

                        var txt = new Text("AA Interference: Strafing Run CANCELLED!");
                        creator.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(
                            new ShowActorInfoSequence(creator, txt, FloatieMessage.MessageNature.Buff,
                                false)));

                        return true;
                    }

                    Utils._activateStrafeMethod.Invoke(__instance,
                        new object[] {creator.team, positionA, positionB, __instance.Def.FloatParam1});
                    ModInit.modLog?.Info?.Write($"[Ability.Activate - 2pts] {creator.Description?.Name}: ActivateStrafe invoked from Ability.Activate. Distance was {Vector3.Distance(positionA, positionB)}");
                    __instance.Combat.MessageCenter.PublishMessage(new AbilityActivatedMessage(creator.GUID,
                        creator.GUID, __instance.Def.Id, positionA, positionB));
                    __instance.ActivateCooldown();
                    __instance.ApplyCreatorEffects(creator);
                    return false;
                }

                else if (specialRules == AbilityDef.SpecialRules.SpawnTurret)
                {
                    Utils._activateSpawnTurretMethod.Invoke(__instance,
                        new object[] {creator.team, positionA, positionB});
                    ModInit.modLog?.Info?.Write($"[Ability.Activate - 2pts] {creator.Description?.Name}: ActivateSpawnTurret invoked from Ability.Activate. Distance was {Vector3.Distance(positionA, positionB)}");
                    __instance.Combat.MessageCenter.PublishMessage(new AbilityActivatedMessage(creator.GUID,
                        creator.GUID, __instance.Def.Id, positionA, positionB));
                    __instance.ActivateCooldown();
                    __instance.ApplyCreatorEffects(creator);
                    return false;
                }

                return true;
            }
        }


        // ActivateStrafe for AOE? use CAC TerrainAttackDeligate and "walk" it across the field?
        //should it be a totally separate ability? probably. maybe. unsure.
        [HarmonyPatch(typeof(Ability), "ActivateStrafe")]
        public static class Ability_ActivateStrafe
        {
            public static bool Prefix(Ability __instance, Team team, Vector3 positionA, Vector3 positionB, float radius)
            {
                if (__instance.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return true;
                ModInit.modLog?.Info?.Write($"Running Ability.ActivateStrafe");
                var dm = __instance.Combat.DataManager;
                var sim = UnityGameInstance.BattleTechGame.Simulation;
                var pilotID = "pilot_sim_starter_dekker";
                var supportHeraldryDef = Utils.SwapHeraldryColors(team.HeraldryDef, dm);
                if (!string.IsNullOrEmpty(ModState.PilotOverride))
                {
                    pilotID = ModState.PilotOverride;
                }

                else if (!string.IsNullOrEmpty(__instance.Def.getAbilityDefExtension().CMDPilotOverride))
                {
                    pilotID = __instance.Def.getAbilityDefExtension().CMDPilotOverride;
                }

                dm.PilotDefs.TryGet(pilotID, out var supportPilotDef);


                //if (__instance.Combat.Teams.All(x => x.GUID != "61612bb3-abf9-4586-952a-0559fa9dcd75"))
                //{
                //Utils.CreateOrUpdateNeutralTeam();
                Team supportTeam;
                if (team.IsLocalPlayer)
                {
                    supportTeam = team.SupportTeam;
                }
                else
                {
                    supportTeam = Utils.CreateOrUpdateAISupportTeam(team);
                }

                //}

                //var supportTeam = __instance.Combat.Teams.FirstOrDefault(x => x.GUID == "61612bb3-abf9-4586-952a-0559fa9dcd75");

                ModInit.modLog?.Info?.Write($"Team neturalTeam = {supportTeam?.DisplayName}");
                var cmdLance = Utils.CreateOrFetchCMDLance(supportTeam);
                var actorResource = __instance.Def.ActorResource;
                var strafeWaves = ModInit.modSettings.strafeWaves;
                if (ModState.StrafeWaves > 0)
                {
                    strafeWaves = ModState.StrafeWaves;
                }
                if (!string.IsNullOrEmpty(__instance.Def?.ActorResource))
                {
                    if (!string.IsNullOrEmpty(ModState.PopupActorResource))
                    {
                        actorResource = ModState.PopupActorResource;
                        ModState.PopupActorResource = "";
                    }

                    ModInit.modLog?.Info?.Write($"Pilot should be {pilotID}");
                    if (ModState.DeploymentAssetsStats.Any(x => x.ID == actorResource) && team.IsLocalPlayer)
                    {
                        var assetStatInfo = ModState.DeploymentAssetsStats.FirstOrDefault(x => x.ID == actorResource);
                        if (assetStatInfo != null)
                        {
                            assetStatInfo.contractUses -= 1;
                            if (assetStatInfo.consumeOnUse)
                            {
                                sim?.CompanyStats.ModifyStat("StratOps", -1, assetStatInfo.stat,
                                    StatCollection.StatOperation.Int_Subtract, 1);
                            }
                        }

                        ModInit.modLog?.Info?.Write($"Decrementing count of {actorResource} in deploymentAssetsDict");
                    }

                    var parentSequenceID = Guid.NewGuid().ToString();
                    var newWave = new PendingStrafeWave(strafeWaves - 1, __instance, team, positionA,
                        positionB, radius, actorResource, supportTeam, cmdLance, supportPilotDef, supportHeraldryDef,
                        dm);
                    ModState.PendingStrafeWaves.Add(parentSequenceID, newWave);
                    Utils.InitiateStrafe(parentSequenceID, newWave);
                    ModInit.modLog?.Info?.Write($"First time initializing strafe with GUID {parentSequenceID}");
                    if (__instance.Def.IntParam1 > 0)
                    {
                        Utils.SpawnFlares(__instance, positionA, positionB, ModInit.modSettings.flareResourceID,
                            __instance.Def.IntParam1, Math.Max(__instance.Def.ActivationETA * strafeWaves, strafeWaves), team.IsLocalPlayer); // make smoke last for all strafe waves because babies
                    }
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(Ability), "ActivateSpawnTurret")]
        public static class Ability_ActivateSpawnTurret
        {
            public static bool Prefix(Ability __instance, Team team, Vector3 positionA, Vector3 positionB)
            {
                if (__instance.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return true;
                ModInit.modLog?.Info?.Write($"Running Ability.ActivateSpawnTurret");
                var combat = UnityGameInstance.BattleTechGame.Combat;
                var dm = combat.DataManager;
                var sim = UnityGameInstance.BattleTechGame.Simulation;

                var teamSelection = team.SupportTeam;
                if (!team.IsLocalPlayer)
                {
                    teamSelection = team as AITeam;
                }
                var actorResource = __instance.Def.ActorResource;
                var supportHeraldryDef = Utils.SwapHeraldryColors(team.HeraldryDef, dm);

                if (!string.IsNullOrEmpty(ModState.PopupActorResource))
                {
                    actorResource = ModState.PopupActorResource;
                    ModState.PopupActorResource = "";
                }

                if (ModState.DeploymentAssetsStats.Any(x => x.ID == actorResource) && team.IsLocalPlayer)
                {
                    var assetStatInfo = ModState.DeploymentAssetsStats.FirstOrDefault(x => x.ID == actorResource);
                    if (assetStatInfo != null)
                    {
                        assetStatInfo.contractUses -= 1;
                        if (assetStatInfo.consumeOnUse)
                        {
                            sim?.CompanyStats.ModifyStat("StratOps", -1, assetStatInfo.stat,
                                StatCollection.StatOperation.Int_Subtract, 1);
                        }
                    }

                    ModInit.modLog?.Info?.Write($"Decrementing count of {actorResource} in deploymentAssetsDict");
                }

                var instanceGUID =
                    $"{__instance.Def.Id}_{team.Name}_{actorResource}_{positionA}_{positionB}@{actorResource}";

                if (ModState.DeferredInvokeSpawns.All(x => x.Key != instanceGUID) && !ModState.DeferredSpawnerFromDelegate)
                {
                    ModInit.modLog?.Info?.Write(
                        $"Deferred Spawner = null, creating delegate and returning false. Delegate should spawn {actorResource}");

                    void DeferredInvokeSpawn() =>
                        Utils._activateSpawnTurretMethod.Invoke(__instance, new object[] {team, positionA, positionB});

                    var kvp = new KeyValuePair<string, Action>(instanceGUID, DeferredInvokeSpawn);
                    ModState.DeferredInvokeSpawns.Add(kvp);
                    Utils.SpawnFlares(__instance, positionA, positionB, ModInit.modSettings.flareResourceID, 1, __instance.Def.ActivationETA, team.IsLocalPlayer);
//                    var flares = Traverse.Create(__instance).Method("SpawnFlares",
//                        new object[] {positionA, positionA, __instance.Def., 1, 1});
//                    flares.GetValue();
                    return false;
                }

                if (!string.IsNullOrEmpty(ModState.DeferredActorResource))
                {
                    actorResource = ModState.DeferredActorResource;
                    ModInit.modLog?.Info?.Write($"{actorResource} restored from deferredActorResource");
                }

                var pilotID = "pilot_sim_starter_dekker";
                if (!string.IsNullOrEmpty(ModState.PilotOverride))
                {
                    pilotID = ModState.PilotOverride;
                }
                else if (!string.IsNullOrEmpty(__instance.Def.getAbilityDefExtension().CMDPilotOverride))
                {
                    pilotID = __instance.Def.getAbilityDefExtension().CMDPilotOverride;
                }

                ModInit.modLog?.Info?.Write($"Pilot should be {pilotID}");
                dm.PilotDefs.TryGet(pilotID, out var supportPilotDef);
                var cmdLance = Utils.CreateOrFetchCMDLance(teamSelection);

                Quaternion quaternion = Quaternion.LookRotation(positionB - positionA);

                if (actorResource.StartsWith("mechdef_") || actorResource.StartsWith("vehicledef_"))
                {
                    ModInit.modLog?.Info?.Write($"Attempting to spawn {actorResource} as mech.");
                    var spawner = new Classes.CustomSpawner(team, __instance, combat, actorResource, cmdLance, teamSelection, positionA, quaternion, supportHeraldryDef, supportPilotDef);
                    spawner.SpawnBeaconUnitAtLocation();
                    return false;
                }

                if (actorResource.StartsWith("mechdef_") && false)
                {
                    ModInit.modLog?.Info?.Write($"Attempting to spawn {actorResource} as mech.");
                    dm.MechDefs.TryGet(actorResource, out var supportActorMechDef);
                    supportActorMechDef.Refresh();
                    var customEncounterTags = new TagSet(teamSelection.EncounterTags) {"SpawnedFromAbility"};
                    var supportActorMech = ActorFactory.CreateMech(supportActorMechDef, supportPilotDef,
                        customEncounterTags, teamSelection.Combat,
                        teamSelection.GetNextSupportUnitGuid(), "", supportHeraldryDef);
                    supportActorMech.Init(positionA, quaternion.eulerAngles.y, false);
                    supportActorMech.InitGameRep(null);

                    teamSelection.AddUnit(supportActorMech);
                    supportActorMech.AddToTeam(teamSelection);

                    supportActorMech.AddToLance(cmdLance);
                    cmdLance.AddUnitGUID(supportActorMech.GUID);
                    supportActorMech.SetBehaviorTree(BehaviorTreeIDEnum.CoreAITree);
                    //supportActorMech.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(__instance.Combat.BattleTechGame, supportActorMech, BehaviorTreeIDEnum.CoreAITree);
                    //supportActorMech.GameRep.gameObject.SetActive(true);

                    supportActorMech.OnPositionUpdate(positionA, quaternion, -1, true, null, false);
                    supportActorMech.DynamicUnitRole = UnitRole.Brawler;
                    UnitSpawnedMessage message = new UnitSpawnedMessage("FROM_ABILITY", supportActorMech.GUID);
                    __instance.Combat.MessageCenter.PublishMessage(message);
                    //supportActorMech.OnPlayerVisibilityChanged(VisibilityLevel.LOSFull);

                    ModInit.modLog?.Info?.Write($"Added {supportActorMech?.MechDef?.Description?.Id} to SupportUnits");

                    ////////////////

                    //supportActorMech.PlaceFarAwayFromMap();
                    var underMap = supportActorMech.CurrentPosition;
                    underMap.y = -1000f;
                    supportActorMech.TeleportActor(underMap);
                    combat.ItemRegistry.AddItem(supportActorMech);
                    combat.RebuildAllLists();
                    EncounterLayerParent encounterLayerParent = combat.EncounterLayerData.gameObject.GetComponentInParent<EncounterLayerParent>();
                    DropPodUtils.DropPodSpawner dropSpawner = encounterLayerParent.gameObject.GetComponent<DropPodUtils.DropPodSpawner>();
                    if (dropSpawner == null) { dropSpawner = encounterLayerParent.gameObject.AddComponent<DropPodUtils.DropPodSpawner>(); }

                    dropSpawner.Unit = supportActorMech;
                    dropSpawner.Combat = combat;
                    dropSpawner.Parent = UnityGameInstance.BattleTechGame.Combat.EncounterLayerData
                        .GetComponentInParent<EncounterLayerParent>();
                    dropSpawner.DropPodPosition = positionA;
                    dropSpawner.DropPodRotation = quaternion;

                    ModInit.modLog?.Trace?.Write($"DropPodAnim location {positionA} is also {dropSpawner.DropPodPosition}");
                    ModInit.modLog?.Trace?.Write($"Is dropAnim null fuckin somehow? {dropSpawner == null}");
                    dropSpawner.DropPodVfxPrefab = dropSpawner.Parent.DropPodVfxPrefab;
                    dropSpawner.DropPodLandedPrefab = dropSpawner.Parent.dropPodLandedPrefab;
                    dropSpawner.LoadDropPodPrefabs(dropSpawner.DropPodVfxPrefab, dropSpawner.DropPodLandedPrefab);
                    ModInit.modLog?.Trace?.Write($"loaded prefabs success");
                    dropSpawner.StartCoroutine(dropSpawner.StartDropPodAnimation(0f));
                    ModInit.modLog?.Trace?.Write($"started drop pod anim");

                    
                    //supportActorMech.OnPlayerVisibilityChanged(VisibilityLevel.LOSFull);

                    //Utils.DeployEvasion(supportActorMech);
                    ///////////////

                    if (team.IsLocalPlayer && (ModInit.modSettings.commandUseCostsMulti > 0 ||
                                               __instance.Def.getAbilityDefExtension().CBillCost > 0))
                    {
                        var unitName = "";
                        var unitCost = 0;
                        var unitID = "";

                        unitName = supportActorMechDef.Description.UIName;
                        unitID = supportActorMechDef.Description.Id;
                        unitCost = supportActorMechDef.Chassis.Description.Cost;

                        if (ModState.CommandUses.All(x => x.UnitID != actorResource))
                        {
                            var commandUse =
                                new CmdUseInfo(unitID, __instance.Def.Description.Name, unitName, unitCost,
                                    __instance.Def.getAbilityDefExtension().CBillCost);

                            ModState.CommandUses.Add(commandUse);
                            ModInit.modLog?.Info?.Write(
                                $"Added usage cost for {commandUse.CommandName} - {commandUse.UnitName}. UnitUseCost (unadjusted): {unitCost}. Ability Use Cost: {__instance.Def.getAbilityDefExtension().CBillCost}");
                        }
                        else
                        {
                            var cmdUse = ModState.CommandUses.FirstOrDefault(x => x.UnitID == actorResource);
                            if (cmdUse == null)
                            {
                                ModInit.modLog?.Info?.Write($"ERROR: cmdUseInfo was null");
                            }
                            else
                            {
                                cmdUse.UseCount += 1;
                                ModInit.modLog?.Info?.Write(
                                    $"Added usage cost for {cmdUse.CommandName} - {cmdUse.UnitName}. UnitUseCost (unadjusted): {unitCost}. Ability Use Cost: {__instance.Def.getAbilityDefExtension().CBillCost}. Now used {cmdUse.UseCount} times.");
                            }
                        }
                    }
                }
                else if (actorResource.StartsWith("vehicledef_") && false) //disable for CU
                {
                    ModInit.modLog?.Info?.Write($"Attempting to spawn {actorResource} as vehicle.");
                    dm.VehicleDefs.TryGet(actorResource, out var supportActorVehicleDef);
                    supportActorVehicleDef.Refresh();
                    var customEncounterTags = new TagSet(teamSelection.EncounterTags) {"SpawnedFromAbility"};
                    var supportActorVehicle = ActorFactory.CreateVehicle(supportActorVehicleDef, supportPilotDef,
                        customEncounterTags, teamSelection.Combat,
                        teamSelection.GetNextSupportUnitGuid(), "", supportHeraldryDef);
                    supportActorVehicle.Init(positionA, quaternion.eulerAngles.y, false);
                    supportActorVehicle.InitGameRep(null);

                    teamSelection.AddUnit(supportActorVehicle);
                    supportActorVehicle.AddToTeam(teamSelection);

                    supportActorVehicle.AddToLance(cmdLance);
                    cmdLance.AddUnitGUID(supportActorVehicle.GUID);
                    supportActorVehicle.SetBehaviorTree(BehaviorTreeIDEnum.CoreAITree);
                    //supportActorVehicle.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(__instance.Combat.BattleTechGame, supportActorVehicle, BehaviorTreeIDEnum.CoreAITree);
                    //supportActorVehicle.GameRep.gameObject.SetActive(true);

                    supportActorVehicle.OnPositionUpdate(positionA, quaternion, -1, true, null, false);
                    supportActorVehicle.DynamicUnitRole = UnitRole.Vehicle;

                    UnitSpawnedMessage message = new UnitSpawnedMessage("FROM_ABILITY", supportActorVehicle.GUID);

                    __instance.Combat.MessageCenter.PublishMessage(message);
                    //supportActorVehicle.OnPlayerVisibilityChanged(VisibilityLevel.LOSFull);

                    //Utils.DeployEvasion(supportActorVehicle);

                    ModInit.modLog?.Info?.Write(
                        $"Added {supportActorVehicle?.VehicleDef?.Description?.Id} to SupportUnits");


                    ////////////////

                    //supportActorMech.PlaceFarAwayFromMap();
                    var underMap = supportActorVehicle.CurrentPosition;
                    underMap.y = -1000f;
                    supportActorVehicle.TeleportActor(underMap);
                    combat.ItemRegistry.AddItem(supportActorVehicle);
                    combat.RebuildAllLists();
                    EncounterLayerParent encounterLayerParent = combat.EncounterLayerData.gameObject.GetComponentInParent<EncounterLayerParent>();
                    DropPodUtils.DropPodSpawner dropSpawner = encounterLayerParent.gameObject.GetComponent<DropPodUtils.DropPodSpawner>();
                    if (dropSpawner == null) { dropSpawner = encounterLayerParent.gameObject.AddComponent<DropPodUtils.DropPodSpawner>(); }

                    dropSpawner.Unit = supportActorVehicle;
                    dropSpawner.Combat = combat;
                    dropSpawner.Parent = UnityGameInstance.BattleTechGame.Combat.EncounterLayerData
                        .GetComponentInParent<EncounterLayerParent>();
                    dropSpawner.DropPodPosition = positionA;
                    dropSpawner.DropPodRotation = quaternion;

                    ModInit.modLog?.Trace?.Write($"DropPodAnim location {positionA} is also {dropSpawner.DropPodPosition}");
                    ModInit.modLog?.Trace?.Write($"Is dropAnim null fuckin somehow? {dropSpawner == null}");
                    dropSpawner.DropPodVfxPrefab = dropSpawner.Parent.DropPodVfxPrefab;
                    dropSpawner.DropPodLandedPrefab = dropSpawner.Parent.dropPodLandedPrefab;
                    dropSpawner.LoadDropPodPrefabs(dropSpawner.DropPodVfxPrefab, dropSpawner.DropPodLandedPrefab);
                    ModInit.modLog?.Trace?.Write($"loaded prefabs success");
                    dropSpawner.StartCoroutine(dropSpawner.StartDropPodAnimation(0f));
                    ModInit.modLog?.Trace?.Write($"started drop pod anim");


                    //supportActorMech.OnPlayerVisibilityChanged(VisibilityLevel.LOSFull);

                    //Utils.DeployEvasion(supportActorVehicle);
                    ///////////////

                    if (team.IsLocalPlayer && (ModInit.modSettings.commandUseCostsMulti > 0 ||
                                               __instance.Def.getAbilityDefExtension().CBillCost > 0))
                    {
                        var unitName = "";
                        var unitCost = 0;
                        var unitID = "";

                        unitName = supportActorVehicleDef.Description.UIName;
                        unitID = supportActorVehicleDef.Description.Id;
                        unitCost = supportActorVehicleDef.Chassis.Description.Cost;

                        if (ModState.CommandUses.All(x => x.UnitID != actorResource))
                        {
                            var commandUse =
                                new CmdUseInfo(unitID, __instance.Def.Description.Name, unitName, unitCost,
                                    __instance.Def.getAbilityDefExtension().CBillCost);

                            ModState.CommandUses.Add(commandUse);
                            ModInit.modLog?.Info?.Write(
                                $"Added usage cost for {commandUse.CommandName} - {commandUse.UnitName}. UnitUseCost (unadjusted): {unitCost}. Ability Use Cost: {__instance.Def.getAbilityDefExtension().CBillCost}");
                        }
                        else
                        {
                            var cmdUse = ModState.CommandUses.FirstOrDefault(x => x.UnitID == actorResource);
                            if (cmdUse == null)
                            {
                                ModInit.modLog?.Info?.Write($"ERROR: cmdUseInfo was null");
                            }
                            else
                            {
                                cmdUse.UseCount += 1;
                                ModInit.modLog?.Info?.Write(
                                    $"Added usage cost for {cmdUse.CommandName} - {cmdUse.UnitName}. UnitUseCost (unadjusted): {unitCost}. Ability Use Cost: {__instance.Def.getAbilityDefExtension().CBillCost}. Now used {cmdUse.UseCount} times.");
                            }
                        }
                    }
                }

                else
                {
                    ModInit.modLog?.Info?.Write($"Attempting to spawn {actorResource} as turret.");
                    var spawnTurretMethod = Traverse.Create(__instance).Method("SpawnTurret",
                        new object[] {teamSelection, actorResource, positionA, quaternion});
                    var turretActor = spawnTurretMethod.GetValue<AbstractActor>();

                    teamSelection.AddUnit(turretActor);
                    turretActor.AddToTeam(teamSelection);

                    turretActor.AddToLance(cmdLance);
                    cmdLance.AddUnitGUID(turretActor.GUID);
                    turretActor.SetBehaviorTree(BehaviorTreeIDEnum.CoreAITree);
                    //turretActor.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(__instance.Combat.BattleTechGame, turretActor, BehaviorTreeIDEnum.CoreAITree);

                    turretActor.OnPositionUpdate(positionA, quaternion, -1, true, null, false);
                    turretActor.DynamicUnitRole = UnitRole.Turret;

                    UnitSpawnedMessage message = new UnitSpawnedMessage("FROM_ABILITY", turretActor.GUID);

                    __instance.Combat.MessageCenter.PublishMessage(message);
                    //turretActor.OnPlayerVisibilityChanged(VisibilityLevel.LOSFull);

                    //supportActorMech.PlaceFarAwayFromMap();
                    var underMap = turretActor.CurrentPosition;
                    underMap.y = -1000f;
                    turretActor.TeleportActor(underMap);
                    combat.ItemRegistry.AddItem(turretActor);
                    combat.RebuildAllLists();
                    EncounterLayerParent encounterLayerParent = combat.EncounterLayerData.gameObject.GetComponentInParent<EncounterLayerParent>();
                    DropPodUtils.DropPodSpawner dropSpawner = encounterLayerParent.gameObject.GetComponent<DropPodUtils.DropPodSpawner>();
                    if (dropSpawner == null) { dropSpawner = encounterLayerParent.gameObject.AddComponent<DropPodUtils.DropPodSpawner>(); }

                    dropSpawner.Unit = turretActor;
                    dropSpawner.Combat = combat;
                    dropSpawner.Parent = UnityGameInstance.BattleTechGame.Combat.EncounterLayerData
                        .GetComponentInParent<EncounterLayerParent>();
                    dropSpawner.DropPodPosition = positionA;
                    dropSpawner.DropPodRotation = quaternion;

                    ModInit.modLog?.Trace?.Write($"DropPodAnim location {positionA} is also {dropSpawner.DropPodPosition}");
                    ModInit.modLog?.Trace?.Write($"Is dropAnim null fuckin somehow? {dropSpawner == null}");
                    dropSpawner.DropPodVfxPrefab = dropSpawner.Parent.DropPodVfxPrefab;
                    dropSpawner.DropPodLandedPrefab = dropSpawner.Parent.dropPodLandedPrefab;
                    dropSpawner.LoadDropPodPrefabs(dropSpawner.DropPodVfxPrefab, dropSpawner.DropPodLandedPrefab);
                    ModInit.modLog?.Trace?.Write($"loaded prefabs success");
                    dropSpawner.StartCoroutine(dropSpawner.StartDropPodAnimation(0f));
                    ModInit.modLog?.Trace?.Write($"started drop pod anim");

                    ///////////////

                    if (team.IsLocalPlayer && (ModInit.modSettings.commandUseCostsMulti > 0 ||
                                               __instance.Def.getAbilityDefExtension().CBillCost > 0))
                    {
                        var unitName = "";
                        var unitCost = 0;
                        var unitID = "";

                        dm.TurretDefs.TryGet(actorResource, out var turretDef);
                        turretDef.Refresh();
                        unitName = turretDef.Description.UIName;
                        unitID = turretDef.Description.Id;
                        unitCost = turretDef.Chassis.Description.Cost;


                        if (ModState.CommandUses.All(x => x.UnitID != actorResource))
                        {
                            var commandUse =
                                new CmdUseInfo(unitID, __instance.Def.Description.Name, unitName, unitCost,
                                    __instance.Def.getAbilityDefExtension().CBillCost);

                            ModState.CommandUses.Add(commandUse);
                            ModInit.modLog?.Info?.Write(
                                $"Added usage cost for {commandUse.CommandName} - {commandUse.UnitName}. UnitUseCost (unadjusted): {unitCost}. Ability Use Cost: {__instance.Def.getAbilityDefExtension().CBillCost}");
                        }
                        else
                        {
                            var cmdUse = ModState.CommandUses.FirstOrDefault(x => x.UnitID == actorResource);
                            if (cmdUse == null)
                            {
                                ModInit.modLog?.Info?.Write($"ERROR: cmdUseInfo was null");
                            }
                            else
                            {
                                cmdUse.UseCount += 1;
                                ModInit.modLog?.Info?.Write(
                                    $"Added usage cost for {cmdUse.CommandName} - {cmdUse.UnitName}. UnitUseCost (unadjusted): {unitCost}. Ability Use Cost: {__instance.Def.getAbilityDefExtension().CBillCost}. Now used {cmdUse.UseCount} times.");
                            }
                        }
                    }
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(Ability), "SpawnFlares")]
        public static class Ability_SpawnFlares
        {
            static bool Prepare() => false; //disabled
            private static bool Prefix(Ability __instance, Vector3 positionA, Vector3 positionB, string prefabName,
                int numFlares, int numPhases)
            {
                if (__instance.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return true;
                Vector3 b = (positionB - positionA) / (float) (numFlares - 1);

                Vector3 line = positionB - positionA;
                Vector3 left = Vector3.Cross(line, Vector3.up).normalized;
                Vector3 right = -left;

                var startLeft = positionA + (left * __instance.Def.FloatParam1);
                var startRight = positionA + (right * __instance.Def.FloatParam1);

                Vector3 vector = positionA;

                vector.y = __instance.Combat.MapMetaData.GetLerpedHeightAt(vector, false);
                startLeft.y = __instance.Combat.MapMetaData.GetLerpedHeightAt(startLeft, false);
                startRight.y = __instance.Combat.MapMetaData.GetLerpedHeightAt(startRight, false);
                List<ObjectSpawnData> list = new List<ObjectSpawnData>();
                for (int i = 0; i < numFlares; i++)
                {
                    ObjectSpawnData item = new ObjectSpawnData(prefabName, vector, Quaternion.identity, true, false);
                    list.Add(item);
                    vector += b;
                    vector.y = __instance.Combat.MapMetaData.GetLerpedHeightAt(vector, false);
                }

                for (int i = 0; i < numFlares; i++)
                {
                    ObjectSpawnData item = new ObjectSpawnData(prefabName, startLeft, Quaternion.identity, true, false);
                    list.Add(item);
                    startLeft += b;
                    startLeft.y = __instance.Combat.MapMetaData.GetLerpedHeightAt(startLeft, false);
                }

                for (int i = 0; i < numFlares; i++)
                {
                    ObjectSpawnData item =
                        new ObjectSpawnData(prefabName, startRight, Quaternion.identity, true, false);
                    list.Add(item);
                    startRight += b;
                    startRight.y = __instance.Combat.MapMetaData.GetLerpedHeightAt(startRight, false);
                }

                SpawnObjectSequence spawnObjectSequence = new SpawnObjectSequence(__instance.Combat, list);
                __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(spawnObjectSequence));
                List<ObjectSpawnData> spawnedObjects = spawnObjectSequence.spawnedObjects;
                CleanupObjectSequence eventSequence = new CleanupObjectSequence(__instance.Combat, spawnedObjects);
                TurnEvent tEvent = new TurnEvent(Guid.NewGuid().ToString(), __instance.Combat, numPhases, null,
                    eventSequence, __instance.Def, false);
                __instance.Combat.TurnDirector.AddTurnEvent(tEvent);
                return false;
            }
        }


        [HarmonyPatch(typeof(CombatGameState), "OnCombatGameDestroyed")]
        public static class CombatGameState_OnCombatGameDestroyed
        {
            private static void Postfix(CombatGameState __instance)
            {
                ModState.ResetAll();
                StrategicSelection.StrategicTargetIndicatorsManager.Clear();
            }
        }


        [HarmonyPatch(typeof(AbstractActor), "OnPhaseBegin")]
        public static class AbstractActor_OnPhaseBegin
        {
            public static void Postfix(AbstractActor __instance)
            {
                if (ModState.ResupplyShutdownPhases.ContainsKey(__instance.GUID))
                {
                    ModState.ResupplyShutdownPhases[__instance.GUID]--;
                    if (ModState.ResupplyShutdownPhases[__instance.GUID] <= 0)
                        ModState.ResupplyShutdownPhases.Remove(__instance.GUID);
                }
            }
        }

        [HarmonyPatch(typeof(Team), "OnRoundBegin")]
        public static class TurnActor_OnRoundBegin
        {
            public static void Postfix(Team __instance)
            {
                if (__instance.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                foreach (var ability in ModState.CommandAbilities)
                {
                    ability.OnNewRound();
                }

                foreach (var despawn in ModState.DeferredDespawnersFromStrafe)
                {
                    var msg = new DespawnActorMessage(EncounterLayerData.MapLogicGuid, despawn.Key, (DeathMethod)DespawnFloatieMessage.Escaped);
                    Utils._despawnActorMethod.Invoke(despawn.Value, new object[] {msg});
                }
                
                //var team = __instance as Team;

                if (__instance?.units != null)
                    foreach (var unit in __instance?.units)
                    {
                        var rep = unit.GameRep as PilotableActorRepresentation;
                        rep?.ClearForcedPlayerVisibilityLevel(__instance.Combat.GetAllLivingCombatants());
                    }
                //                team?.ResetUnitVisibilityLevels();
                __instance?.RebuildVisibilityCacheAllUnits(__instance.Combat.GetAllLivingCombatants());

            }
        }

        [HarmonyPatch(typeof(TurnDirector), "StartFirstRound")]
        public static class TurnDirector_StartFirstRound
        {
            public static void Postfix(TurnDirector __instance)
            {

                if (ModState.DeferredInvokeBattleArmor.Count > 0)
                {
                    for (var index = 0; index < ModState.DeferredInvokeBattleArmor.Count; index++)
                    {
                        var spawn = ModState.DeferredInvokeBattleArmor[index].Value;
                        ModInit.modLog?.Info?.Write(
                            $"[TurnDirector.StartFirstRound] Found deferred spawner at index {index} of {ModState.DeferredInvokeBattleArmor.Count - 1}, invoking and trying to spawn a battle armor of some kind.");
                        ModState.DeferredBattleArmorSpawnerFromDelegate = true;
                        spawn();

                    }
                    ModState.ResetDeferredBASpawners();
                }

                if (__instance.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;

                var playerTeam = __instance.Combat.Teams.First(x => x.IsLocalPlayer);
                var dm = playerTeam.Combat.DataManager;

                foreach (var abilityDefKVP in dm.AbilityDefs.Where(x =>
                    x.Value.specialRules == AbilityDef.SpecialRules.SpawnTurret ||
                    x.Value.specialRules == AbilityDef.SpecialRules.Strafe))
                {

                    if (playerTeam.units.Any(x => x.GetPilot().Abilities.Any(y => y.Def == abilityDefKVP.Value)) ||
                        playerTeam.units.Any(x => x.ComponentAbilities.Any(z => z.Def == abilityDefKVP.Value)))
                    {
                        //only do things for abilities that pilots have? move things here. also move AbstractActor initialization to ability start to minimize neutralTeam think time, etc. and then despawn? - done
                        var ability = new Ability(abilityDefKVP.Value);
                        ability.Init(playerTeam.Combat);
                        if (ModState.CommandAbilities.All(x => x != ability))
                        {
                            ModState.CommandAbilities.Add(ability);
                        }

                        ModInit.modLog?.Info?.Write($"[TurnDirector.StartFirstRound] Added {ability?.Def?.Id} to CommandAbilities");

                    }
                }
                __instance.Combat.UpdateResupplyTeams();
                __instance.Combat.UpdateResupplyAbilitiesGetAllLivingActors();
            }
        }

        [HarmonyPatch(typeof(TurnDirector), "IncrementActiveTurnActor")]
        public static class TurnDirector_IncrementActiveTurnActor
        {
            public static void Prefix(TurnDirector __instance)
            {
                if (__instance.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                if (ModState.DeferredInvokeSpawns.Count > 0 && __instance.ActiveTurnActor is Team activeTeam &&
                    activeTeam.IsLocalPlayer)
                {
                    for (var index = 0; index < ModState.DeferredInvokeSpawns.Count; index++)
                    {
                        var spawn = ModState.DeferredInvokeSpawns[index].Value;
                        var resource = ModState.DeferredInvokeSpawns[index].Key.Split('@');
                        ModState.DeferredActorResource = resource[1];
                        ModInit.modLog?.Info?.Write(
                            $"Found deferred spawner at index {index} of {ModState.DeferredInvokeSpawns.Count - 1}, invoking and trying to spawn {ModState.DeferredActorResource}.");
                        ModState.DeferredSpawnerFromDelegate = true;
                        spawn();
                        ModState.ResetDelegateInfos();
                    }

                    ModState.ResetDeferredSpawners();
                }
            }
        }

        [HarmonyPatch(typeof(CombatHUDActionButton), "ActivateCommandAbility",
            new Type[] {typeof(string), typeof(Vector3), typeof(Vector3)})]
        public static class CombatHUDActionButton_ActivateCommandAbility
        {
            public static bool Prefix(CombatHUDActionButton __instance, string teamGUID,
                Vector3 positionA, //prefix to try and make command abilities behave like normal ones
                Vector3 positionB)
            {
                var HUD = Traverse.Create(__instance).Property("HUD").GetValue<CombatHUD>();
                var theActor = HUD.SelectedActor;
                if (theActor == null) return true;
                var combat = HUD.Combat;
                if (combat.ActiveContract.ContractTypeValue.IsSkirmish) return true;
                if (__instance.Ability != null &&
                    __instance.Ability.Def.ActivationTime == AbilityDef.ActivationTiming.CommandAbility &&
                    (__instance.Ability.Def.Targeting == AbilityDef.TargetingType.CommandTargetTwoPoints ||
                     __instance.Ability.Def.Targeting == AbilityDef.TargetingType.CommandSpawnPosition))
                {
                    MessageCenterMessage messageCenterMessage = new AbilityInvokedMessage(theActor.GUID, theActor.GUID,
                        __instance.Ability.Def.Id, positionA, positionB);
                    messageCenterMessage.IsNetRouted = true;
                    combat.MessageCenter.PublishMessage(messageCenterMessage);
                    messageCenterMessage = new AbilityConfirmedMessage(theActor.GUID, theActor.GUID,
                        __instance.Ability.Def.Id, positionA, positionB);
                    messageCenterMessage.IsNetRouted = true;
                    combat.MessageCenter.PublishMessage(messageCenterMessage);
                    __instance.DisableButton();
                }

                return false;
            }

            public static void Postfix(CombatHUDActionButton __instance, string teamGUID, Vector3 positionA,
                Vector3 positionB)
            {
                if (UnityGameInstance.BattleTechGame.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                var def = __instance.Ability.Def;
                var HUD = Traverse.Create(__instance).Property("HUD").GetValue<CombatHUD>();
                var theActor = HUD.SelectedActor;
                if (theActor == null) return;
                if (def.specialRules == AbilityDef.SpecialRules.Strafe &&
                    ModInit.modSettings.strafeEndsActivation)
                {
                    if (theActor is Mech mech)
                    {
                        mech.GenerateAndPublishHeatSequence(-1, true, false, theActor.GUID);
                    }

                    var sequence = theActor.DoneWithActor();
                    theActor.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(sequence));
                    //theActor.OnActivationEnd(theActor.GUID, __instance.GetInstanceID());
                    return;
                }

                if (def.specialRules == AbilityDef.SpecialRules.SpawnTurret &&
                    ModInit.modSettings.spawnTurretEndsActivation)
                {
                    if (theActor is Mech mech)
                    {
                        mech.GenerateAndPublishHeatSequence(-1, true, false, theActor.GUID);
                    }
                    var sequence = theActor.DoneWithActor();
                    theActor.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(sequence));
                    //theActor.OnActivationEnd(theActor.GUID, __instance.GetInstanceID());
                }
                //need to make sure proccing from equipment button also ends turn?
            }
        }

        [HarmonyPatch(typeof(CombatHUDEquipmentSlot), "ActivateCommandAbility",
            new Type[] {typeof(string), typeof(Vector3), typeof(Vector3)})]
        public static class CombatHUDEquipmentSlot_ActivateCommandAbility
        {
            public static bool Prefix(CombatHUDEquipmentSlot __instance, string teamGUID, Vector3 positionA,
                Vector3 positionB)
            {
                var HUD = Traverse.Create(__instance).Property("HUD").GetValue<CombatHUD>();
                var theActor = HUD.SelectedActor;
                if (theActor == null) return true;
                var combat = HUD.Combat;
                if (combat.ActiveContract.ContractTypeValue.IsSkirmish) return true;
                if (__instance.Ability != null &&
                    __instance.Ability.Def.ActivationTime == AbilityDef.ActivationTiming.CommandAbility &&
                    (__instance.Ability.Def.Targeting == AbilityDef.TargetingType.CommandTargetTwoPoints ||
                     __instance.Ability.Def.Targeting == AbilityDef.TargetingType.CommandSpawnPosition))
                {
                    MessageCenterMessage messageCenterMessage = new AbilityInvokedMessage(theActor.GUID, theActor.GUID,
                        __instance.Ability.Def.Id, positionA, positionB);
                    messageCenterMessage.IsNetRouted = true;
                    combat.MessageCenter.PublishMessage(messageCenterMessage);
                    messageCenterMessage = new AbilityConfirmedMessage(theActor.GUID, theActor.GUID,
                        __instance.Ability.Def.Id, positionA, positionB);
                    messageCenterMessage.IsNetRouted = true;
                    combat.MessageCenter.PublishMessage(messageCenterMessage);
                    __instance.DisableButton();
                }

                return false;
            }

            public static void Postfix(CombatHUDEquipmentSlot __instance, string teamGUID, Vector3 positionA,
                Vector3 positionB)
            {
                if (__instance.Ability.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                var def = __instance.Ability.Def;
                var HUD = Traverse.Create(__instance).Property("HUD").GetValue<CombatHUD>();
                var theActor = HUD.SelectedActor;
                if (theActor == null) return;
                if (def.specialRules == AbilityDef.SpecialRules.Strafe &&
                    ModInit.modSettings.strafeEndsActivation)
                {
                    if (theActor is Mech mech)
                    {
                        mech.GenerateAndPublishHeatSequence(-1, true, false, theActor.GUID);
                    }

                    var sequence = theActor.DoneWithActor();
                    theActor.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(sequence));
                    //theActor.OnActivationEnd(theActor.GUID, __instance.GetInstanceID());
                    return;
                }

                if (def.specialRules == AbilityDef.SpecialRules.SpawnTurret &&
                    ModInit.modSettings.spawnTurretEndsActivation)
                {
                    if (theActor is Mech mech)
                    {
                        mech.GenerateAndPublishHeatSequence(-1, true, false, theActor.GUID);
                    }
                    var sequence = theActor.DoneWithActor();
                    theActor.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(sequence));
                    //theActor.OnActivationEnd(theActor.GUID, __instance.GetInstanceID());
                }
            }
        }

        [HarmonyPatch(typeof(SelectionStateCommand), "OnAddToStack")]
        public static class SelectionStateCommand_OnAddToStack
        {
            public static void Postfix(SelectionStateCommand __instance)
            {
                if (UnityGameInstance.BattleTechGame.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                var HUD = Traverse.Create(__instance).Property("HUD").GetValue<CombatHUD>();
                var theActor = HUD.SelectedActor;
                if (theActor == null) return;
                CombatTargetingReticle.Instance.HideReticle();
                var maxRange = Mathf.RoundToInt(__instance.FromButton.Ability.Def.IntParam2);
                CombatTargetingReticle.Instance.ShowRangeIndicators(theActor.CurrentPosition, 0f, maxRange, false, true);
                CombatTargetingReticle.Instance.UpdateRangeIndicator(theActor.CurrentPosition, false, true);
                CombatTargetingReticle.Instance.ShowReticle();
            }
        }

        [HarmonyPatch(typeof(SelectionStateCommandSpawnTarget), "ProcessMousePos")]
        public static class SelectionStateCommandSpawnTarget_ProcessMousePos
        {
            public static bool Prefix(SelectionStateCommandSpawnTarget __instance, Vector3 worldPos,
                int ___numPositionsLocked)
            {
                if (UnityGameInstance.BattleTechGame.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return true;
                CombatSpawningReticle.Instance.ShowReticle();
                var HUD = Traverse.Create(__instance).Property("HUD").GetValue<CombatHUD>();
                var theActor = HUD.SelectedActor;
                if (theActor == null) return true;
                var distance = Mathf.RoundToInt(Vector3.Distance(theActor.CurrentPosition, worldPos));
                var maxRange = Mathf.RoundToInt(__instance.FromButton.Ability.Def.IntParam2);
                //CombatTargetingReticle.Instance.ShowRangeIndicators(theActor.CurrentPosition, 0f, maxRange, true, true);
                //CombatTargetingReticle.Instance.ShowReticle();
                if (__instance.FromButton.Ability.Def.specialRules == AbilityDef.SpecialRules.SpawnTurret &&
                    distance > maxRange && ___numPositionsLocked == 0)
                {
                    ModState.OutOfRange = true;
                    CombatSpawningReticle.Instance.HideReticle();
                    //CombatTargetingReticle.Instance.HideReticle();
                    //                    ModInit.modLog?.Info?.Write($"Cannot spawn turret with coordinates farther than __instance.Ability.Def.IntParam2: {__instance.FromButton.Ability.Def.IntParam2}");
                    return false;
                }

                ModState.OutOfRange = false;
                return true;
            }
        }

        [HarmonyPatch(typeof(SelectionStateCommandSpawnTarget), "OnInactivate")]
        public static class SelectionStateCommandSpawnTarget_OnInactivate
        {
            public static void Postfix(SelectionStateCommandSpawnTarget __instance)
            {
                CombatTargetingReticle.Instance.HideReticle();
            }
        }

        [HarmonyPatch(typeof(SelectionStateCommandTargetTwoPoints), "ProcessMousePos")]
        public static class SelectionStateCommandTargetTwoPoints_ProcessMousePos
        {
            public static bool Prefix(SelectionStateCommandTargetTwoPoints __instance, Vector3 worldPos,
                int ___numPositionsLocked)
            {
                if (UnityGameInstance.BattleTechGame.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return true;
                
                var HUD = Traverse.Create(__instance).Property("HUD").GetValue<CombatHUD>();
                var positionA = Traverse.Create(__instance).Property("positionA").GetValue<Vector3>();
                var positionB = Traverse.Create(__instance).Property("positionB").GetValue<Vector3>();

                var theActor = HUD.SelectedActor;
                if (theActor == null) return true;
                var distance = Mathf.RoundToInt(Vector3.Distance(theActor.CurrentPosition, worldPos));
                var distanceToA = Mathf.RoundToInt(Vector3.Distance(theActor.CurrentPosition, positionA));
                var distanceToB = Mathf.RoundToInt(Vector3.Distance(theActor.CurrentPosition, positionB));

                var maxRange = Mathf.RoundToInt(__instance.FromButton.Ability.Def.IntParam2);
                var radius = __instance.FromButton.Ability.Def.FloatParam1;
                CombatTargetingReticle.Instance.UpdateReticle(positionA, positionB, radius, false);
                //CombatTargetingReticle.Instance.ShowRangeIndicators(theActor.CurrentPosition, 0f, maxRange, true, true);
                CombatTargetingReticle.Instance.ShowReticle();
                if (__instance.FromButton.Ability.Def.specialRules == AbilityDef.SpecialRules.Strafe &&
                    (distance > maxRange && ___numPositionsLocked == 0) ||
                    (distanceToA > maxRange && ___numPositionsLocked == 1))
                {
                    ModState.OutOfRange = true;
                    CombatTargetingReticle.Instance.HideReticle();
//                    ModInit.modLog?.Info?.Write($"Cannot strafe with coordinates farther than __instance.Ability.Def.IntParam2: {__instance.FromButton.Ability.Def.IntParam2}");
                    return false;
                }

                ModState.OutOfRange = false;
                return true;
            }
        }

        [HarmonyPatch(typeof(SelectionStateCommandTargetTwoPoints), "ProcessLeftClick")]
        public static class SelectionStateCommandTargetTwoPoints_ProcessLeftClick
        {
            public static bool Prefix(SelectionStateCommandTargetTwoPoints __instance, Vector3 worldPos,
                int ___numPositionsLocked, ref bool __result)
            {
                if (UnityGameInstance.BattleTechGame.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return true;
                var dm = __instance.FromButton.Ability.Combat.DataManager;
                var hk = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                var actorResource = __instance.FromButton.Ability.Def.ActorResource;
                if (___numPositionsLocked < 1)
                {
                    ModState.PopupActorResource = actorResource;
                }
                if (hk && string.IsNullOrEmpty(ModState.DeferredActorResource) &&
                    !ModState.OutOfRange)
                {
                    var beaconDescs = "";
                    var type = "";
                    if (actorResource.StartsWith("mechdef_"))
                    {
                        dm.MechDefs.TryGet(actorResource, out var unit);
                        beaconDescs = $"1 (DEFAULT): {unit?.Description?.UIName ?? unit?.Description?.Name}\n\n";
                        type = "mechdef_";
                    }

                    else if (actorResource.StartsWith("vehicledef_"))
                    {
                        dm.VehicleDefs.TryGet(actorResource, out var unit);
                        beaconDescs = $"1 (DEFAULT): {unit?.Description?.UIName ?? unit?.Description?.Name}\n\n";
                        type = "vehicledef_";
                    }
                    else
                    {
                        dm.TurretDefs.TryGet(actorResource, out var unit);
                        beaconDescs = $"1 (DEFAULT): {unit?.Description?.UIName ?? unit?.Description?.Name}\n\n";
                        type = "turretdef_";
                    }

                    var beacons = new List<MechComponentRef>();
                    if (__instance.FromButton.Ability.Def.specialRules == AbilityDef.SpecialRules.SpawnTurret)
                    {
                        beacons = Utils.GetOwnedDeploymentBeaconsOfByTypeAndTag(type, "CanSpawnTurret",
                            __instance.FromButton.Ability.Def.StringParam2);
                    }
                    else if (__instance.FromButton.Ability.Def.specialRules == AbilityDef.SpecialRules.Strafe)
                    {
                        beacons = Utils.GetOwnedDeploymentBeaconsOfByTypeAndTag(type, "CanStrafe",
                            __instance.FromButton.Ability.Def.StringParam2);
                    }

                    beacons.Sort((MechComponentRef x, MechComponentRef y) =>
                        string.CompareOrdinal(x.Def.Description.UIName, y.Def.Description.UIName));



                    ModInit.modLog?.Info?.Write("sorted beacons at SSCT2Pts");

                    for (var index = 0; index < beacons.Count; index++)
                    {
                        var beacon = beacons[index];
                        var id = beacon.Def.ComponentTags.FirstOrDefault(x =>
                            x.StartsWith("mechdef_") || x.StartsWith("vehicledef_") ||
                            x.StartsWith("turretdef_"));

                        var waveString = beacon.Def.ComponentTags.FirstOrDefault(x => x.StartsWith("StrafeWaves_"));
                        int.TryParse(waveString?.Substring(11), out var waves);
                        var waveDesc = "";
                        if (waves > 0)
                        {
                            waveDesc = $"- Waves: {waves}";
                        }

                        var aoeDesc = "";
                        if (beacon.IsAOEStrafe(__instance.FromButton.Ability.Def.specialRules == AbilityDef.SpecialRules.Strafe))
                        {
                            aoeDesc = $" THIS IS AN AOE ATTACK!";
                        }

                        if (id.StartsWith("mechdef_"))
                        {
                            dm.MechDefs.TryGet(id, out var beaconunit);
                            if (ModState.DeploymentAssetsStats != null)
                                beaconDescs +=
                                    $"{index + 2}: {beaconunit?.Description?.UIName ?? beaconunit?.Description?.Name} {waveDesc} - You have {ModState.DeploymentAssetsStats.FirstOrDefault(x => x.ID == id).contractUses} remaining.{aoeDesc}\n\n";
                        }
                        else if (id.StartsWith("vehicledef_"))
                        {
                            dm.VehicleDefs.TryGet(id, out var beaconunit);
                            beaconDescs +=
                                $"{index + 2}: {beaconunit?.Description?.UIName ?? beaconunit?.Description?.Name} {waveDesc} - You have {ModState.DeploymentAssetsStats.FirstOrDefault(x => x.ID == id).contractUses} remaining.{aoeDesc}\n\n";
                        }
                        else
                        {
                            dm.TurretDefs.TryGet(id, out var beaconunit);
                            beaconDescs +=
                                $"{index + 2}: {beaconunit?.Description?.UIName ?? beaconunit?.Description?.Name} {waveDesc} - You have {ModState.DeploymentAssetsStats.FirstOrDefault(x => x.ID == id).contractUses} remaining.{aoeDesc}\n\n";
                        }
                    }

                    var popup = GenericPopupBuilder
                        .Create("Select a unit to deploy",
                            beaconDescs)
                        .AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants
                            .PopupBackfill));
                    popup.AlwaysOnTop = true;
                    popup.AddButton("1.", () => { });
                    ModInit.modLog?.Info?.Write(
                        $"Added button for 1.");
                    switch (beacons.Count)
                    {
                        case 0:
                        {
                            goto RenderNow;
                        }
                        case 1:
                        {
                            var beacon = beacons[0];
                            var id = beacon.Def.ComponentTags.FirstOrDefault((string x) =>
                                x.StartsWith("mechdef_") || x.StartsWith("vehicledef_") ||
                                x.StartsWith("turretdef_"));
                            var pilotID = beacon.Def.ComponentTags.FirstOrDefault(x => x.StartsWith("StratOpsPilot_"))
                                ?.Remove(0, 14);
                            var waveString = beacon.Def.ComponentTags.FirstOrDefault(x => x.StartsWith("StrafeWaves_"));
                            int.TryParse(waveString?.Substring(11), out var waves);
                                ModInit.modLog?.Info?.Write(
                                $"beacon for button 2. will be {beacon.Def.Description.Name}, ID will be {id}, pilot will be {pilotID}");
                            popup.AddButton("2.", (Action) (() =>
                            {
                                ModState.StrafeWaves = waves;
                                ModState.PopupActorResource = id;
                                ModState.PilotOverride = pilotID;
                                ModState.IsStrafeAOE = beacon.IsAOEStrafe(
                                    __instance.FromButton.Ability.Def.specialRules == AbilityDef.SpecialRules.Strafe);
                                ModInit.modLog?.Info?.Write(
                                    $"Player pressed {id} with pilot {pilotID}. Now -{ModState.PopupActorResource}- and pilot -{ModState.PilotOverride}- should be the same.");
                            }));
                            goto RenderNow;
                        }
                        case 2:
                        {
                            var beacon = beacons[1];
                            var id = beacon.Def.ComponentTags.FirstOrDefault((string x) =>
                                x.StartsWith("mechdef_") || x.StartsWith("vehicledef_") ||
                                x.StartsWith("turretdef_"));
                            var pilotID = beacon.Def.ComponentTags.FirstOrDefault(x => x.StartsWith("StratOpsPilot_"))
                                ?.Remove(0, 14);
                            var waveString = beacon.Def.ComponentTags.FirstOrDefault(x => x.StartsWith("StrafeWaves_"));
                            int.TryParse(waveString?.Substring(11), out var waves);
                                ModInit.modLog?.Info?.Write(
                                $"beacon for button 3. will be {beacon.Def.Description.Name}, ID will be {id}, pilot will be {pilotID}");
                            var id1 = id;
                            var pilotID1 = pilotID;
                            var waves1 = waves;
                            var beacon1 = beacon;
                            popup.AddButton("3.", (Action) (() =>
                            {
                                ModState.StrafeWaves = waves1;
                                ModState.PopupActorResource = id1;
                                ModState.PilotOverride = pilotID1;
                                ModState.IsStrafeAOE = beacon1.IsAOEStrafe(
                                    __instance.FromButton.Ability.Def.specialRules == AbilityDef.SpecialRules.Strafe);
                                ModInit.modLog?.Info?.Write(
                                    $"Player pressed {id1} with pilot {pilotID1}. Now -{ModState.PopupActorResource}- and pilot -{ModState.PilotOverride}- should be the same.");
                            }));

                            beacon = beacons[0];
                            id = beacon.Def.ComponentTags.FirstOrDefault((string x) =>
                                x.StartsWith("mechdef_") || x.StartsWith("vehicledef_") ||
                                x.StartsWith("turretdef_"));
                            pilotID = beacon.Def.ComponentTags.FirstOrDefault(x => x.StartsWith("StratOpsPilot_"))
                                ?.Remove(0, 14);
                            waveString = beacon.Def.ComponentTags.FirstOrDefault(x => x.StartsWith("StrafeWaves_"));
                            int.TryParse(waveString?.Substring(11), out waves);
                                ModInit.modLog?.Info?.Write(
                                $"beacon for button 2. will be {beacon.Def.Description.Name}, ID will be {id}, pilot will be {pilotID}");
                            popup.AddButton("2.", (Action) (() =>
                            {
                                ModState.StrafeWaves = waves;
                                ModState.PopupActorResource = id;
                                ModState.PilotOverride = pilotID;
                                ModState.IsStrafeAOE = beacon.IsAOEStrafe(
                                    __instance.FromButton.Ability.Def.specialRules == AbilityDef.SpecialRules.Strafe);
                                ModInit.modLog?.Info?.Write(
                                    $"Player pressed {id} with pilot {pilotID}. Now -{ModState.PopupActorResource}- and pilot -{ModState.PilotOverride}- should be the same.");
                            }));

                            goto RenderNow;
                        }
                    }

                    if (beacons.Count > 2)
                    {
                        var beacon = beacons[1];
                        var id = beacon.Def.ComponentTags.FirstOrDefault((string x) =>
                            x.StartsWith("mechdef_") || x.StartsWith("vehicledef_") ||
                            x.StartsWith("turretdef_"));
                        var pilotID = beacon.Def.ComponentTags.FirstOrDefault(x => x.StartsWith("StratOpsPilot_"))
                            ?.Remove(0, 14);
                        var waveString = beacon.Def.ComponentTags.FirstOrDefault(x => x.StartsWith("StrafeWaves_"));
                        int.TryParse(waveString?.Substring(11), out var waves);
                        ModInit.modLog?.Info?.Write(
                            $"beacon for button 3. will be {beacon.Def.Description.Name}, ID will be {id}, pilot will be {pilotID}");
                        var id1 = id;
                        var pilotID1 = pilotID;
                        var waves1 = waves;
                        var beacon1 = beacon;
                        popup.AddButton("3.", (Action) (() =>
                        {
                            ModState.StrafeWaves = waves1;
                            ModState.PopupActorResource = id1;
                            ModState.PilotOverride = pilotID1;
                            ModState.IsStrafeAOE = beacon1.IsAOEStrafe(
                                __instance.FromButton.Ability.Def.specialRules == AbilityDef.SpecialRules.Strafe);
                            ModInit.modLog?.Info?.Write(
                                $"Player pressed {id1} with pilot {pilotID1}. Now -{ModState.PopupActorResource}- and pilot -{ModState.PilotOverride}- should be the same.");
                        }));

                        beacon = beacons[0];
                        id = beacon.Def.ComponentTags.FirstOrDefault((string x) =>
                            x.StartsWith("mechdef_") || x.StartsWith("vehicledef_") ||
                            x.StartsWith("turretdef_"));
                        pilotID = beacon.Def.ComponentTags.FirstOrDefault(x => x.StartsWith("StratOpsPilot_"))
                            ?.Remove(0, 14);
                        waveString = beacon.Def.ComponentTags.FirstOrDefault(x => x.StartsWith("StrafeWaves_"));
                        int.TryParse(waveString?.Substring(11), out waves);
                        ModInit.modLog?.Info?.Write(
                            $"beacon for button 2. will be {beacon.Def.Description.Name}, ID will be {id}, pilot will be {pilotID}");
                        var id2 = id;
                        var pilotID2 = pilotID;
                        var waves2 = waves;
                        var beacon2 = beacon;
                        popup.AddButton("2.", (Action) (() =>
                        {
                            ModState.StrafeWaves = waves2;
                            ModState.PopupActorResource = id2;
                            ModState.PilotOverride = pilotID2;
                            ModState.IsStrafeAOE = beacon2.IsAOEStrafe(
                                __instance.FromButton.Ability.Def.specialRules == AbilityDef.SpecialRules.Strafe);
                            ModInit.modLog?.Info?.Write(
                                $"Player pressed {id2} with pilot {pilotID2}. Now -{ModState.PopupActorResource}- and pilot -{ModState.PilotOverride}- should be the same.");
                        }));

                        for (var index = 2; index < beacons.Count; index++)
                        {
                            beacon = beacons[index];
                            id = beacon.Def.ComponentTags.FirstOrDefault(x =>
                                x.StartsWith("mechdef_") || x.StartsWith("vehicledef_") ||
                                x.StartsWith("turretdef_"));
                            
                            pilotID = beacon.Def.ComponentTags.FirstOrDefault(x => x.StartsWith("StratOpsPilot_"))
                                ?.Remove(0, 14);
                            waveString = beacon.Def.ComponentTags.FirstOrDefault(x => x.StartsWith("StrafeWaves_"));
                            int.TryParse(waveString?.Substring(11), out waves);
                            ModInit.modLog?.Info?.Write(
                                $"beacon for button {index + 2}. will be {beacon.Def.Description.Name}, ID will be {id}, pilot will be {pilotID}");
                            var buttonName = $"{index + 2}.";
                            if (string.IsNullOrEmpty(id)) continue;
                            var id3 = id;
                            var pilotID3 = pilotID;
                            var waves3 = waves;
                            var beacon3 = beacon;
                            popup.AddButton(buttonName,
                                (Action) (() =>
                                {
                                    ModState.StrafeWaves = waves3;
                                    ModState.PopupActorResource = id3;
                                    ModState.PilotOverride = pilotID3;
                                    ModState.IsStrafeAOE = beacon3.IsAOEStrafe(
                                        __instance.FromButton.Ability.Def.specialRules == AbilityDef.SpecialRules.Strafe);
                                    ModInit.modLog?.Info?.Write(
                                        $"Player pressed {id3} with pilot {pilotID3}. Now -{ModState.PopupActorResource}- and pilot -{ModState.PilotOverride}- should be the same.");
                                }));
                            ModInit.modLog?.Info?.Write(
                                $"Added button for {buttonName}");
                        }
                    }

                    RenderNow:
                    popup.CancelOnEscape();
                    popup.Render();

                    var HUD = Traverse.Create(__instance).Property("HUD").GetValue<CombatHUD>();
                    var theActor = HUD.SelectedActor;
                    if (theActor == null) return true;
                    var distance = Mathf.RoundToInt(Vector3.Distance(theActor.CurrentPosition, worldPos));
                    var maxRange = Mathf.RoundToInt(__instance.FromButton.Ability.Def.IntParam2);
                    __result = true;
                    if ((__instance.FromButton.Ability.Def.specialRules == AbilityDef.SpecialRules.Strafe &&
                         distance > maxRange) ||
                        (__instance.FromButton.Ability.Def.specialRules ==
                         AbilityDef.SpecialRules.SpawnTurret &&
                         distance > maxRange))
                    {
                        __result = false;
                        return false;
                    }

                    return true;
                }

                return true;
            }

            public static void Postfix(SelectionStateCommandTargetTwoPoints __instance, Vector3 worldPos,
                int ___numPositionsLocked)
            {
                if (___numPositionsLocked == 2)
                {
                    var cHUD = Traverse.Create(__instance).Property("HUD").GetValue<CombatHUD>();
                    var creator = cHUD.SelectedActor;
                    ModState.cancelChanceForPlayerStrafe = 0f;
                    
                    var opforUnit = creator.FindMeAnOpforUnit();
                    if (opforUnit != null)
                    {
                        ModState.cancelChanceForPlayerStrafe = opforUnit.GetAvoidStrafeChanceForTeam();
                    }
                    var chanceDisplay = (float)Math.Round(1 - ModState.cancelChanceForPlayerStrafe, 2) * 100;
                    cHUD.AttackModeSelector.FireButton.FireText.SetText($"{chanceDisplay}% - Confirm", Array.Empty<object>());

                    ModInit.modLog?.Trace?.Write($"[SelectionStateCommandTargetTwoPoints.ProcessLeftClick] Creator {creator.DisplayName} initializing strafe vs target {opforUnit.team.DisplayName}. Calculated cancelChance {ModState.cancelChanceForPlayerStrafe}, display success chance: {chanceDisplay}.");
                }
            }
        }

        [HarmonyPatch(typeof(CombatHUDMechwarriorTray), "ResetAbilityButton",
            new Type[] { typeof(AbstractActor), typeof(CombatHUDActionButton), typeof(Ability), typeof(bool) })]
        public static class CombatHUDMechwarriorTray_ResetAbilityButton_Patch
        {
            public static void Postfix(CombatHUDMechwarriorTray __instance, AbstractActor actor, CombatHUDActionButton button, Ability ability, bool forceInactive)
            {
                if (UnityGameInstance.BattleTechGame.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                if (actor == null || ability == null) return;
                //                if (button == __instance.FireButton)
                //                {
                //                   ModInit.modLog?.Trace?.Write(
                //                       $"Leaving Fire Button Enabled");
                //                   return;
                //                }
                if (actor.HasActivatedThisRound || !actor.IsAvailableThisPhase ||
                    (actor.Combat.StackManager.IsAnyOrderActive && actor.Combat.TurnDirector.IsInterleaved))
                {
                    return;
                }
                if (ability.Def.Id != ModInit.modSettings.BattleArmorMountAndSwarmID && (actor.IsMountedUnit() && !actor.IsMountedInternal()) || actor.IsSwarmingUnit())
                {
                    button.DisableButton(); // maybe remove this for mounted units?
                }

                if (ability.Def.Id == ModInit.modSettings.BattleArmorDeSwarmRoll ||
                    ability.Def.Id == ModInit.modSettings.BattleArmorDeSwarmSwat)
                {
                    if (actor is Vehicle vehicle || actor.IsCustomUnitVehicle())
                    {
                        button.DisableButton();
                    }

                    if (!actor.HasSwarmingUnits())
                    {
                        button.DisableButton();
                    }
                }

                if (ability.Def.Id == ModInit.modSettings.AirliftAbilityID && ModInit.modSettings.CanDropOffAfterMoving)// && actor.MovingToPosition == null) // maybe need to check IsAnyOrderActive (but that might screw me)
                {
                    if (actor.HasAirliftedEnemy() || actor.HasAirliftedFriendly())
                    {
                        //button.DisableButton();
                        //if (!button.gameObject.activeSelf)
                        //{
                        //    button.gameObject.SetActive(true);
                        //}

                        //button.InitButton(CombatHUDMechwarriorTray.GetSelectionTypeFromTargeting(ability.Def.Targeting, false), ability, ability.Def.AbilityIcon, ability.Def.Description.Id, ability.Def.Description.Name, actor);
                        button.ResetButtonIfNotActive(actor);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CombatHUDWeaponPanel), "ResetAbilityButton",
            new Type[] { typeof(AbstractActor), typeof(CombatHUDActionButton), typeof(Ability), typeof(bool) })]
        public static class CombatHUDWeaponPanel_ResetAbilityButton_Patch
        {
            public static void Postfix(CombatHUDWeaponPanel __instance, AbstractActor actor, CombatHUDActionButton button, Ability ability, bool forceInactive)
            {
                if (UnityGameInstance.BattleTechGame.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                if (actor == null || ability == null) return;
                //                if (button == __instance.FireButton)
                //                {
                //                   ModInit.modLog?.Trace?.Write(
                //                       $"Leaving Fire Button Enabled");
                //                   return;
                //                }

                if (actor.HasActivatedThisRound || !actor.IsAvailableThisPhase ||
                    (actor.Combat.StackManager.IsAnyOrderActive && actor.Combat.TurnDirector.IsInterleaved))
                {
                    return;
                }

                if (ability.Def.Id != ModInit.modSettings.BattleArmorMountAndSwarmID && (actor.IsMountedUnit() && !actor.IsMountedInternal()) || actor.IsSwarmingUnit())
                {
                    button.DisableButton(); // maybe remove this for mounted units?
                }

                if (ability.Def.Id == ModInit.modSettings.BattleArmorDeSwarmRoll ||
                    ability.Def.Id == ModInit.modSettings.BattleArmorDeSwarmSwat)
                {
                    if (actor is Vehicle vehicle || actor.IsCustomUnitVehicle())
                    {
                        button.DisableButton();
                    }

                    if (!actor.HasSwarmingUnits())
                    {
                        button.DisableButton();
                    }
                }

                if (ability.Def.Id == ModInit.modSettings.AirliftAbilityID && ModInit.modSettings.CanDropOffAfterMoving)// && actor.MovingToPosition == null) // maybe need to check IsAnyOrderActive (but that might screw me)
                {
                    if (actor.HasAirliftedEnemy() || actor.HasAirliftedFriendly())
                    {
                        //button.DisableButton();
                        //if (!button.gameObject.activeSelf)
                        //{
                        //    button.gameObject.SetActive(true);
                        //}

                        //button.InitButton(CombatHUDMechwarriorTray.GetSelectionTypeFromTargeting(ability.Def.Targeting, false), ability, ability.Def.AbilityIcon, ability.Def.Description.Id, ability.Def.Description.Name, actor);
                        button.ResetButtonIfNotActive(actor);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CombatHUDEquipmentSlotEx), "ResetAbilityButton",
            new Type[] {typeof(AbstractActor), typeof(CombatHUDActionButton), typeof(Ability), typeof(bool)})]
        public static class CombatHUDEquipmentSlotEx_ResetAbilityButton
        {
            public static void Postfix(CombatHUDEquipmentSlotEx __instance, AbstractActor actor,
                CombatHUDActionButton button, Ability ability, bool forceInactive)
            {
                if (UnityGameInstance.BattleTechGame.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                if (actor == null || ability == null) return;
                //                if (button == __instance.FireButton)
                //                {
                //                   ModInit.modLog?.Trace?.Write(
                //                       $"Leaving Fire Button Enabled");
                //                   return;
                //                }

                if (actor.HasActivatedThisRound || !actor.IsAvailableThisPhase ||
                    (actor.Combat.StackManager.IsAnyOrderActive && actor.Combat.TurnDirector.IsInterleaved))
                {
                    return;
                }

                if (ability.Def.Id != ModInit.modSettings.BattleArmorMountAndSwarmID && (actor.IsMountedUnit() && !actor.IsMountedInternal()) || actor.IsSwarmingUnit())
                {
                    button.DisableButton(); // maybe remove this for mounted units?
                }

                if (ability.Def.Id == ModInit.modSettings.BattleArmorDeSwarmRoll ||
                    ability.Def.Id == ModInit.modSettings.BattleArmorDeSwarmSwat)
                {
                    if (actor is Vehicle vehicle || actor.IsCustomUnitVehicle())
                    {
                        button.DisableButton();
                    }

                    if (!actor.HasSwarmingUnits())
                    {
                        button.DisableButton();
                    }
                }

                if (ability.Def.Id == ModInit.modSettings.AirliftAbilityID && ModInit.modSettings.CanDropOffAfterMoving)// && actor.MovingToPosition == null) // maybe need to check IsAnyOrderActive (but that might screw me)
                {
                    if (actor.HasAirliftedEnemy() || actor.HasAirliftedFriendly())
                    {
                        //button.DisableButton();
                        //if (!button.gameObject.activeSelf)
                        //{
                        //    button.gameObject.SetActive(true);
                        //}

                        //button.InitButton(CombatHUDMechwarriorTray.GetSelectionTypeFromTargeting(ability.Def.Targeting, false), ability, ability.Def.AbilityIcon, ability.Def.Description.Id, ability.Def.Description.Name, actor);
                        button.ResetButtonIfNotActive(actor);
                    }
                }
            }
        }


        [HarmonyPatch(typeof(CameraControl), "ShowActorCam")]
        public static class CameraControl_ShowActorCam
        {
            public static bool Prefix(CameraControl __instance, AbstractActor actor, Quaternion rotation,
                float duration, ref AttachToActorCameraSequence __result)
            {
                var combat = UnityGameInstance.BattleTechGame.Combat;
                if (combat.ActiveContract.ContractTypeValue.IsSkirmish) return true;
                Vector3 offset = new Vector3(0f, 50f, 50f);
                __result = new AttachToActorCameraSequence(combat, actor.GameRep.transform, offset, rotation, duration,
                    true, false);
                return false;
            }
        }

        [HarmonyPatch(typeof(CombatSpawningReticle), "ShowReticle")]
        public static class CombatSpawningReticle_ShowReticle
        {
            public static void Postfix(CombatSpawningReticle __instance)
            {
                if (!string.IsNullOrEmpty(ModInit.modSettings.customSpawnReticleAsset))
                {
                    var childComponents = __instance.gameObject.GetComponentsInChildren<Transform>(true);

                    for (int i = 0; i < childComponents.Length; i++)
                    {
                        if (childComponents[i].name == "ReticleDecalCircle")
                        {
                            var decalFromCirle = childComponents[i].GetComponent<BTUIDecal>();
                            var dm = UnityGameInstance.BattleTechGame.DataManager;
                            var newTexture = dm.GetObjectOfType<Texture2D>(ModInit.modSettings.customSpawnReticleAsset,
                                BattleTechResourceType.Texture2D);
                            decalFromCirle.DecalMaterial.mainTexture = newTexture;
                        }
                    }
                    //var circle1 = GameObject.Find("ReticleDecalCircle");
                }

                var decals = __instance.gameObject.GetComponentsInChildren<BTUIDecal>();

                foreach (var decal in decals)
                {
                    if (ModInit.modSettings.customSpawnReticleColor != null)
                    {
                        var customColor = new Color(ModInit.modSettings.customSpawnReticleColor.Rf,
                            ModInit.modSettings.customSpawnReticleColor.Gf,
                            ModInit.modSettings.customSpawnReticleColor.Bf);
                        decal.DecalMaterial.color = customColor;
                    }
                    else
                    {
                        decal.DecalMaterial.color = Color.green;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CombatAuraReticle), "RefreshActiveProbeRange")]
        public static class CombatAuraReticle_RefreshActiveProbeRange
        {
            static bool Prepare() => false; //disabled
            public static void Postfix(CombatAuraReticle __instance, bool showActiveProbe, AbstractActor ___owner, ref float ___currentAPRange)
            {
                if (!showActiveProbe || __instance.AuraBubble() != null) return;

                float num = 0f;
                if (___owner.ComponentAbilities.Count > 0)
                {
                    for (int i = 0; i < ___owner.ComponentAbilities.Count; i++)
                    {
                        if (___owner.ComponentAbilities[i].Def.Targeting == AbilityDef.TargetingType.ActiveProbe)
                        {
                            num = ___owner.ComponentAbilities[i].Def.FloatParam1;
                            break;
                        }
                    }
                }
                if (!Mathf.Approximately(num, ___currentAPRange))
                {
                    var apObject = Traverse.Create(__instance).Property("activeProbeRangeScaledObject")
                        .GetValue<GameObject>();
                    apObject.transform.localScale = new Vector3(num * 2f, 1f, num * 2f);
                }
                ___currentAPRange = num;

            }
        }
    }
}