using System;
using System.Collections.Generic;
using System.Linq;
using Abilifier.Patches;
using BattleTech;
using BattleTech.Data;
using BattleTech.Rendering;
using BattleTech.UI;
using Harmony;
using HBS;
using StrategicOperations.Framework;
using UnityEngine;

namespace StrategicOperations.Patches
{
    public class StrategicOperationsPatches
    {
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
                if (UnityGameInstance.BattleTechGame.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                var dm = __instance.Combat.DataManager;
                LoadRequest loadRequest = dm.CreateLoadRequest();

                loadRequest.AddBlindLoadRequest(BattleTechResourceType.PilotDef, "pilot_sim_starter_dekker");
                ModInit.modLog.LogMessage($"Added loadrequest for PilotDef: pilot_sim_starter_dekker (hardcoded)");

                loadRequest.AddBlindLoadRequest(BattleTechResourceType.Texture2D, "select-256");
                loadRequest.AddBlindLoadRequest(BattleTechResourceType.Texture2D, "select-512");
                ModInit.modLog.LogMessage($"Added loadrequest for Texture2D: uixTxrFram_circleSmallOutline2 (hardcoded)");


                foreach (var abilityDef in dm.AbilityDefs.Where(x => x.Key.StartsWith("AbilityDefCMD_")))
                {
                    var ability = new Ability(abilityDef.Value);
                    if (string.IsNullOrEmpty(ability.Def?.ActorResource)) continue;
                    if (!string.IsNullOrEmpty(ability.Def.getAbilityDefExtension().CMDPilotOverride))
                    {
                        var pilotID = ability.Def.getAbilityDefExtension().CMDPilotOverride;
                        ModInit.modLog.LogMessage($"Added loadrequest for PilotDef: {pilotID}");
                        loadRequest.AddBlindLoadRequest(BattleTechResourceType.PilotDef, pilotID);
                    }

                    if (ability.Def.ActorResource.StartsWith("mechdef_"))
                    {
                        ModInit.modLog.LogMessage($"Added loadrequest for MechDef: {ability.Def.ActorResource}");
                        loadRequest.AddBlindLoadRequest(BattleTechResourceType.MechDef, ability.Def.ActorResource);
                    }

                    if (ability.Def.ActorResource.StartsWith("vehicledef_"))
                    {
                        ModInit.modLog.LogMessage($"Added loadrequest for VehicleDef: {ability.Def.ActorResource}");
                        loadRequest.AddBlindLoadRequest(BattleTechResourceType.VehicleDef, ability.Def.ActorResource);
                    }

                    if (ability.Def.ActorResource.StartsWith("turretdef_"))
                    {
                        ModInit.modLog.LogMessage($"Added loadrequest for TurretDef: {ability.Def.ActorResource}");
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
                        ModInit.modLog.LogMessage($"Added loadrequest for PilotDef: {pilotID}");
                        loadRequest.AddBlindLoadRequest(BattleTechResourceType.PilotDef, pilotID);
                    }

                    var id = beacon.Def.ComponentTags.FirstOrDefault(x =>
                        x.StartsWith("mechdef_") || x.StartsWith("vehicledef_") ||
                        x.StartsWith("turretdef_"));
                    if (string.IsNullOrEmpty(id)) continue;

                    if (id.StartsWith("mechdef_"))
                    {
                        ModInit.modLog.LogMessage($"Added loadrequest for MechDef: {id}");
                        loadRequest.AddBlindLoadRequest(BattleTechResourceType.MechDef, id);
                    }
                    else if (id.StartsWith("vehicledef_"))
                    {
                        ModInit.modLog.LogMessage($"Added loadrequest for VehicleDef: {id}");
                        loadRequest.AddBlindLoadRequest(BattleTechResourceType.VehicleDef, id);
                    }
                    else if (id.StartsWith("turretdef_"))
                    {
                        ModInit.modLog.LogMessage($"Added loadrequest for TurretDef: {id}");
                        loadRequest.AddBlindLoadRequest(BattleTechResourceType.TurretDef, id);
                    }

                }

                loadRequest.ProcessRequests(1000u);
            }
        }

