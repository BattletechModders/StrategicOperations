using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using BattleTech.Data;
using Harmony;
using HBS.Logging;
using StrategicOperations.Framework;
using UnityEngine;

namespace StrategicOperations.Patches
{
    class StrategicOperationsPatches
    {
        [HarmonyPatch(typeof(SimGameState), "RequestDataManagerResources")]
        
        public static class SimGameState_RequestDataManagerResources_Patch
        {
            public static void Postfix(SimGameState __instance)
            {
                LoadRequest loadRequest = __instance.DataManager.CreateLoadRequest();
                loadRequest.AddAllOfTypeBlindLoadRequest(BattleTechResourceType.TurretDef, new bool?(true)); //but TurretDefs
                loadRequest.ProcessRequests(10U);
            }
        }

        [HarmonyPatch(typeof(TurnDirector), "OnInitializeContractComplete")]
        public static class TurnDirector_OnInitializeContractComplete
        {
            public static void Postfix(TurnDirector __instance, MessageCenterMessage message)
            {
                var dm = __instance.Combat.DataManager;
                LoadRequest loadRequest = dm.CreateLoadRequest();

                loadRequest.AddBlindLoadRequest(BattleTechResourceType.PilotDef, "pilot_sim_starter_dekker");
                ModInit.modLog.LogMessage($"Added loadrequest for PilotDef: pilot_sim_starter_dekker (hardcoded)");
                loadRequest.AddBlindLoadRequest(BattleTechResourceType.MechDef, "mechdef_hunchback_HBK-4G");
                ModInit.modLog.LogMessage($"Added loadrequest for MechDef: mechdef_hunchback_HBK-4G (hardcoded)");

                foreach (var abilityDef in dm.AbilityDefs.Where(x => x.Key.StartsWith("AbilityDefCMD_")))
                {
                    var ability = new Ability(abilityDef.Value);
                    if (string.IsNullOrEmpty(ability.Def?.ActorResource)) continue;
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
                loadRequest.ProcessRequests(1000u);
            }
        }
        [HarmonyPatch(typeof(GameRepresentation), "Update")]
        public static class GameRepresentation_Update
        {
            public static bool Prefix(GameRepresentation __instance, AbstractActor ____parentActor)
            {
                var combat = UnityGameInstance.BattleTechGame.Combat;
                var registry = combat.ItemRegistry;

                if (____parentActor == null || ____parentActor?.spawnerGUID == null)
                {
                    //ModInit.modLog.LogMessage($"Couldn't find UnitSpawnPointGameLogic for {____parentActor?.DisplayName}. Should be CMD Ability actor; skipping safety teleport!");
                    return false;
                }
                if (registry.GetItemByGUID<UnitSpawnPointGameLogic>(____parentActor?.spawnerGUID) != null) return true;
                //ModInit.modLog.LogMessage($"Couldn't find UnitSpawnPointGameLogic for {____parentActor?.DisplayName}. Should be CMD Ability actor; skipping safety teleport!");
                return false;
            }
        }
        [HarmonyPatch(typeof(Team), "ActivateAbility")]
        public static class Team_ActivateAbility
        {
            public static bool Prefix(Team __instance, AbilityMessage msg)
            {
                Ability ability = ModState.CommandAbilities.Find((Ability x) => x.Def.Id == msg.abilityID);
                if (ability == null)
                {
                    ModInit.modLog.LogMessage(
                        $"Team doesn't have CommandAbility {ability.Def.Description.Name}");
                    return false;
                }
                switch (ability.Def.Targeting)
                {
                    case AbilityDef.TargetingType.CommandInstant:
                        ability.Activate(__instance, null);
                        goto IL_12E;
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
                        goto IL_12E;
                    }
                    case AbilityDef.TargetingType.CommandTargetSinglePoint:
                        ability.Activate(__instance, msg.positionA);
                        goto IL_12E;
                    case AbilityDef.TargetingType.CommandTargetTwoPoints:
                    case AbilityDef.TargetingType.CommandSpawnPosition:
                        ability.Activate(__instance, msg.positionA, msg.positionB);
                        goto IL_12E;
                }
                ModInit.modLog.LogMessage(
                    $"Team.ActivateAbility needs to add handling for targetingtype {ability.Def.Targeting}");
                return false;
                IL_12E:
//                CooldownAllCMDAbilities(); // this seems stupid?
                __instance.Combat.MessageCenter.PublishMessage(new AbilityConfirmedMessage(msg.actingObjectGuid, msg.affectedObjectGuid, msg.abilityID, msg.positionA, msg.positionB));
                return false;
            }
        }
        [HarmonyPatch(typeof(Ability), "ActivateStrafe")]
        public static class Ability_ActivateStrafe
        {
            public static bool Prefix(Ability __instance, Team team, Vector3 positionA, Vector3 positionB, float radius)
            {
                ModInit.modLog.LogMessage($"Initializing Strafing Run!");
                AbstractActor abstractActor = team.FindSupportActor(__instance.Def.ActorResource);
                if (abstractActor == null)
                {
                    ModInit.modLog.LogMessage(
                        $"Couldn't find actor {__instance.Def.ActorResource} for ability {__instance.Def.Description.Name} - aborting");
                    return false;
                }
                TB_StrafeSequence eventSequence = new TB_StrafeSequence(abstractActor, positionA, positionB, radius);
                TurnEvent tEvent = new TurnEvent(Guid.NewGuid().ToString(), __instance.Combat, __instance.Def.ActivationETA, null, eventSequence, __instance.Def, false);
                __instance.Combat.TurnDirector.AddTurnEvent(tEvent);
                if (__instance.Def.IntParam1 > 0)
                {
                    var flares = Traverse.Create(__instance).Method("SpawnFlares",new object[] { positionA, positionB, __instance.Def.StringParam1,
                        __instance.Def.IntParam1, __instance.Def.ActivationETA});
                    flares.GetValue();
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(CombatGameState), "OnCombatGameDestroyed")]
        internal static class CombatGameState_OnCombatGameDestroyed
        {
            // Token: 0x06000013 RID: 19 RVA: 0x00003A0C File Offset: 0x00001C0C
            private static void Postfix(CombatGameState __instance)
            {
                ModState.Reset();
            }
        }

