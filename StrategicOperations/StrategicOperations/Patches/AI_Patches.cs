using System.Linq;
using System.Reflection;
using BattleTech;
using BattleTech.UI;
using Harmony;
using StrategicOperations.Framework;
using UnityEngine;
using static StrategicOperations.Framework.Classes;

namespace StrategicOperations.Patches
{
    public class AI_Patches
    {
        [HarmonyPatch(typeof(Team), "AddUnit")]
        public static class Team_AddUnit_AI
        {
            static bool Prepare() => ModInit.modSettings.commandAbilities_AI.Any(x=>x.AddChance > 0f);

            public static void Postfix(Team __instance, AbstractActor unit)
            {
                if (__instance.Combat.TurnDirector.CurrentRound > 1) return; // don't give abilities to reinforcements?
                if(__instance.GUID != "be77cadd-e245-4240-a93e-b99cc98902a5") return; // TargetTeam is only team that gets cmdAbilities added?
                AI_Utils.GenerateAIStrategicAbilities(unit);
            }
        }

        // this is just for testing AI evaluator. sorta. hopefully.
                [HarmonyPatch(typeof(SelectionStateCommandTargetTwoPoints), "ProcessPressedButton")]
        public static class SelectionStateCommandTargetTwoPoints_ProcessPressedButton
        {
            static bool Prepare() => ModInit.modSettings.DEVTEST_AIPOS;

            public static bool Prefix(SelectionStateCommandTargetTwoPoints __instance, string button, ref bool __result)
            {
                if (button == "BTN_FireConfirm")
                {
                    if (__instance.FromButton.Ability.Def.specialRules == AbilityDef.SpecialRules.Strafe)
                    {
                        var dmg = AI_Utils.EvaluateStrafing(__instance.SelectedActor, out Ability ability,
                            out Vector3 start,
                            out Vector3 end);
                        if (dmg > 1)
                        {
                            ModState.popupActorResource =
                                AI_Utils.AssignRandomSpawnAsset(__instance.FromButton.Ability);
                            __instance.FromButton.ActivateCommandAbility(__instance.SelectedActor.team.GUID, start,
                                end);
                            ModInit.modLog.LogMessage(
                                $"activated Strafe at pos {start.x}, {start.y}, {start.z} and {end.x}, {end.y},{end.z}");
                            __result = true;
                            return false;
                        }

                        ModInit.modLog.LogMessage(
                            $"dmg <1");
                        __result = true;
                        return false;
                    }

                    if (__instance.FromButton.Ability.Def.specialRules == AbilityDef.SpecialRules.SpawnTurret)
                    {
                        var tgts = AI_Utils.EvaluateSpawn(__instance.SelectedActor, out Ability ability,
                            out Vector3 start,
                            out Vector3 end);
                        if (tgts > 1)
                        {
                            ModState.popupActorResource =
                                AI_Utils.AssignRandomSpawnAsset(__instance.FromButton.Ability);
                            __instance.FromButton.ActivateCommandAbility(__instance.SelectedActor.team.GUID, start,
                                end);
                            ModInit.modLog.LogMessage(
                                $"activated SpawnTurret at pos {start.x}, {start.y}, {start.z} and {end.x}, {end.y},{end.z}");
                            __result = true;
                            return false;
                        }

                        ModInit.modLog.LogMessage(
                            $"dmg <1");
                        __result = true;
                        return false;
                    }
                }
                ModInit.modLog.LogMessage(
                    $"button fucked up");
                __result = true;
                return false;
            }
        }

        [HarmonyPatch]
        public static class CanMoveAndShootWithoutOverheatingNode_Tick_Patch  // may need to be CanMoveAndShootWithoutOverheatingNode. was IsMovementAvailableForUnitNode
        {
            public static MethodBase TargetMethod()
            {
                var type = AccessTools.TypeByName("CanMoveAndShootWithoutOverheatingNode");
                return AccessTools.Method(type, "Tick");
            }

