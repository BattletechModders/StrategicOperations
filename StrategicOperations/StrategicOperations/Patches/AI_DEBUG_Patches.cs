using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using BattleTech;
using BattleTech.UI;
using Harmony;
using StrategicOperations.Framework;
using UnityEngine;
using static StrategicOperations.Framework.Classes;

namespace StrategicOperations.Patches
{
    class AI_DEBUG_Patches
    {

        [HarmonyPatch]
        public static class SortMoveCandidatesByInfMapNode_Tick 
        {
            public static MethodBase TargetMethod()
            {
                var type = AccessTools.TypeByName("SortMoveCandidatesByInfMapNode");
                return AccessTools.Method(type, "Tick");
            }

            public static void Postfix(ref BehaviorTreeResults __result, string ___name,
                AbstractActor ___unit)
            {
                ModInit.modLog.LogTrace($"[SortMoveCandidatesByInfMapNode Tick] Sorting finished. Actor {___unit.DisplayName} eval'd highest weighted position as {___unit.BehaviorTree.influenceMapEvaluator.WorkspaceEvaluationEntries[0].Position} with weight {___unit.BehaviorTree.influenceMapEvaluator.WorkspaceEvaluationEntries[0].GetHighestAccumulator()}");
            }
        }

        [HarmonyPatch]
        public static class MoveTowardsHighestPriorityMoveCandidateNode_Tick
        {
            public static MethodBase TargetMethod()
            {
                var type = AccessTools.TypeByName("MoveTowardsHighestPriorityMoveCandidateNode");
                return AccessTools.Method(type, "Tick");
            }

            public static void Postfix(ref BehaviorTreeResults __result, string ___name,
                AbstractActor ___unit)
            {
                ModInit.modLog.LogTrace($"[MoveTowardsHighestPriorityMoveCandidateNode Tick] Moving towards highest eval'd position: Actor {___unit.DisplayName} eval'd highest weighted position as {___unit.BehaviorTree.influenceMapEvaluator.WorkspaceEvaluationEntries[0].Position} with weight {___unit.BehaviorTree.influenceMapEvaluator.WorkspaceEvaluationEntries[0].GetHighestAccumulator()}");
                ModInit.modLog.LogTrace($"[MoveTowardsHighestPriorityMoveCandidateNode Tick] Moving towards highest eval'd position: Actor {___unit.DisplayName} eval'd highest weighted position as {___unit.BehaviorTree.influenceMapEvaluator.WorkspaceEvaluationEntries[0].Position} with weight {___unit.BehaviorTree.influenceMapEvaluator.WorkspaceEvaluationEntries[0].GetHighestAccumulator()}");
            }
        }
    }
}