        [HarmonyPatch(typeof(TurnDirector), "StartFirstRound")]
        public static class TurnDirector_StartFirstRound
        {
            public static void Postfix(TurnDirector __instance)
            {
                //load resources like CJ does in DataLoadHelper

                var team = __instance.Combat.Teams.First(x => x.IsLocalPlayer);
                var dm = team.Combat.DataManager;

                foreach (var abilityDefKVP in dm.AbilityDefs.Where(x=>x.Key.StartsWith("AbilityDefCMD_")))
                {
                    var ability = new Ability(abilityDefKVP.Value);
                    ability.Init(team.Combat);
                    if (ModState.CommandAbilities.All(x => x != ability))
                    {
                        ModState.CommandAbilities.Add(ability);
                    }

                    ModInit.modLog.LogMessage($"Added {ability?.Def?.Id} to CommandAbilities");


                    //need to create SpawnPointGameLogic? for magic safety teleporter ???s or disable SafetyTeleport logic for certain units?

                    dm.PilotDefs.TryGet("pilot_sim_starter_dekker", out var supportPilotDef);
//                    var supportPilot = new Pilot(supportPilotDef, "", false);


                    var neutralTeam =
                        __instance.Combat.Teams.First(x => x.GUID == "61612bb3-abf9-4586-952a-0559fa9dcd75");
                    var cmdLance = Utils.CreateCMDLance(neutralTeam);
                    

                    if (!string.IsNullOrEmpty(ability.Def?.ActorResource))
                    {
                        if (ability.Def.specialRules == AbilityDef.SpecialRules.SpawnTurret) continue;

                        if (ability.Def.ActorResource.StartsWith("vehicledef_"))
                        {
                            dm.VehicleDefs.TryGet(ability.Def.ActorResource, out var supportActorVehicleDef);
                            supportActorVehicleDef.Refresh();
                            var supportActorVehicle = ActorFactory.CreateVehicle(supportActorVehicleDef, supportPilotDef, neutralTeam.EncounterTags, neutralTeam.Combat,
                                neutralTeam.GetNextSupportUnitGuid(), "", null);
                            supportActorVehicle.Init(neutralTeam.OffScreenPosition,0f,false);
                            supportActorVehicle.InitGameRep(null);
                            neutralTeam.AddUnit(supportActorVehicle);
                            supportActorVehicle.AddToTeam(neutralTeam);
                            supportActorVehicle.AddToLance(cmdLance);
                            team.SupportUnits.Add(supportActorVehicle);
                            supportActorVehicle.GameRep.gameObject.SetActive(true);
                            supportActorVehicle.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(__instance.Combat.BattleTechGame, supportActorVehicle, BehaviorTreeIDEnum.DoNothingTree);
                            ModInit.modLog.LogMessage($"Added {supportActorVehicle?.VehicleDef?.Description?.Id} to SupportUnits");
                        }

                        else
                        {
                            ModInit.modLog.LogMessage($"Something wrong with CMD Ability {ability.Def.Id}, invalid ActorResource");
                            continue;
                        }
                    }

                    if (string.IsNullOrEmpty(ability.Def?.WeaponResource)) continue;
                    dm.MechDefs.TryGet("mechdef_hunchback_HBK-4G", out var supportWeaponMechDef);
                    supportWeaponMechDef.Refresh();
                    var supportWeaponMech = ActorFactory.CreateMech(supportWeaponMechDef, supportPilotDef, neutralTeam.EncounterTags, neutralTeam.Combat,
                        neutralTeam.GetNextSupportUnitGuid(), "", null);
                    supportWeaponMech.Init(neutralTeam.OffScreenPosition,0f,false);
                    supportWeaponMech.InitGameRep(null);
                    neutralTeam.AddUnit(supportWeaponMech);
                    supportWeaponMech.AddToTeam(neutralTeam);
                    supportWeaponMech.AddToLance(cmdLance);
                    supportWeaponMech.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(__instance.Combat.BattleTechGame, supportWeaponMech, BehaviorTreeIDEnum.CoreAITree);
                    dm.WeaponDefs.TryGet(ability.Def.WeaponResource, out var weaponDef);
                    supportWeaponMech.GameRep.gameObject.SetActive(true);
                    var mcRef = new MechComponentRef(weaponDef?.Description?.Id, "", ComponentType.Weapon,
                        ChassisLocations.RightTorso, -1, ComponentDamageLevel.Functional, false) {DataManager = dm};
                    
                    mcRef.RefreshComponentDef();
                    var weapon = new Weapon(supportWeaponMech, neutralTeam.Combat, mcRef, "0"); // need to be on a unit of some kind ->creating supportWeaponMech. maybe can add to player mech but "hide" it?
                    //probably need to init Ammoboxes. potentially implement selector for different ammo types, but would require CAC.
                    
                    team.SupportWeapons.Add(weapon);
                    ModInit.modLog.LogMessage($"Added {weapon?.weaponDef?.Description?.Id} to SupportWeapons");

                }
            }
        }
    }
}
