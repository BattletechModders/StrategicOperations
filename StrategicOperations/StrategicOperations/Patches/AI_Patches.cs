using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Abilifier;
using BattleTech;
using BattleTech.Framework;
using BattleTech.UI;
using Harmony;
using StrategicOperations.Framework;
using UnityEngine;
using static StrategicOperations.Framework.Classes;

namespace StrategicOperations.Patches
{
    public class AI_Patches
    {
        [HarmonyPatch(typeof(TurnDirector), "OnInitializeContractComplete")]
        public static class TurnDirector_OnInitializeContractComplete_AI
        {
            static bool Prepare() => ModInit.modSettings.AI_CommandAbilityAddChance > 0;

            public static void Postfix(TurnDirector __instance, MessageCenterMessage message)
            {
                Utils.CreateOrUpdateCustomTeam(); // can remove this after testing.

                var tgtTeam =
                    __instance.Combat.Teams.FirstOrDefault(x => x.GUID == "be77cadd-e245-4240-a93e-b99cc98902a5"); // TargetTeam is only team that gets cmdAbilities added?
                AI_Utils.GenerateAIStrategicAbilities(tgtTeam, __instance.Combat);
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
                if (ModState.AiCmd.active)
                {
                    __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                    return false;
                }
                var spawnVal = AI_Utils.EvaluateSpawn(___unit, out var abilitySpawn, out var vector1Spawn, out var vector2Spawn);
                var strafeVal = AI_Utils.EvaluateStrafing(___unit, out var abilityStrafe, out var vector1Strafe, out var vector2Strafe);

                if (spawnVal > ModInit.modSettings.AI_InvokeSpawnThreshold && spawnVal > strafeVal)
                {
                    var info = new AI_CmdInvocation(abilitySpawn, vector1Spawn, vector2Spawn, true);
                    ModState.AiCmd = info;
                    __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                    return false;
                }
                else if (strafeVal > ModInit.modSettings.AI_InvokeStrafeThreshold && strafeVal >= spawnVal)
                {
                    var info = new AI_CmdInvocation(abilityStrafe, vector1Strafe, vector2Strafe, true);
                    ModState.AiCmd = info;
                    __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                    return false;
                }
                return true;
            }
        }


        [HarmonyPatch(typeof(AITeam), "makeInvocationFromOrders")]
        public static class AITeam_makeInvocationFromOrders_patch
        {
            public static bool Prefix(AITeam __instance, AbstractActor unit, OrderInfo order, ref InvocationMessage __result)
            {
                if (!ModState.AiCmd.active) return true;
                ModState.popupActorResource =
                    AI_Utils.AssignRandomSpawnAsset(ModState.AiCmd.ability);

                ModInit.modLog.LogTrace($"AICMD DUMP: {ModState.AiCmd.active}, {ModState.AiCmd.vectorOne}, {ModState.AiCmd.vectorTwo}.");
                ModInit.modLog.LogTrace($"CMD Ability DUMP: {ModState.AiCmd.ability} { ModState.AiCmd.ability.Def.Id}, Combat is null? {ModState.AiCmd.ability.Combat != null}");

                ModState.AiCmd.ability.Activate(unit, ModState.AiCmd.vectorOne,
                    ModState.AiCmd.vectorTwo);
                ModInit.modLog.LogMessage(
                    $"activated {ModState.AiCmd.ability.Def.Description.Id} at pos {ModState.AiCmd.vectorOne.x}, {ModState.AiCmd.vectorOne.y}, {ModState.AiCmd.vectorOne.z} and {ModState.AiCmd.vectorTwo.x}, {ModState.AiCmd.vectorTwo.y}, {ModState.AiCmd.vectorTwo.z}, dist = {ModState.AiCmd.dist}");

                if (!unit.HasMovedThisRound)
                {
                    unit.BehaviorTree.IncreaseSprintHysteresisLevel();
                }
                __result = new ReserveActorInvocation(unit, ReserveActorAction.DONE, unit.Combat.TurnDirector.CurrentRound);
                ModState.AiCmd = new AI_CmdInvocation();
                return false;
                // invoke ability from modstate and then create/use a Brace/Reserve order.
                // probably need to make sure we're referencing the actual ability on the AI actor, and not a new instance? unless it already is...
            }
        }
    }
}
