using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using CustAmmoCategories;
using CustomAmmoCategoriesPatches;
using FogOfWar;
using Harmony;
using HBS;
using UnityEngine;

namespace StrategicOperations.Patches
{
    class DirtyDebugPatches
    {
        [HarmonyPatch(typeof(MultiSequence), "RevealRadius", new Type[] {typeof(Vector3), typeof(float), typeof(string)})]
        public static class MultiSequence_RevealRadius_GrossPatchReplacer
        {
            public static bool Prefix(MultiSequence __instance, Vector3 focalPosition, float revealRadius, string parentGUID, ref GameObject ___focalPoint, ref List<PilotableActorRepresentation> ___shownList, ref int ___nextRevealatronIndex)
            {
                if (revealRadius != 0f)
                {
                    var combat = UnityGameInstance.BattleTechGame.Combat;
                    //__instance.ClearFocalPoint();
                    if (___focalPoint != null)
                    {
                        UnityEngine.Object.Destroy(___focalPoint);
                        ___focalPoint = null;
                    }

                    ___focalPoint = new GameObject("focalPoint");
                    SnapToTerrain snapToTerrain = ___focalPoint.AddComponent<SnapToTerrain>();
                    snapToTerrain.verticalOffset = 10f;
                    snapToTerrain.transform.position = focalPosition;
                    snapToTerrain.UpdatePosition();
                    FogOfWarRevealatron fogOfWarRevealatron = ___focalPoint.AddComponent<FogOfWarRevealatron>();

                    ___nextRevealatronIndex++;
                    fogOfWarRevealatron.GUID = parentGUID + string.Format(".{0}", ___nextRevealatronIndex);
                    fogOfWarRevealatron.radiusMeters = revealRadius;
                    LazySingletonBehavior<FogOfWarView>.Instance.FowSystem.AddRevealatronViewer(fogOfWarRevealatron);
                    List<AbstractActor> allActors = combat.AllActors;
                    //__instance.ClearShownList();

                    if (___shownList != null)
                    {
                        List<ICombatant> allCombatants = combat.GetAllCombatants();
                        for (int i = 0; i < ___shownList.Count; i++)
                        {
                            ___shownList[i].ClearForcedPlayerVisibilityLevel(allCombatants);
                        }
                        ___shownList = null;
                    }

                    ___shownList = new List<PilotableActorRepresentation>();
                    for (int i = 0; i < allActors.Count; i++)
                    {
                        AbstractActor abstractActor = allActors[i];
                        UnitSpawnPointGameLogic itemByGUID = combat.ItemRegistry.GetItemByGUID<UnitSpawnPointGameLogic>(abstractActor.spawnerGUID);
                        var position = new Vector3(9999.9f, 0f, 0f);
                        if (itemByGUID != null)
                        {
                            position = itemByGUID.Position;
                        }

                        if (!allActors[i].team.IsLocalPlayer && (Vector3.Distance(allActors[i].CurrentPosition, focalPosition) < revealRadius || (abstractActor.IsTeleportedOffScreen && Vector3.Distance(position, focalPosition) < revealRadius)))
                        {
                            PilotableActorRepresentation pilotableActorRepresentation = abstractActor.GameRep as PilotableActorRepresentation;
                            if (pilotableActorRepresentation != null)
                            {
                                pilotableActorRepresentation.SetForcedPlayerVisibilityLevel(VisibilityLevel.LOSFull, false);
                                ___shownList.Add(pilotableActorRepresentation);
                            }
                        }
                    }
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(ReserveActorInvocation), "Invoke", new Type[] { typeof(CombatGameState)})]
        public static class ReserveActorInvocation_Invoke_ShittyBypass
        {
            static bool Prepare() => true; //enabled
            public static void Prefix(ReserveActorInvocation __instance, CombatGameState combatGameState)
            {
                if (__instance.targetRound != combatGameState.TurnDirector.CurrentRound)
                {
                    ModInit.modLog.LogMessage($"[ReserveActorInvocation.Invoke]: Running shitty bypass");
                    var actor = combatGameState.FindActorByGUID(__instance.targetGUID);
                    if (!actor.team.IsLocalPlayer)
                    {
                        __instance.targetRound = combatGameState.TurnDirector.CurrentRound;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Vehicle), "ArmorForLocation")]
        public static class Vehicle_ArmorForLocation_Dirty // dirty skip playimpact on despawned/dead actor?
        {
            static bool Prepare() => false;
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
        }
    }
}
