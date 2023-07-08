using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BattleTech;
using FogOfWar;
using HBS;
using UnityEngine;
using ArmorLocation = BattleTech.ArmorLocation;

namespace StrategicOperations.Patches
{
    class DirtyDebugPatches
    {
        [HarmonyPatch]
        //[HarmonyPatch(typeof(HitLocation), "GetHitLocation", new Type[] {typeof(Dictionary<ArmorLocation, int>), typeof(float), typeof(ArmorLocation), typeof(float)})]
        public static class HitLocation_GetHitLocation_MechArmorLoc
        {
            public static void Postfix(Dictionary<ArmorLocation, int> hitTable, float randomRoll, ArmorLocation bonusLocation, float bonusLocationMultiplier, ref ArmorLocation __result)
            {
                if (__result == ArmorLocation.None)
                {
                    ModInit.modLog?.Info?.Write($"[HitLocation.GetHitLocation]: Running shitty bypass due to GetHitLocation ArmorLocation.None, defaulting to ArmorLocation.CenterTorso");
                    __result = ArmorLocation.CenterTorso;
                }
                
            }

            public static MethodBase TargetMethod()
            {
                var method = AccessTools
                    .GetDeclaredMethods(typeof(HitLocation)).FirstOrDefault(x => x.Name == "GetHitLocation" && x.GetParameters().Length == 4)?.MakeGenericMethod(typeof(ArmorLocation));
                return method;
            }
        }

        [HarmonyPatch]
        //[HarmonyPatch(typeof(HitLocation), "GetHitLocation", new Type[] {typeof(Dictionary<ArmorLocation, int>), typeof(float), typeof(ArmorLocation), typeof(float)})]
        public static class HitLocation_GetHitLocation_VehicleChassicLoc
        {
            public static void Postfix(Dictionary<VehicleChassisLocations, int> hitTable, float randomRoll, VehicleChassisLocations bonusLocation, float bonusLocationMultiplier, ref VehicleChassisLocations __result)
            {
                if (__result == VehicleChassisLocations.None)
                {
                    ModInit.modLog?.Info?.Write($"[HitLocation.GetHitLocation]: Running shitty bypass due to GetHitLocation VehicleChassisLocations.None, defaulting to VehicleChassisLocations.Front");
                    __result = VehicleChassisLocations.Front;
                }

            }

            public static MethodBase TargetMethod()
            {
                var method = AccessTools
                    .GetDeclaredMethods(typeof(HitLocation)).FirstOrDefault(x => x.Name == "GetHitLocation" && x.GetParameters().Length == 4)?.MakeGenericMethod(typeof(VehicleChassisLocations));
                return method;
            }
        }