        [HarmonyPatch(typeof(GameRepresentation), "Update")]
        public static class GameRepresentation_Update
        {
            public static bool Prefix(GameRepresentation __instance, AbstractActor ____parentActor)
            {
                var combat = UnityGameInstance.BattleTechGame.Combat;
                if (combat == null) return true;
                if (combat.ActiveContract.ContractTypeValue.IsSkirmish) return true;
                var registry = combat.ItemRegistry;

                if (____parentActor?.spawnerGUID == null)
                {
                    //ModInit.modLog.LogMessage($"Couldn't find UnitSpawnPointGameLogic for {____parentActor?.DisplayName}. Should be CMD Ability actor; skipping safety teleport!");
                    return false;
                }

                return registry.GetItemByGUID<UnitSpawnPointGameLogic>(____parentActor?.spawnerGUID) != null;
                //ModInit.modLog.LogMessage($"Couldn't find UnitSpawnPointGameLogic for {____parentActor?.DisplayName}. Should be CMD Ability actor; skipping safety teleport!");
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
                    ModInit.modLog.LogMessage(
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
                            ModInit.modLog.LogMessage(
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

                ModInit.modLog.LogMessage(
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
            new Type[] {typeof(AbstractActor), typeof(Vector3), typeof(Vector3)})]
        public static class Ability_Activate
        {
            public static bool Prefix(Ability __instance, AbstractActor creator, Vector3 positionA, Vector3 positionB)
            {
                if (__instance.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return true;
                ModInit.modLog.LogMessage($"Running Ability.Activate");
                if (!__instance.IsAvailable)
                {
                    ModInit.modLog.LogMessage(
                        $"Ability {__instance.Def.Description.Name} was unavailable, continuing to vanilla handling.");
                    return true;
                }

                AbilityDef.SpecialRules specialRules = __instance.Def.specialRules;
                if (specialRules == AbilityDef.SpecialRules.Strafe)
                {
                    Utils._activateStrafeMethod.Invoke(__instance,
                        new object[] {creator.team, positionA, positionB, __instance.Def.FloatParam1});
                    ModInit.modLog.LogMessage($"ActivateStrafe invoked from Ability.Activate");
                    __instance.Combat.MessageCenter.PublishMessage(new AbilityActivatedMessage(creator.GUID,
                        creator.GUID, __instance.Def.Id, positionA, positionB));
                    __instance.ActivateCooldown();
                    return false;
                }

                else if (specialRules == AbilityDef.SpecialRules.SpawnTurret)
                {
                    Utils._activateSpawnTurretMethod.Invoke(__instance,
                        new object[] {creator.team, positionA, positionB});
                    ModInit.modLog.LogMessage($"ActivateSpawnTurret invoked from Ability.Activate");
                    __instance.Combat.MessageCenter.PublishMessage(new AbilityActivatedMessage(creator.GUID,
                        creator.GUID, __instance.Def.Id, positionA, positionB));
                    __instance.ActivateCooldown();
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Ability), "ActivateStrafe")]
        public static class Ability_ActivateStrafe
        {
            public static bool Prefix(Ability __instance, Team team, Vector3 positionA, Vector3 positionB, float radius)
            {
                if (__instance.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return true;
                ModInit.modLog.LogMessage($"Running Ability.ActivateStrafe");
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

                if (__instance.Combat.Teams.All(x => x.GUID != "61612bb3-abf9-4586-952a-0559fa9dcd75"))
                {
                    Utils.CreateOrUpdateNeutralTeam();
                }

                var neutralTeam =
                    __instance.Combat.Teams.FirstOrDefault(
                        x => x.GUID == "61612bb3-abf9-4586-952a-0559fa9dcd75");

                ModInit.modLog.LogMessage($"Team neturalTeam = {neutralTeam?.DisplayName}");
                var cmdLance = Utils.CreateCMDLance(neutralTeam);
                var actorResource = __instance.Def.ActorResource;
                if (!string.IsNullOrEmpty(__instance.Def?.ActorResource))
                {
                    if (!string.IsNullOrEmpty(ModState.popupActorResource))
                    {
                        actorResource = ModState.popupActorResource;
                        ModState.popupActorResource = "";
                    }

                    ModInit.modLog.LogMessage($"Pilot should be {pilotID}");
                    if (ModState.deploymentAssetsStats.Any(x => x.ID == actorResource))
                    {
                        var assetStatInfo = ModState.deploymentAssetsStats.FirstOrDefault(x => x.ID == actorResource);
                        if (assetStatInfo != null)
                        {
                            assetStatInfo.contractUses -= 1;
                            if (assetStatInfo.consumeOnUse)
                            {
                                sim?.CompanyStats.ModifyStat("StratOps", -1, assetStatInfo.stat,
                                    StatCollection.StatOperation.Int_Subtract, 1);
                            }
                        }

                        ModInit.modLog.LogMessage($"Decrementing count of {actorResource} in deploymentAssetsDict");
                    }

                    if (actorResource.StartsWith("mechdef_"))
                    {
                        dm.MechDefs.TryGet(actorResource, out var supportActorMechDef);
                        supportActorMechDef.Refresh();
                        var supportActorMech = ActorFactory.CreateMech(supportActorMechDef,
                            supportPilotDef, neutralTeam.EncounterTags, neutralTeam.Combat,
                            neutralTeam.GetNextSupportUnitGuid(), "", supportHeraldryDef);
                        supportActorMech.Init(neutralTeam.OffScreenPosition, 0f, false);
                        supportActorMech.InitGameRep(null);
                        neutralTeam.AddUnit(supportActorMech);
                        supportActorMech.AddToTeam(neutralTeam);
                        supportActorMech.AddToLance(cmdLance);
                        supportActorMech.GameRep.gameObject.SetActive(true);
                        supportActorMech.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(
                            __instance.Combat.BattleTechGame, supportActorMech,
                            BehaviorTreeIDEnum.DoNothingTree);
                        ModInit.modLog.LogMessage($"Initializing Strafing Run!");

                        TB_StrafeSequence eventSequence =
                            new TB_StrafeSequence(supportActorMech, positionA, positionB, radius, team);
                        TurnEvent tEvent = new TurnEvent(Guid.NewGuid().ToString(), __instance.Combat,
                            __instance.Def.ActivationETA, null, eventSequence, __instance.Def, false);
                        __instance.Combat.TurnDirector.AddTurnEvent(tEvent);
                        if (__instance.Def.IntParam1 > 0)
                        {
                            var flares = Traverse.Create(__instance).Method("SpawnFlares", new object[]
                            {
                                positionA, positionB, __instance.Def.StringParam1,
                                __instance.Def.IntParam1, __instance.Def.ActivationETA
                            });
                            flares.GetValue();
                        }

                        if (ModInit.modSettings.commandUseCostsMulti > 0 ||
                            __instance.Def.getAbilityDefExtension().CBillCost > 0)
                        {
                            var unitName = "";
                            var unitCost = 0;
                            var unitID = "";
                            unitName = supportActorMechDef.Description.UIName;
                            unitID = supportActorMechDef.Description.Id;
                            unitCost = supportActorMechDef.Chassis.Description.Cost;
                            ModInit.modLog.LogMessage($"Usage cost will be {unitCost}");

                            if (ModState.CommandUses.All(x => x.UnitID != actorResource))
                            {

                                var commandUse = new Utils.CmdUseInfo(unitID, __instance.Def.Description.Name, unitName,
                                    unitCost, __instance.Def.getAbilityDefExtension().CBillCost);

                                ModState.CommandUses.Add(commandUse);
                                ModInit.modLog.LogMessage(
                                    $"Added usage cost for {commandUse.CommandName} - {commandUse.UnitName}");
                            }
                            else
                            {
                                var cmdUse = ModState.CommandUses.FirstOrDefault(x => x.UnitID == actorResource);
                                if (cmdUse == null)
                                {
                                    ModInit.modLog.LogMessage($"ERROR: cmdUseInfo was null");
                                }
                                else
                                {
                                    cmdUse.UseCount += 1;
                                    ModInit.modLog.LogMessage(
                                        $"Added usage cost for {cmdUse.CommandName} - {cmdUse.UnitName}, used {cmdUse.UseCount} times");
                                }
                            }
                        }
                    }

                    else if (actorResource.StartsWith("vehicledef_"))
                    {
                        dm.VehicleDefs.TryGet(actorResource, out var supportActorVehicleDef);
                        supportActorVehicleDef.Refresh();
                        var supportActorVehicle = ActorFactory.CreateVehicle(supportActorVehicleDef,
                            supportPilotDef, neutralTeam.EncounterTags, neutralTeam.Combat,
                            neutralTeam.GetNextSupportUnitGuid(), "", supportHeraldryDef);
                        supportActorVehicle.Init(neutralTeam.OffScreenPosition, 0f, false);
                        supportActorVehicle.InitGameRep(null);
                        neutralTeam.AddUnit(supportActorVehicle);
                        supportActorVehicle.AddToTeam(neutralTeam);
                        supportActorVehicle.AddToLance(cmdLance);
                        supportActorVehicle.GameRep.gameObject.SetActive(true);
                        supportActorVehicle.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(
                            __instance.Combat.BattleTechGame, supportActorVehicle,
                            BehaviorTreeIDEnum.DoNothingTree);

                        ModInit.modLog.LogMessage($"Initializing Strafing Run!");

                        TB_StrafeSequence eventSequence =
                            new TB_StrafeSequence(supportActorVehicle, positionA, positionB, radius, team);
                        TurnEvent tEvent = new TurnEvent(Guid.NewGuid().ToString(), __instance.Combat,
                            __instance.Def.ActivationETA, null, eventSequence, __instance.Def, false);
                        __instance.Combat.TurnDirector.AddTurnEvent(tEvent);
                        if (__instance.Def.IntParam1 > 0)
                        {
                            var flares = Traverse.Create(__instance).Method("SpawnFlares", new object[]
                            {
                                positionA, positionB, __instance.Def.StringParam1,
                                __instance.Def.IntParam1, __instance.Def.ActivationETA
                            });
                            flares.GetValue();
                        }

                        if (ModInit.modSettings.commandUseCostsMulti > 0 ||
                            __instance.Def.getAbilityDefExtension().CBillCost > 0)
                        {
                            var unitName = "";
                            var unitCost = 0;
                            var unitID = "";

                            unitName = supportActorVehicleDef.Description.UIName;
                            unitID = supportActorVehicleDef.Description.Id;
                            unitCost = supportActorVehicleDef.Chassis.Description.Cost;
                            ModInit.modLog.LogMessage(
                                $"Usage cost will be {unitCost} + {__instance.Def.getAbilityDefExtension().CBillCost}");


                            if (ModState.CommandUses.All(x => x.UnitID != actorResource))
                            {

                                var commandUse = new Utils.CmdUseInfo(unitID, __instance.Def.Description.Name, unitName,
                                    unitCost, __instance.Def.getAbilityDefExtension().CBillCost);

                                ModState.CommandUses.Add(commandUse);
                                ModInit.modLog.LogMessage(
                                    $"Added usage cost for {commandUse.CommandName} - {commandUse.UnitName}");
                            }
                            else
                            {
                                var cmdUse = ModState.CommandUses.FirstOrDefault(x => x.UnitID == actorResource);
                                if (cmdUse == null)
                                {
                                    ModInit.modLog.LogMessage($"ERROR: cmdUseInfo was null");
                                }
                                else
                                {
                                    cmdUse.UseCount += 1;
                                    ModInit.modLog.LogMessage(
                                        $"Added usage cost for {cmdUse.CommandName} - {cmdUse.UnitName}, used {cmdUse.UseCount} times");
                                }
                            }
                        }
                    }
                    else if (actorResource.StartsWith("turretdef_"))
                    {
                        dm.TurretDefs.TryGet(actorResource, out var supportActorTurretDef);
                        supportActorTurretDef.Refresh();
                        var supportActorTurret = ActorFactory.CreateTurret(supportActorTurretDef,
                            supportPilotDef, neutralTeam.EncounterTags, neutralTeam.Combat,
                            neutralTeam.GetNextSupportUnitGuid(), "", supportHeraldryDef);
                        supportActorTurret.Init(neutralTeam.OffScreenPosition, 0f, false);
                        supportActorTurret.InitGameRep(null);
                        neutralTeam.AddUnit(supportActorTurret);
                        supportActorTurret.AddToTeam(neutralTeam);
                        supportActorTurret.AddToLance(cmdLance);
                        supportActorTurret.GameRep.gameObject.SetActive(true);
                        supportActorTurret.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(
                            __instance.Combat.BattleTechGame, supportActorTurret,
                            BehaviorTreeIDEnum.DoNothingTree);

                        ModInit.modLog.LogMessage($"Initializing Strafing Run!");

                        TB_StrafeSequence eventSequence =
                            new TB_StrafeSequence(supportActorTurret, positionA, positionB, radius, team);
                        TurnEvent tEvent = new TurnEvent(Guid.NewGuid().ToString(), __instance.Combat,
                            __instance.Def.ActivationETA, null, eventSequence, __instance.Def, false);
                        __instance.Combat.TurnDirector.AddTurnEvent(tEvent);
                        if (__instance.Def.IntParam1 > 0)
                        {
                            var flares = Traverse.Create(__instance).Method("SpawnFlares", new object[]
                            {
                                positionA, positionB, __instance.Def.StringParam1,
                                __instance.Def.IntParam1, __instance.Def.ActivationETA
                            });
                            flares.GetValue();
                        }

                        if (ModInit.modSettings.commandUseCostsMulti > 0 ||
                            __instance.Def.getAbilityDefExtension().CBillCost > 0)
                        {
                            var unitName = "";
                            var unitCost = 0;
                            var unitID = "";

                            unitName = supportActorTurretDef.Description.UIName;
                            unitID = supportActorTurretDef.Description.Id;
                            unitCost = supportActorTurretDef.Chassis.Description.Cost;


                            if (ModState.CommandUses.All(x => x.UnitID != actorResource))
                            {

                                var commandUse = new Utils.CmdUseInfo(unitID, __instance.Def.Description.Name, unitName,
                                    unitCost, __instance.Def.getAbilityDefExtension().CBillCost);

                                ModState.CommandUses.Add(commandUse);
                                ModInit.modLog.LogMessage(
                                    $"Added usage cost for {commandUse.CommandName} - {commandUse.UnitName}. UnitUseCost (unadjusted): {unitCost}. Ability Use Cost: {__instance.Def.getAbilityDefExtension().CBillCost}");
                            }
                            else
                            {
                                var cmdUse = ModState.CommandUses.FirstOrDefault(x => x.UnitID == actorResource);
                                if (cmdUse == null)
                                {
                                    ModInit.modLog.LogMessage($"ERROR: cmdUseInfo was null");
                                }
                                else
                                {
                                    cmdUse.UseCount += 1;
                                    ModInit.modLog.LogMessage(
                                        $"Added usage cost for {cmdUse.CommandName} - {cmdUse.UnitName}. UnitUseCost (unadjusted): {unitCost}. Ability Use Cost: {__instance.Def.getAbilityDefExtension().CBillCost}. Now used {cmdUse.UseCount} times");
                                }
                            }
                        }
                    }
                    else
                    {
                        ModInit.modLog.LogMessage(
                            $"Something wrong with CMD Ability {__instance.Def.Id}, invalid ActorResource");
                        ModState.selectedAIVectors = new List<Vector3>();
                        return false;
                    }

                    ModState.selectedAIVectors = new List<Vector3>();
                    return false;
                }

                ModState.selectedAIVectors = new List<Vector3>();
                return false;
            }
        }

        [HarmonyPatch(typeof(Ability), "ActivateSpawnTurret")]
        public static class Ability_ActivateSpawnTurret
        {
            public static bool Prefix(Ability __instance, Team team, Vector3 positionA, Vector3 positionB)
            {
                if (__instance.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return true;
                ModInit.modLog.LogMessage($"Running Ability.ActivateSpawnTurret");
                var combat = UnityGameInstance.BattleTechGame.Combat;
                var dm = combat.DataManager;
                var sim = UnityGameInstance.BattleTechGame.Simulation;

                var actorResource = __instance.Def.ActorResource;

                var supportHeraldryDef = Utils.SwapHeraldryColors(team.HeraldryDef, dm);


                if (!string.IsNullOrEmpty(ModState.popupActorResource))
                {
                    actorResource = ModState.popupActorResource;
                    ModState.popupActorResource = "";
                }

                if (ModState.deploymentAssetsStats.Any(x => x.ID == actorResource))
                {
                    var assetStatInfo = ModState.deploymentAssetsStats.FirstOrDefault(x => x.ID == actorResource);
                    if (assetStatInfo != null)
                    {
                        assetStatInfo.contractUses -= 1;
                        if (assetStatInfo.consumeOnUse)
                        {
                            sim?.CompanyStats.ModifyStat("StratOps", -1, assetStatInfo.stat,
                                StatCollection.StatOperation.Int_Subtract, 1);
                        }
                    }

                    ModInit.modLog.LogMessage($"Decrementing count of {actorResource} in deploymentAssetsDict");
                }

                var instanceGUID =
                    $"{__instance.Def.Id}_{team.Name}_{actorResource}_{positionA}_{positionB}@{actorResource}";

                if (ModState.deferredInvokeSpawns.All(x => x.Key != instanceGUID) && !ModState.FromDelegate)
                {
                    ModInit.modLog.LogMessage(
                        $"Deferred Spawner = null, creating delegate and returning false. Delegate should spawn {actorResource}");

                    void DeferredInvokeSpawn() =>
                        Utils._activateSpawnTurretMethod.Invoke(__instance, new object[] {team, positionA, positionB});

                    var kvp = new KeyValuePair<string, Action>(instanceGUID, DeferredInvokeSpawn);
                    ModState.deferredInvokeSpawns.Add(kvp);
                    var flares = Traverse.Create(__instance).Method("SpawnFlares",
                        new object[] {positionA, positionA, __instance.Def.StringParam1, 1, 1});
                    flares.GetValue();
                    return false;
                }

                if (!string.IsNullOrEmpty(ModState.deferredActorResource))
                {
                    actorResource = ModState.deferredActorResource;
                    ModInit.modLog.LogMessage($"{actorResource} restored from deferredActorResource");
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

                ModInit.modLog.LogMessage($"Pilot should be {pilotID}");
                dm.PilotDefs.TryGet(pilotID, out var supportPilotDef);
                var cmdLance = Utils.CreateCMDLance(team.SupportTeam);

                Quaternion quaternion = Quaternion.LookRotation(positionB - positionA);

                if (actorResource.StartsWith("mechdef_"))
                {
                    ModInit.modLog.LogMessage($"Attempting to spawn {actorResource} as mech.");
                    dm.MechDefs.TryGet(actorResource, out var supportActorMechDef);
                    supportActorMechDef.Refresh();
                    var supportActorMech = ActorFactory.CreateMech(supportActorMechDef, supportPilotDef,
                        team.SupportTeam.EncounterTags, team.SupportTeam.Combat,
                        team.SupportTeam.GetNextSupportUnitGuid(), "", supportHeraldryDef);
                    supportActorMech.Init(positionA, quaternion.eulerAngles.y, false);
                    supportActorMech.InitGameRep(null);

                    team.SupportTeam.AddUnit(supportActorMech);
                    supportActorMech.AddToTeam(team.SupportTeam);

                    supportActorMech.AddToLance(cmdLance);
                    supportActorMech.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(
                        __instance.Combat.BattleTechGame, supportActorMech, BehaviorTreeIDEnum.CoreAITree);
                    //supportActorMech.GameRep.gameObject.SetActive(true);

                    supportActorMech.OnPositionUpdate(positionA, quaternion, -1, true, null, false);
                    supportActorMech.DynamicUnitRole = UnitRole.Brawler;

                    UnitSpawnedMessage message = new UnitSpawnedMessage("FROM_ABILITY", supportActorMech.GUID);

                    __instance.Combat.MessageCenter.PublishMessage(message);
                    supportActorMech.OnPlayerVisibilityChanged(VisibilityLevel.LOSFull);

                    Utils.DeployEvasion(supportActorMech);

                    ModInit.modLog.LogMessage($"Added {supportActorMech?.MechDef?.Description?.Id} to SupportUnits");

                    if (ModInit.modSettings.commandUseCostsMulti > 0 ||
                        __instance.Def.getAbilityDefExtension().CBillCost > 0)
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
                                new Utils.CmdUseInfo(unitID, __instance.Def.Description.Name, unitName, unitCost,
                                    __instance.Def.getAbilityDefExtension().CBillCost);

                            ModState.CommandUses.Add(commandUse);
                            ModInit.modLog.LogMessage(
                                $"Added usage cost for {commandUse.CommandName} - {commandUse.UnitName}. UnitUseCost (unadjusted): {unitCost}. Ability Use Cost: {__instance.Def.getAbilityDefExtension().CBillCost}");
                        }
                        else
                        {
                            var cmdUse = ModState.CommandUses.FirstOrDefault(x => x.UnitID == actorResource);
                            if (cmdUse == null)
                            {
                                ModInit.modLog.LogMessage($"ERROR: cmdUseInfo was null");
                            }
                            else
                            {
                                cmdUse.UseCount += 1;
                                ModInit.modLog.LogMessage(
                                    $"Added usage cost for {cmdUse.CommandName} - {cmdUse.UnitName}. UnitUseCost (unadjusted): {unitCost}. Ability Use Cost: {__instance.Def.getAbilityDefExtension().CBillCost}. Now used {cmdUse.UseCount} times.");
                            }
                        }
                    }


                }
                else if (actorResource.StartsWith("vehicledef_"))
                {
                    ModInit.modLog.LogMessage($"Attempting to spawn {actorResource} as vehicle.");
                    dm.VehicleDefs.TryGet(actorResource, out var supportActorVehicleDef);
                    supportActorVehicleDef.Refresh();
                    var supportActorVehicle = ActorFactory.CreateVehicle(supportActorVehicleDef, supportPilotDef,
                        team.SupportTeam.EncounterTags, team.SupportTeam.Combat,
                        team.SupportTeam.GetNextSupportUnitGuid(), "", supportHeraldryDef);
                    supportActorVehicle.Init(positionA, quaternion.eulerAngles.y, false);
                    supportActorVehicle.InitGameRep(null);

                    team.SupportTeam.AddUnit(supportActorVehicle);
                    supportActorVehicle.AddToTeam(team.SupportTeam);

                    supportActorVehicle.AddToLance(cmdLance);
                    supportActorVehicle.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(
                        __instance.Combat.BattleTechGame, supportActorVehicle, BehaviorTreeIDEnum.CoreAITree);
                    //supportActorVehicle.GameRep.gameObject.SetActive(true);

                    supportActorVehicle.OnPositionUpdate(positionA, quaternion, -1, true, null, false);
                    supportActorVehicle.DynamicUnitRole = UnitRole.Vehicle;

                    UnitSpawnedMessage message = new UnitSpawnedMessage("FROM_ABILITY", supportActorVehicle.GUID);

                    __instance.Combat.MessageCenter.PublishMessage(message);
                    supportActorVehicle.OnPlayerVisibilityChanged(VisibilityLevel.LOSFull);

                    Utils.DeployEvasion(supportActorVehicle);

                    ModInit.modLog.LogMessage(
                        $"Added {supportActorVehicle?.VehicleDef?.Description?.Id} to SupportUnits");


                    if (ModInit.modSettings.commandUseCostsMulti > 0 ||
                        __instance.Def.getAbilityDefExtension().CBillCost > 0)
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
                                new Utils.CmdUseInfo(unitID, __instance.Def.Description.Name, unitName, unitCost,
                                    __instance.Def.getAbilityDefExtension().CBillCost);

                            ModState.CommandUses.Add(commandUse);
                            ModInit.modLog.LogMessage(
                                $"Added usage cost for {commandUse.CommandName} - {commandUse.UnitName}. UnitUseCost (unadjusted): {unitCost}. Ability Use Cost: {__instance.Def.getAbilityDefExtension().CBillCost}");
                        }
                        else
                        {
                            var cmdUse = ModState.CommandUses.FirstOrDefault(x => x.UnitID == actorResource);
                            if (cmdUse == null)
                            {
                                ModInit.modLog.LogMessage($"ERROR: cmdUseInfo was null");
                            }
                            else
                            {
                                cmdUse.UseCount += 1;
                                ModInit.modLog.LogMessage(
                                    $"Added usage cost for {cmdUse.CommandName} - {cmdUse.UnitName}. UnitUseCost (unadjusted): {unitCost}. Ability Use Cost: {__instance.Def.getAbilityDefExtension().CBillCost}. Now used {cmdUse.UseCount} times.");
                            }
                        }
                    }
                }

                else
                {
                    ModInit.modLog.LogMessage($"Attempting to spawn {actorResource} as turret.");
                    var spawnTurretMethod = Traverse.Create(__instance).Method("SpawnTurret",
                        new object[] {team.SupportTeam, actorResource, positionA, quaternion});
                    var turretActor = spawnTurretMethod.GetValue<AbstractActor>();

                    team.SupportTeam.AddUnit(turretActor);
                    turretActor.AddToTeam(team.SupportTeam);

                    turretActor.AddToLance(cmdLance);
                    turretActor.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(__instance.Combat.BattleTechGame,
                        turretActor, BehaviorTreeIDEnum.CoreAITree);

                    turretActor.OnPositionUpdate(positionA, quaternion, -1, true, null, false);
                    turretActor.DynamicUnitRole = UnitRole.Turret;

                    UnitSpawnedMessage message = new UnitSpawnedMessage("FROM_ABILITY", turretActor.GUID);

                    __instance.Combat.MessageCenter.PublishMessage(message);
                    turretActor.OnPlayerVisibilityChanged(VisibilityLevel.LOSFull);


                    if (ModInit.modSettings.commandUseCostsMulti > 0 ||
                        __instance.Def.getAbilityDefExtension().CBillCost > 0)
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
                                new Utils.CmdUseInfo(unitID, __instance.Def.Description.Name, unitName, unitCost,
                                    __instance.Def.getAbilityDefExtension().CBillCost);

                            ModState.CommandUses.Add(commandUse);
                            ModInit.modLog.LogMessage(
                                $"Added usage cost for {commandUse.CommandName} - {commandUse.UnitName}. UnitUseCost (unadjusted): {unitCost}. Ability Use Cost: {__instance.Def.getAbilityDefExtension().CBillCost}");
                        }
                        else
                        {
                            var cmdUse = ModState.CommandUses.FirstOrDefault(x => x.UnitID == actorResource);
                            if (cmdUse == null)
                            {
                                ModInit.modLog.LogMessage($"ERROR: cmdUseInfo was null");
                            }
                            else
                            {
                                cmdUse.UseCount += 1;
                                ModInit.modLog.LogMessage(
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
            }
        }

        [HarmonyPatch(typeof(TurnActor), "OnRoundBegin")]
        public static class TurnActor_OnRoundBegin
        {
            public static void Postfix(TurnActor __instance)
            {
                if (__instance.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                foreach (var ability in ModState.CommandAbilities)
                {
                    ability.OnNewRound();
                }

                var team = __instance as Team;

                if (team?.units != null)
                    foreach (var unit in team?.units)
                    {
                        var rep = unit.GameRep as PilotableActorRepresentation;
                        rep.ClearForcedPlayerVisibilityLevel(__instance.Combat.GetAllLivingCombatants());
                    }

                //                team?.ResetUnitVisibilityLevels();
                team?.RebuildVisibilityCacheAllUnits(__instance.Combat.GetAllLivingCombatants());

            }
        }

        [HarmonyPatch(typeof(TurnDirector), "StartFirstRound")]
        public static class TurnDirector_StartFirstRound
        {
            public static void Postfix(TurnDirector __instance)
            {
                if (__instance.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                //will need to add handling for AI teams in here
                var team = __instance.Combat.Teams.First(x => x.IsLocalPlayer);
                var dm = team.Combat.DataManager;

                foreach (var abilityDefKVP in dm.AbilityDefs.Where(x =>
                    x.Value.specialRules == AbilityDef.SpecialRules.SpawnTurret ||
                    x.Value.specialRules == AbilityDef.SpecialRules.Strafe))
                {

                    if (team.units.Any(x => x.GetPilot().Abilities.Any(y => y.Def == abilityDefKVP.Value)) ||
                        team.units.Any(x => x.ComponentAbilities.Any(z => z.Def == abilityDefKVP.Value)))
                    {
                        //only do things for abilities that pilots have? move things here. also move AbstractActor initialization to ability start to minimize neutralTeam think time, etc. and then despawn? - done
                        var ability = new Ability(abilityDefKVP.Value);
                        ability.Init(team.Combat);
                        if (ModState.CommandAbilities.All(x => x != ability))
                        {
                            ModState.CommandAbilities.Add(ability);
                        }

                        ModInit.modLog.LogMessage($"Added {ability?.Def?.Id} to CommandAbilities");

                    }
                }
            }
        }

        [HarmonyPatch(typeof(TurnDirector), "IncrementActiveTurnActor")]
        public static class TurnDirector_IncrementActiveTurnActor
        {
            public static void Prefix(TurnDirector __instance)
            {
                if (__instance.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;
                if (ModState.deferredInvokeSpawns.Count > 0 && __instance.ActiveTurnActor is Team activeTeam &&
                    activeTeam.IsLocalPlayer)
                {
                    for (var index = 0; index < ModState.deferredInvokeSpawns.Count; index++)
                    {
                        var spawn = ModState.deferredInvokeSpawns[index].Value;
                        var resource = ModState.deferredInvokeSpawns[index].Key.Split('@');
                        ModState.deferredActorResource = resource[1];
                        ModInit.modLog.LogMessage(
                            $"Found deferred spawner at index {index} of {ModState.deferredInvokeSpawns.Count - 1}, invoking and trying to spawn {ModState.deferredActorResource}.");
                        ModState.FromDelegate = true;
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
                if (def.specialRules == AbilityDef.SpecialRules.Strafe &&
                    ModInit.modSettings.strafeEndsActivation)
                {
                    if (theActor is Mech mech)
                    {
                        mech.GenerateAndPublishHeatSequence(-1, true, false, theActor.GUID);
                    }

                    theActor.DoneWithActor();
                    theActor.OnActivationEnd(theActor.GUID, __instance.GetInstanceID());
                    return;
                }

                if (def.specialRules == AbilityDef.SpecialRules.SpawnTurret &&
                    ModInit.modSettings.spawnTurretEndsActivation)
                {
                    if (theActor is Mech mech)
                    {
                        mech.GenerateAndPublishHeatSequence(-1, true, false, theActor.GUID);
                    }

                    theActor.DoneWithActor();
                    theActor.OnActivationEnd(theActor.GUID, __instance.GetInstanceID());
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
                if (def.specialRules == AbilityDef.SpecialRules.Strafe &&
                    ModInit.modSettings.strafeEndsActivation)
                {
                    if (theActor is Mech mech)
                    {
                        mech.GenerateAndPublishHeatSequence(-1, true, false, theActor.GUID);
                    }

                    theActor.DoneWithActor();
                    theActor.OnActivationEnd(theActor.GUID, __instance.GetInstanceID());
                    return;
                }

                if (def.specialRules == AbilityDef.SpecialRules.SpawnTurret &&
                    ModInit.modSettings.spawnTurretEndsActivation)
                {
                    if (theActor is Mech mech)
                    {
                        mech.GenerateAndPublishHeatSequence(-1, true, false, theActor.GUID);
                    }

                    theActor.DoneWithActor();
                    theActor.OnActivationEnd(theActor.GUID, __instance.GetInstanceID());
                }
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
                var distance = Mathf.RoundToInt(Vector3.Distance(theActor.CurrentPosition, worldPos));
                var maxRange = Mathf.RoundToInt(__instance.FromButton.Ability.Def.IntParam2);
                if (__instance.FromButton.Ability.Def.specialRules == AbilityDef.SpecialRules.SpawnTurret &&
                    distance > maxRange && ___numPositionsLocked == 0)
                {
                    ModState.OutOfRange = true;
                    CombatSpawningReticle.Instance.HideReticle();
//                    ModInit.modLog.LogMessage($"Cannot spawn turret with coordinates farther than __instance.Ability.Def.IntParam2: {__instance.FromButton.Ability.Def.IntParam2}");
                    return false;
                }

                ModState.OutOfRange = false;
                return true;
            }
        }

        [HarmonyPatch(typeof(SelectionStateCommandTargetTwoPoints), "ProcessMousePos")]
        public static class SelectionStateCommandTargetTwoPoints_ProcessMousePos
        {
            public static bool Prefix(SelectionStateCommandTargetTwoPoints __instance, Vector3 worldPos,
                int ___numPositionsLocked)
            {
                if (UnityGameInstance.BattleTechGame.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return true;
                CombatTargetingReticle.Instance.ShowReticle();
                var HUD = Traverse.Create(__instance).Property("HUD").GetValue<CombatHUD>();
                var positionA = Traverse.Create(__instance).Property("positionA").GetValue<Vector3>();
                var positionB = Traverse.Create(__instance).Property("positionB").GetValue<Vector3>();

                var theActor = HUD.SelectedActor;
                var distance = Mathf.RoundToInt(Vector3.Distance(theActor.CurrentPosition, worldPos));
                var distanceToA = Mathf.RoundToInt(Vector3.Distance(theActor.CurrentPosition, positionA));
                var distanceToB = Mathf.RoundToInt(Vector3.Distance(theActor.CurrentPosition, positionB));

                var maxRange = Mathf.RoundToInt(__instance.FromButton.Ability.Def.IntParam2);
                var radius = __instance.FromButton.Ability.Def.FloatParam1;
                CombatTargetingReticle.Instance.UpdateReticle(positionA, positionB, radius, false);
                if (__instance.FromButton.Ability.Def.specialRules == AbilityDef.SpecialRules.Strafe &&
                    (distance > maxRange && ___numPositionsLocked == 0) ||
                    (distanceToA > maxRange && ___numPositionsLocked == 1))
                {
                    ModState.OutOfRange = true;
                    CombatTargetingReticle.Instance.HideReticle();
//                    ModInit.modLog.LogMessage($"Cannot strafe with coordinates farther than __instance.Ability.Def.IntParam2: {__instance.FromButton.Ability.Def.IntParam2}");
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
                //ModState.popupActorResource = actorResource;
                if (hk && string.IsNullOrEmpty(ModState.deferredActorResource) && ___numPositionsLocked < 1 &&
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
                        beacons = Utils.GetOwnedDeploymentBeaconsOfByTypeAndTag(type, "CanSpawnTurret", __instance.FromButton.Ability.Def.StringParam2);
                    }
                    else if (__instance.FromButton.Ability.Def.specialRules == AbilityDef.SpecialRules.Strafe)
                    {
                        beacons = Utils.GetOwnedDeploymentBeaconsOfByTypeAndTag(type, "CanStrafe", __instance.FromButton.Ability.Def.StringParam2);
                    }

                    beacons.Sort((MechComponentRef x, MechComponentRef y) =>
                        string.CompareOrdinal(x.Def.Description.UIName, y.Def.Description.UIName));
                    ModInit.modLog.LogMessage("sorted beacons at SSCT2Pts");

                    for (var index = 0; index < beacons.Count; index++)
                    {
                        var beacon = beacons[index];
                        var id = beacon.Def.ComponentTags.FirstOrDefault(x =>
                            x.StartsWith("mechdef_") || x.StartsWith("vehicledef_") ||
                            x.StartsWith("turretdef_"));
                        if (id.StartsWith("mechdef_"))
                        {
                            dm.MechDefs.TryGet(id, out var beaconunit);
                            beaconDescs +=
                                $"{index + 2}: {beaconunit?.Description?.UIName ?? beaconunit?.Description?.Name} - You have {ModState.deploymentAssetsStats.FirstOrDefault(x => x.ID == id).contractUses} remaining.\n\n";
                        }
                        else if (id.StartsWith("vehicledef_"))
                        {
                            dm.VehicleDefs.TryGet(id, out var beaconunit);
                            beaconDescs +=
                                $"{index + 2}: {beaconunit?.Description?.UIName ?? beaconunit?.Description?.Name} - You have {ModState.deploymentAssetsStats.FirstOrDefault(x => x.ID == id).contractUses} remaining.\n\n";
                        }
                        else
                        {
                            dm.TurretDefs.TryGet(id, out var beaconunit);
                            beaconDescs +=
                                $"{index + 2}: {beaconunit?.Description?.UIName ?? beaconunit?.Description?.Name} - You have {ModState.deploymentAssetsStats.FirstOrDefault(x => x.ID == id).contractUses} remaining.\n\n";
                        }
                    }

                    var popup = GenericPopupBuilder
                        .Create("Select a unit to deploy",
                            beaconDescs)
                        .AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants
                            .PopupBackfill));
                    popup.AlwaysOnTop = true;
                    popup.AddButton("1.", () => { });
                    ModInit.modLog.LogMessage(
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
                            ModInit.modLog.LogMessage(
                                $"beacon for button 2. will be {beacon.Def.Description.Name}, ID will be {id}, pilot will be {pilotID}");
                            popup.AddButton("2.", (Action) (() =>
                            {
                                ModState.popupActorResource = id;
                                ModState.PilotOverride = pilotID;
                                ModInit.modLog.LogMessage(
                                    $"Player pressed {id} with pilot {pilotID}. Now -{ModState.popupActorResource}- and pilot -{ModState.PilotOverride}- should be the same.");
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
                            ModInit.modLog.LogMessage(
                                $"beacon for button 3. will be {beacon.Def.Description.Name}, ID will be {id}, pilot will be {pilotID}");
                            var id1 = id;
                            var pilotID1 = pilotID;
                            popup.AddButton("3.", (Action) (() =>
                            {
                                ModState.popupActorResource = id1;
                                ModState.PilotOverride = pilotID1;
                                ModInit.modLog.LogMessage(
                                    $"Player pressed {id1} with pilot {pilotID1}. Now -{ModState.popupActorResource}- and pilot -{ModState.PilotOverride}- should be the same.");
                            }));

                            beacon = beacons[0];
                            id = beacon.Def.ComponentTags.FirstOrDefault((string x) =>
                                x.StartsWith("mechdef_") || x.StartsWith("vehicledef_") ||
                                x.StartsWith("turretdef_"));
                            pilotID = beacon.Def.ComponentTags.FirstOrDefault(x => x.StartsWith("StratOpsPilot_"))
                                ?.Remove(0, 14);
                            ModInit.modLog.LogMessage(
                                $"beacon for button 2. will be {beacon.Def.Description.Name}, ID will be {id}, pilot will be {pilotID}");
                            popup.AddButton("2.", (Action) (() =>
                            {
                                ModState.popupActorResource = id;
                                ModState.PilotOverride = pilotID;
                                ModInit.modLog.LogMessage(
                                    $"Player pressed {id} with pilot {pilotID}. Now -{ModState.popupActorResource}- and pilot -{ModState.PilotOverride}- should be the same.");
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
                        ModInit.modLog.LogMessage(
                            $"beacon for button 3. will be {beacon.Def.Description.Name}, ID will be {id}, pilot will be {pilotID}");
                        var id1 = id;
                        var pilotID1 = pilotID;
                        popup.AddButton("3.", (Action) (() =>
                        {
                            ModState.popupActorResource = id1;
                            ModState.PilotOverride = pilotID1;
                            ModInit.modLog.LogMessage(
                                $"Player pressed {id1} with pilot {pilotID1}. Now -{ModState.popupActorResource}- and pilot -{ModState.PilotOverride}- should be the same.");
                        }));

                        beacon = beacons[0];
                        id = beacon.Def.ComponentTags.FirstOrDefault((string x) =>
                            x.StartsWith("mechdef_") || x.StartsWith("vehicledef_") ||
                            x.StartsWith("turretdef_"));
                        pilotID = beacon.Def.ComponentTags.FirstOrDefault(x => x.StartsWith("StratOpsPilot_"))
                            ?.Remove(0, 14);
                        ModInit.modLog.LogMessage(
                            $"beacon for button 2. will be {beacon.Def.Description.Name}, ID will be {id}, pilot will be {pilotID}");
                        var id2 = id;
                        var pilotID2 = pilotID;
                        popup.AddButton("2.", (Action) (() =>
                        {
                            ModState.popupActorResource = id2;
                            ModState.PilotOverride = pilotID2;
                            ModInit.modLog.LogMessage(
                                $"Player pressed {id2} with pilot {pilotID2}. Now -{ModState.popupActorResource}- and pilot -{ModState.PilotOverride}- should be the same.");
                        }));

                        for (var index = 2; index < beacons.Count; index++)
                        {
                            beacon = beacons[index];
                            id = beacon.Def.ComponentTags.FirstOrDefault(x =>
                                x.StartsWith("mechdef_") || x.StartsWith("vehicledef_") ||
                                x.StartsWith("turretdef_"));
                            ModInit.modLog.LogMessage(
                                $"beacon for button {index + 2}. will be {beacon.Def.Description.Name}, ID will be {id}, pilot will be {pilotID}");
                            pilotID = beacon.Def.ComponentTags.FirstOrDefault(x => x.StartsWith("StratOpsPilot_"))
                                ?.Remove(0, 14);
                            var buttonName = $"{index + 2}.";
                            if (string.IsNullOrEmpty(id)) continue;
                            var id3 = id;
                            var pilotID3 = pilotID;
                            popup.AddButton(buttonName,
                                (Action) (() =>
                                {
                                    ModState.popupActorResource = id3;
                                    ModState.PilotOverride = pilotID3;
                                    ModInit.modLog.LogMessage(
                                        $"Player pressed {id3} with pilot {pilotID3}. Now -{ModState.popupActorResource}- and pilot -{ModState.PilotOverride}- should be the same.");
                                }));
                            ModInit.modLog.LogMessage(
                                $"Added button for {buttonName}");
                        }
                    }

                    RenderNow:
                    popup.CancelOnEscape();
                    popup.Render();

                    var HUD = Traverse.Create(__instance).Property("HUD").GetValue<CombatHUD>();
                    var theActor = HUD.SelectedActor;
                    var distance = Mathf.RoundToInt(Vector3.Distance(theActor.CurrentPosition, worldPos));
                    var maxRange = Mathf.RoundToInt(__instance.FromButton.Ability.Def.IntParam2);
                    __result = true;
                    if ((__instance.FromButton.Ability.Def.specialRules == AbilityDef.SpecialRules.Strafe &&
                         distance > maxRange) ||
                        (__instance.FromButton.Ability.Def.specialRules ==
                         AbilityDef.SpecialRules.SpawnTurret &&
                         distance > maxRange && ___numPositionsLocked < 1))
                    {
                        __result = false;
                        return false;
                    }

                    return true;
                }

                return true;
            }
        }

        // this is just for testing AI evaluator. sorta. hopefully.
        [HarmonyPatch(typeof(SelectionStateCommandTargetTwoPoints), "ProcessPressedButton")]
        public static class SelectionStateCommandTargetTwoPoints_ProcessPressedButton
        {
            static bool Prepare() => false; //keeping for testing but disabled for doves build
            public static bool Prefix(SelectionStateCommandTargetTwoPoints __instance, string button, ref bool __result)
            {
                if (button == "BTN_FireConfirm")
                {
                    var dmg = AI_Utils.EvaluateStrafing(__instance.SelectedActor, out Ability ability,
                        out Vector3 start,
                        out Vector3 end);
                    if (dmg > 1)
                    {
                        __instance.FromButton.ActivateCommandAbility(__instance.SelectedActor.team.GUID, start, end);
                        ModInit.modLog.LogMessage(
                            $"activated ability at pos {start.x}, {start.y},{start.z} and {end.x}, {end.y},{end.z}");
                        __result = true;
                        return false;
                    }

                    ModInit.modLog.LogMessage(
                        $"dmg <1");
                    __result = true;
                    return false;
                }

                ModInit.modLog.LogMessage(
                    $"button fucked up");
                __result = true;
                return false;
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
                if (string.IsNullOrEmpty(ModInit.modSettings.customSpawnReticleAsset))
                {
                    var circle = GameObject.Find("ReticleDecalCircle");
                    var decalFromCirle = circle.GetComponent<BTUIDecal>();
                    var dm = UnityGameInstance.BattleTechGame.DataManager;
                    var newTexture = dm.GetObjectOfType<Texture2D>(ModInit.modSettings.customSpawnReticleAsset,
                        BattleTechResourceType.Texture2D);
                    decalFromCirle.DecalMaterial.mainTexture = newTexture;
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
    }
}