            public static bool Prefix(ref BehaviorTreeResults __result, string ___name,
                AbstractActor ___unit)
            {
                if (!___unit.Combat.TurnDirector.IsInterleaved) return true;
                if (ModState.AiCmds.ContainsKey(___unit.GUID))
                {
                    if (ModState.AiCmds[___unit.GUID].active)
                    {
                        __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                        return false;
                    }

                    var spawnVal = AI_Utils.EvaluateSpawn(___unit, out var abilitySpawn, out var vector1Spawn,
                        out var vector2Spawn);
                    var strafeVal = AI_Utils.EvaluateStrafing(___unit, out var abilityStrafe, out var vector1Strafe,
                        out var vector2Strafe);

                    if (spawnVal > ModInit.modSettings.AI_InvokeSpawnThreshold && spawnVal > strafeVal)
                    {
                        var info = new AI_CmdInvocation(abilitySpawn, vector1Spawn, vector2Spawn, true);
                        ModState.AiCmds[___unit.GUID] = info;
                        __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                        return false;
                    }
                    else if (strafeVal > ModInit.modSettings.AI_InvokeStrafeThreshold && strafeVal >= spawnVal)
                    {
                        var info = new AI_CmdInvocation(abilityStrafe, vector1Strafe, vector2Strafe, true);
                        ModState.AiCmds[___unit.GUID] = info;
                        __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                        return false;
                    }
                }
                else
                {

                    var spawnVal = AI_Utils.EvaluateSpawn(___unit, out var abilitySpawn, out var vector1Spawn,
                        out var vector2Spawn);
                    var strafeVal = AI_Utils.EvaluateStrafing(___unit, out var abilityStrafe, out var vector1Strafe,
                        out var vector2Strafe);

                    if (strafeVal > spawnVal) goto strafe;
                    if (spawnVal >= ModInit.modSettings.AI_InvokeSpawnThreshold)
                    {
                        var info = new AI_CmdInvocation(abilitySpawn, vector1Spawn, vector2Spawn, true);
                        ModState.AiCmds.Add(___unit.GUID, info);
                        __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                        return false;
                    }
                    strafe:
                    if (strafeVal >= ModInit.modSettings.AI_InvokeStrafeThreshold)
                    {
                        var info = new AI_CmdInvocation(abilityStrafe, vector1Strafe, vector2Strafe, true);
                        ModState.AiCmds.Add(___unit.GUID, info);
                        __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                        return false;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(AITeam), "makeInvocationFromOrders")]
        public static class AITeam_makeInvocationFromOrders_patch
        {
            public static bool Prefix(AITeam __instance, AbstractActor unit, OrderInfo order, ref InvocationMessage __result)
            {
                if (!ModState.AiCmds.ContainsKey(unit.GUID)) return true;
                else if (!ModState.AiCmds[unit.GUID].active) return true;

                ModState.popupActorResource =
                    AI_Utils.AssignRandomSpawnAsset(ModState.AiCmds[unit.GUID].ability);

                ModInit.modLog.LogTrace($"AICMD DUMP: {ModState.AiCmds[unit.GUID].active}, {ModState.AiCmds[unit.GUID].vectorOne}, {ModState.AiCmds[unit.GUID].vectorTwo}.");
                ModInit.modLog.LogTrace($"CMD Ability DUMP: {ModState.AiCmds[unit.GUID].ability} { ModState.AiCmds[unit.GUID].ability.Def.Id}, Combat is not null? {ModState.AiCmds[unit.GUID].ability.Combat != null}");

                ModState.AiCmds[unit.GUID].ability.Activate(unit, ModState.AiCmds[unit.GUID].vectorOne,
                    ModState.AiCmds[unit.GUID].vectorTwo);
                ModInit.modLog.LogMessage(
                    $"activated {ModState.AiCmds[unit.GUID].ability.Def.Description.Id} at pos {ModState.AiCmds[unit.GUID].vectorOne.x}, {ModState.AiCmds[unit.GUID].vectorOne.y}, {ModState.AiCmds[unit.GUID].vectorOne.z} and {ModState.AiCmds[unit.GUID].vectorTwo.x}, {ModState.AiCmds[unit.GUID].vectorTwo.y}, {ModState.AiCmds[unit.GUID].vectorTwo.z}, dist = {ModState.AiCmds[unit.GUID].dist}");

                if (!unit.HasMovedThisRound)
                {
                    unit.BehaviorTree.IncreaseSprintHysteresisLevel();
                }
                __result = new ReserveActorInvocation(unit, ReserveActorAction.DONE, unit.Combat.TurnDirector.CurrentRound);
                ModState.AiCmds.Remove(unit.GUID);
                return false;
                // invoke ability from modstate and then create/use a Brace/Reserve order.
                // probably need to make sure we're referencing the actual ability on the AI actor, and not a new instance? unless it already is...
            }
        }
    }
}