        [HarmonyPatch(typeof(MultiSequence), "RevealRadius", new Type[] {typeof(Vector3), typeof(float), typeof(string)})]
        public static class MultiSequence_RevealRadius_GrossPatchReplacer
        {
            public static void Prefix(ref bool __runOriginal, MultiSequence __instance, Vector3 focalPosition, float revealRadius, string parentGUID)
            {
                if (!__runOriginal) return;
                if (revealRadius != 0f)
                {
                    var combat = UnityGameInstance.BattleTechGame.Combat;
                    //__instance.ClearFocalPoint();
                    if (__instance.focalPoint != null)
                    {
                        UnityEngine.Object.Destroy(__instance.focalPoint);
                        __instance.focalPoint = null;
                    }

                    __instance.focalPoint = new GameObject("focalPoint");
                    SnapToTerrain snapToTerrain = __instance.focalPoint.AddComponent<SnapToTerrain>();
                    snapToTerrain.verticalOffset = 10f;
                    snapToTerrain.transform.position = focalPosition;
                    snapToTerrain.UpdatePosition();
                    FogOfWarRevealatron fogOfWarRevealatron = __instance.focalPoint.AddComponent<FogOfWarRevealatron>();

                    __instance.nextRevealatronIndex++;
                    fogOfWarRevealatron.GUID = parentGUID + string.Format(".{0}", __instance.nextRevealatronIndex);
                    fogOfWarRevealatron.radiusMeters = revealRadius;
                    
                    //LazySingletonBehavior<FogOfWarView>.Instance?.FowSystem?.AddRevealatronViewer(fogOfWarRevealatron);
                    var fowSystem = LazySingletonBehavior<FogOfWarView>.Instance?.FowSystem;
                    if (fowSystem == null)
                    {
                        __runOriginal = true;
                        return;
                    }
                    fowSystem.AddRevealatronViewer(fogOfWarRevealatron);
                    List <AbstractActor> GetAllLivingActors = combat.GetAllLivingActors();
                    //__instance.ClearShownList();

                    if (__instance.shownList != null)
                    {
                        List<ICombatant> allCombatants = combat.GetAllLivingCombatants();
                        for (int i = 0; i < __instance.shownList.Count; i++)
                        {
                            __instance.shownList[i].ClearForcedPlayerVisibilityLevel(allCombatants);
                        }
                        __instance.shownList = null;
                    }

                    __instance.shownList = new List<PilotableActorRepresentation>();
                    for (int i = 0; i < GetAllLivingActors.Count; i++)
                    {
                        AbstractActor abstractActor = GetAllLivingActors[i];
                        UnitSpawnPointGameLogic itemByGUID = combat.ItemRegistry.GetItemByGUID<UnitSpawnPointGameLogic>(abstractActor.spawnerGUID);
                        var position = new Vector3(9999.9f, 0f, 0f);
                        if (itemByGUID != null)
                        {
                            position = itemByGUID.Position;
                        }

                        if (!GetAllLivingActors[i].team.IsLocalPlayer && (Vector3.Distance(GetAllLivingActors[i].CurrentPosition, focalPosition) < revealRadius || (abstractActor.IsTeleportedOffScreen && Vector3.Distance(position, focalPosition) < revealRadius)))
                        {
                            PilotableActorRepresentation pilotableActorRepresentation = abstractActor.GameRep as PilotableActorRepresentation;
                            if (pilotableActorRepresentation != null)
                            {
                                pilotableActorRepresentation.SetForcedPlayerVisibilityLevel(VisibilityLevel.LOSFull, false);
                                __instance.shownList.Add(pilotableActorRepresentation);
                            }
                        }
                    }
                }
                __runOriginal = false;
                return;
            }
        }

        [HarmonyPatch(typeof(ReserveActorInvocation), "Invoke", new Type[] { typeof(CombatGameState)})]
        public static class ReserveActorInvocation_Invoke_ShittyBypass
        {
            public static void Prefix(ReserveActorInvocation __instance, CombatGameState combatGameState)
            {
                if (__instance.targetRound != combatGameState.TurnDirector.CurrentRound)
                {
                    ModInit.modLog?.Info?.Write($"[ReserveActorInvocation.Invoke]: Running shitty bypass");
                    var actor = combatGameState.FindActorByGUID(__instance.targetGUID);
                    if (!actor.team.IsLocalPlayer)
                    {
                        __instance.targetRound = combatGameState.TurnDirector.CurrentRound;
                    }
                }
            }

            static bool Prepare() => true; //enabled
        }

        [HarmonyPatch(typeof(Vehicle), "ArmorForLocation")]
        public static class Vehicle_ArmorForLocation_Dirty // dirty skip playimpact on despawned/dead actor?
        {
            public static void Prefix(Vehicle __instance, ref int loc)
            {
                switch (loc)
                {
                    case 32: loc = 8;
                        break;
                    case 64: loc = 4;
                        break;
                    case 128: loc = 8;
                        break;
                    case 256: loc = 16;
                        break;
                    case 512: loc = 16;
                        break;
                    case 1024: loc = 16;
                        break;
                }
            }

            static bool Prepare() => false;
        }
    }
}