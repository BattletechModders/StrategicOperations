using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
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
                }
                ModInit.modLog.LogMessage(
                    $"Team.ActivateAbility needs to add handling for targetingtype {ability.Def.Targeting}");
                return false;
                publishAbilityConfirmed:
           //     Utils.CooldownAllCMDAbilities(); // this initiates a cooldown for ALL cmd abilities when one is used. dumb.
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
                TB_StrafeSequence eventSequence = new TB_StrafeSequence(abstractActor, positionA, positionB, radius, team);
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

        [HarmonyPatch(typeof(Ability), "ActivateSpawnTurret")]
        public static class Ability_ActivateSpawnTurret
        {
            public static bool Prefix(Ability __instance, Team team, Vector3 positionA, Vector3 positionB)
            {
                if (ModState.deferredInvokeSpawn == null)
                {
                    ModInit.modLog.LogMessage($"Deferred Spawner = null, creating delegate and returning false.");
                    ModState.deferredInvokeSpawn = () =>
                        Utils._activateSpawnTurretMethod.Invoke(__instance,
                            new object[] {team, positionA, positionB});
                    var flares = Traverse.Create(__instance).Method("SpawnFlares",new object[] { positionA, positionA, __instance.Def.StringParam1, 1, 1});
                    flares.GetValue();
                    return false;
                }

                var combat = UnityGameInstance.BattleTechGame.Combat;

                var cmdLance = Utils.CreateCMDLance(team.SupportTeam);

                Quaternion quaternion = Quaternion.LookRotation(positionB - positionA);
                var spawnTurretMethod = Traverse.Create(__instance).Method("SpawnTurret", new object[]{team.SupportTeam, __instance.Def.ActorResource, positionA, quaternion});
                var turretActor = spawnTurretMethod.GetValue<AbstractActor>();

                team.SupportTeam.AddUnit(turretActor);
                turretActor.AddToTeam(team.SupportTeam);

                turretActor.AddToLance(cmdLance);
                turretActor.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(__instance.Combat.BattleTechGame, turretActor, BehaviorTreeIDEnum.CoreAITree);
                
                turretActor.OnPositionUpdate(positionA, quaternion, -1, true, null, false);
                turretActor.DynamicUnitRole = UnitRole.Turret;
                
                UnitSpawnedMessage message = new UnitSpawnedMessage("FROM_ABILITY", turretActor.GUID);

                __instance.Combat.MessageCenter.PublishMessage(message);
                turretActor.OnPlayerVisibilityChanged(VisibilityLevel.LOSFull);
               
                var stackID = combat.StackManager.NextStackUID;

                var component =
                    UnitDropPodSpawner.UnitDropPodSpawnerInstance.GetComponentInParent<EncounterLayerParent>();
                if (component.dropPodLandedPrefab != null)
                {
                    UnitDropPodSpawner.UnitDropPodSpawnerInstance.LoadDropPodPrefabs(component.DropPodVfxPrefab, component.dropPodLandedPrefab);
                }

                var dropPodAnimationSequence = new GenericAnimationSequence(combat);
                EncounterLayerParent.EnqueueLoadAwareMessage(new AddSequenceToStackMessage(dropPodAnimationSequence));

                UnitDropPodSpawner.UnitDropPodSpawnerInstance.StartDropPodAnimation(0.75f, null, stackID, stackID); // maybe dont need this? probably should test in urban environment.

                ModState.ResetDeferredSpawner();
                return false;
            }
        }

        [HarmonyPatch(typeof(Ability), "SpawnFlares")]
        public static class Ability_SpawnFlares
        {
            private static bool Prefix(Ability __instance, Vector3 positionA, Vector3 positionB, string prefabName, int numFlares, int numPhases)
            {
                Vector3 b = (positionB - positionA) / (float)(numFlares - 1);

                Vector3 line = positionB - positionA;
                Vector3 left = Vector3.Cross(line, Vector3.up).normalized;
                Vector3 right = -left;

                var startLeft = positionA + (left * __instance.Def.FloatParam1);
                var startRight = positionA + (right * __instance.Def.FloatParam1);

                Vector3 vector = positionA;

                vector.y = __instance.Combat.MapMetaData.GetLerpedHeightAt(vector, false);
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
                    ObjectSpawnData item = new ObjectSpawnData(prefabName, startRight, Quaternion.identity, true, false);
                    list.Add(item);
                    startRight += b;
                    startRight.y = __instance.Combat.MapMetaData.GetLerpedHeightAt(startRight, false);
                }

                SpawnObjectSequence spawnObjectSequence = new SpawnObjectSequence(__instance.Combat, list);
                __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(spawnObjectSequence));
                List<ObjectSpawnData> spawnedObjects = spawnObjectSequence.spawnedObjects;
                CleanupObjectSequence eventSequence = new CleanupObjectSequence(__instance.Combat, spawnedObjects);
                TurnEvent tEvent = new TurnEvent(Guid.NewGuid().ToString(), __instance.Combat, numPhases, null, eventSequence, __instance.Def, false);
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
            public static void Postfix(TurnDirector __instance)
            {
                for (int i = 0; i < ModState.CommandAbilities.Count; i++)
                {
                    ModState.CommandAbilities[i].OnNewRound();
                }
            }
        }

        [HarmonyPatch(typeof(TurnDirector), "StartFirstRound")]
        public static class TurnDirector_StartFirstRound
        {
            public static void Postfix(TurnDirector __instance)
            {

                var team = __instance.Combat.Teams.First(x => x.IsLocalPlayer);
                var dm = team.Combat.DataManager;

                foreach (var abilityDefKVP in dm.AbilityDefs.Where(x=>x.Value.specialRules == AbilityDef.SpecialRules.SpawnTurret || x.Value.specialRules == AbilityDef.SpecialRules.Strafe))
                {

                    if (team.units.Any(x => x.GetPilot().Abilities.Any(y => y.Def == abilityDefKVP.Value)))
                    {
                        //only do things for abilities that pilots have? move things here. also move AbstractActor initialization to ability start to minimize neutralTeam think time, etc. and then despawn?
                        var ability = new Ability(abilityDefKVP.Value);
                    ability.Init(team.Combat);
                    if (ModState.CommandAbilities.All(x => x != ability))
                    {
                        ModState.CommandAbilities.Add(ability);
                    }

                    ModInit.modLog.LogMessage($"Added {ability?.Def?.Id} to CommandAbilities");

                    //need to create SpawnPointGameLogic? for magic safety teleporter ???s or disable SafetyTeleport logic for certain units?

                    dm.PilotDefs.TryGet("pilot_sim_starter_dekker", out var supportPilotDef);

                    if (__instance.Combat.Teams.All(x => x.GUID != "61612bb3-abf9-4586-952a-0559fa9dcd75"))
                    {
                        Utils.CreateOrUpdateNeutralTeam();
                    }
                    var neutralTeam =
                        __instance.Combat.Teams.FirstOrDefault(x => x.GUID == "61612bb3-abf9-4586-952a-0559fa9dcd75");
                    
                    ModInit.modLog.LogMessage($"Team neturalTeam = {neutralTeam?.DisplayName}");
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
                    }
                }
            }
        }

        [HarmonyPatch(typeof(TurnDirector), "IncrementActiveTurnActor")]
        public static class TurnDirector_IncrementActiveTurnActor
        {
            public static void Prefix(TurnDirector __instance)
            {
                if (ModState.deferredInvokeSpawn != null && __instance.ActiveTurnActor is Team activeTeam && activeTeam.IsLocalPlayer)
                {
                    ModInit.modLog.LogMessage($"Found deferred spawner, invoking.");
                    ModState.deferredInvokeSpawn();
                    ModState.ResetDeferredSpawner();
                }
            }
        }

        [HarmonyPatch(typeof(CombatHUDActionButton), "ActivateCommandAbility", new Type[]{typeof(string), typeof(Vector3), typeof(Vector3)})]
        public static class CombatHUDActionButton_ActivateCommandAbility
        {
            public static bool Prefix(CombatHUDActionButton __instance, string teamGUID, Vector3 positionA, Vector3 positionB, out bool __state)
            {
                var combat = UnityGameInstance.BattleTechGame.Combat;

                var HUD = Traverse.Create(__instance).Property("HUD").GetValue<CombatHUD>();
                var theActor = HUD.SelectedActor;
                var distanceToA = Mathf.RoundToInt(Vector3.Distance(theActor.CurrentPosition, positionA));
                var distanceToB = Mathf.RoundToInt(Vector3.Distance(theActor.CurrentPosition, positionB));

                var maxRange = Mathf.RoundToInt(__instance.Ability.Def.IntParam2);

                if (__instance.Ability.Def.specialRules == AbilityDef.SpecialRules.Strafe && distanceToA > maxRange)
                {
                    var popup = GenericPopupBuilder.Create(GenericPopupType.Info, $"INVALID GRID COORDINATES\n\nDistance to target marker A: {distanceToA}\nDistance to target marker B: {distanceToB}\n\nMaximum Deployment Range: {maxRange}");
                    popup.AddButton("Acknowledged");
                    popup.IsNestedPopupWithBuiltInFader().CancelOnEscape().Render();

                    ModInit.modLog.LogMessage($"Cannot activate strafe with coordinates farther than __instance.Ability.Def.IntParam2: {__instance.Ability.Def.IntParam2}");
                    __state = false;
                    return false;
                }
                
                if (__instance.Ability.Def.specialRules == AbilityDef.SpecialRules.SpawnTurret && distanceToA > maxRange)
                {
                    var popup = GenericPopupBuilder.Create(GenericPopupType.Info, $"INVALID DEPLOYMENT COORDINATES\n\nDistance to Deployment: {distanceToA}\n\nMaximum Deployment Range: {maxRange}");
                    popup.AddButton("Acknowledged");
                    popup.IsNestedPopupWithBuiltInFader().CancelOnEscape().Render();

                    ModInit.modLog.LogMessage($"Cannot spawn turret with coordinates farther than __instance.Ability.Def.IntParam2: {__instance.Ability.Def.IntParam2}");
                    __state = false;
                    return false;
                }
                __state = true;
                return true;
            }

            public static void Postfix(CombatHUDActionButton __instance, string teamGUID, Vector3 positionA,
                Vector3 positionB, bool __state)
            {
                if (!__state) return;
                var HUD = Traverse.Create(__instance).Property("HUD").GetValue<CombatHUD>();
                var theActor = HUD.SelectedActor;
                if (__instance.Ability.Def.specialRules == AbilityDef.SpecialRules.Strafe &&
                    ModInit.modSettings.strafeEndsActivation)
                {
                    theActor.OnActivationEnd(theActor.GUID, __instance.GetInstanceID());
                }
                if (__instance.Ability.Def.specialRules == AbilityDef.SpecialRules.SpawnTurret &&
                    ModInit.modSettings.spawnTurretEndsActivation)
                {
                    theActor.OnActivationEnd(theActor.GUID, __instance.GetInstanceID());
                }
            }
        }

        [HarmonyPatch(typeof(SelectionStateCommandSpawnTarget), "ProcessMousePos")]
        public static class SelectionStateCommandSpawnTarget_ProcessMousePos
        {
            public static bool Prefix(SelectionStateCommandSpawnTarget __instance, Vector3 worldPos, int ___numPositionsLocked)
            {
                CombatSpawningReticle.Instance.ShowReticle();
                var HUD = Traverse.Create(__instance).Property("HUD").GetValue<CombatHUD>();
                var theActor = HUD.SelectedActor;
                var distance = Mathf.RoundToInt(Vector3.Distance(theActor.CurrentPosition, worldPos));
                var maxRange = Mathf.RoundToInt(__instance.FromButton.Ability.Def.IntParam2);
                if (__instance.FromButton.Ability.Def.specialRules == AbilityDef.SpecialRules.SpawnTurret && distance > maxRange && ___numPositionsLocked == 0)
                {
                    CombatSpawningReticle.Instance.HideReticle();
//                    ModInit.modLog.LogMessage($"Cannot spawn turret with coordinates farther than __instance.Ability.Def.IntParam2: {__instance.FromButton.Ability.Def.IntParam2}");
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(SelectionStateCommandTargetTwoPoints), "ProcessMousePos")]
        public static class SelectionStateCommandTargetTwoPoints_ProcessMousePos
        {
            public static bool Prefix(SelectionStateCommandTargetTwoPoints __instance, Vector3 worldPos, int ___numPositionsLocked)
            {
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
                CombatTargetingReticle.Instance.UpdateReticle(positionA,positionB, radius, false);
                if (__instance.FromButton.Ability.Def.specialRules == AbilityDef.SpecialRules.Strafe && (distance > maxRange && ___numPositionsLocked == 0) || (distanceToA > maxRange && ___numPositionsLocked == 1))
                {
                    CombatTargetingReticle.Instance.HideReticle();
//                    ModInit.modLog.LogMessage($"Cannot strafe with coordinates farther than __instance.Ability.Def.IntParam2: {__instance.FromButton.Ability.Def.IntParam2}");
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(SelectionStateCommandTargetTwoPoints), "ProcessLeftClick")]
        public static class SelectionStateCommandTargetTwoPoints_ProcessLeftClick
        {
            public static bool Prefix(SelectionStateCommandTargetTwoPoints __instance, Vector3 worldPos, int ___numPositionsLocked, ref bool __result)
            {
                var HUD = Traverse.Create(__instance).Property("HUD").GetValue<CombatHUD>();
                var theActor = HUD.SelectedActor;
                var distance = Mathf.RoundToInt(Vector3.Distance(theActor.CurrentPosition, worldPos));
                var maxRange = Mathf.RoundToInt(__instance.FromButton.Ability.Def.IntParam2);
                __result = true;
                if ((__instance.FromButton.Ability.Def.specialRules == AbilityDef.SpecialRules.Strafe || (__instance.FromButton.Ability.Def.specialRules == AbilityDef.SpecialRules.SpawnTurret)) && distance > maxRange)
                {
                    return false;
                }
                return true;
            }
        }
    }
}
