using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using BattleTech;
using BattleTech.UI;
using CustomUnits;
using Harmony;
using StrategicOperations.Framework;
using UnityEngine;
using static StrategicOperations.Framework.Classes;

namespace StrategicOperations.Patches
{
    class AI_DEBUG_Patches
    {

        [HarmonyPatch(typeof(SortMoveCandidatesByInfMapNode), "Tick")]
        public static class SortMoveCandidatesByInfMapNode_Tick
        {
           static bool Prepare() => ModInit.modSettings.Debug;

            public static void Postfix(SortMoveCandidatesByInfMapNode __instance, ref BehaviorTreeResults __result)
            {
                ModInit.modLog?.Debug?.Write(
                    $"[SortMoveCandidatesByInfMapNode Tick] Sorting finished. Actor {__instance.unit.DisplayName} eval'd highest weighted position as {__instance.unit.BehaviorTree.influenceMapEvaluator.WorkspaceEvaluationEntries[0].Position} with weight {__instance.unit.BehaviorTree.influenceMapEvaluator.WorkspaceEvaluationEntries[0].GetHighestAccumulator()}");
            }
        }

        [HarmonyPatch(typeof(MoveTowardsHighestPriorityMoveCandidateNode), "Tick")]
        public static class MoveTowardsHighestPriorityMoveCandidateNode_Tick
        {
           static bool Prepare() => ModInit.modSettings.Debug;

            public static void Postfix(MoveTowardsHighestPriorityMoveCandidateNode __instance, ref BehaviorTreeResults __result)
            {
                ModInit.modLog?.Debug?.Write(
                    $"[MoveTowardsHighestPriorityMoveCandidateNode Tick] Moving towards highest eval'd position: Actor {__instance.unit.DisplayName} eval'd highest weighted position as {__instance.unit.BehaviorTree.influenceMapEvaluator.WorkspaceEvaluationEntries[0].Position} with weight {__instance.unit.BehaviorTree.influenceMapEvaluator.WorkspaceEvaluationEntries[0].GetHighestAccumulator()}");
            }
        }

        [HarmonyPatch(typeof(Mech), "JumpDistance", MethodType.Getter)]
        public static class Mech_JumpDistance
        {
            static bool Prepare() => false; //turned back off because fuck it, not my problem.
            public static void Prefix(Mech __instance, ref float __result)
            {
                try
                {
                    if (__instance is TrooperSquad squad)
                    {
                        var countJets = squad.workingJumpsLocations().Count;
                        ModInit.modLog?.Trace?.Write(
                            $"[Mech_JumpDistance PREFIX] value from mech {squad.DisplayName} - {squad.Description.Id} before calcs was {__result}, jets count: {countJets} ");
                    }
                }
                catch (Exception ex)
                {
                    ModInit.modLog?.Error?.Write(ex.ToString());
                }
            }

            public static void Postfix(Mech __instance, ref float __result)
            {
                if (__instance is TrooperSquad squad)
                {
                    try
                    {
                        var countJets = squad.workingJumpsLocations().Count;
                        ModInit.modLog?.Trace?.Write(
                            $"[Mech_JumpDistance POSTFIX] value from mech {squad.DisplayName} - {squad.Description.Id} before calcs was {__result}, jets count: {countJets} ");
                        if (float.IsPositiveInfinity(__result))
                        {
                            ModInit.modLog?.Trace?.Write($"[Mech_JumpDistance POSTFIX] INFINITY STONES!");
                        }
                    }
                    catch (Exception ex)
                    {
                        ModInit.modLog?.Error?.Write(ex.ToString());
                    }
                }
            }
        }
    }
}
