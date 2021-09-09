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
    public class AI_Patches
    {
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

        [HarmonyPatch(typeof(Team), "AddUnit", new Type[] {typeof(AbstractActor)})]
        public static class Team_AddUnit_Patch
        {
            public static void Postfix(Team __instance, AbstractActor unit)
            {
                if (__instance.IsLocalPlayer) return;

                if (unit is Mech mech)
                {
                    if (mech.EncounterTags.Contains("SpawnedFromAbility")) return;
                }
                AI_Utils.GenerateAIStrategicAbilities(unit);
                
            }
        }

        [HarmonyPatch(typeof(PreferFarthestAwayFromClosestHostilePositionFactor), "EvaluateInfluenceMapFactorAtPosition",
        new Type[] {typeof(AbstractActor), typeof(Vector3), typeof(float), typeof(MoveType), typeof(PathNode) })]
        public static class PreferFarthestAwayFromClosestHostilePositionFactor_EvaluateInfluenceMapFactorAtPosition_BattleArmor
        {
            public static bool Prefix(PreferFarthestAwayFromClosestHostilePositionFactor __instance, AbstractActor unit, Vector3 position, float angle, MoveType moveType_unused, PathNode pathNode_unused, ref float __result)
            {
                if (unit.HasMountedUnits() ||
                    unit.ComponentAbilities.Any(x => x.Def.Id == ModInit.modSettings.BattleArmorMountID))
                {
                    var result = 9001 * (1/AIUtil.DistanceToClosestEnemy(unit, position));
                    ModInit.modLog.LogTrace($"[PreferFarthestAwayFromClosestHostilePositionFactor] Actor {unit.DisplayName} evaluating position {position}, should return {result}");
                    __result = result;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(PreferHigherExpectedDamageToHostileFactor), "EvaluateInfluenceMapFactorAtPositionWithHostile",
            new Type[] { typeof(AbstractActor), typeof(Vector3), typeof(float), typeof(MoveType), typeof(ICombatant) })]
        public static class PreferHigherExpectedDamageToHostileFactor_EvaluateInfluenceMapFactorAtPosition_BattleArmor
        {
            public static bool Prefix(PreferFarthestAwayFromClosestHostilePositionFactor __instance, AbstractActor unit, Vector3 position, float angle, MoveType moveType, ICombatant hostileUnit, ref float __result)
            {
                if (unit.HasMountedUnits() ||
                    unit.ComponentAbilities.Any(x => x.Def.Id == ModInit.modSettings.BattleArmorMountID))
                {
                    var result = 9001 * (1 / AIUtil.DistanceToClosestEnemy(unit, position));
                    ModInit.modLog.LogTrace($"[PreferFarthestAwayFromClosestHostilePositionFactor] Actor {unit.DisplayName} evaluating position {position}, should return {result}");
                    __result = result;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(PreferNoCloserThanMinDistToHostileFactor), "EvaluateInfluenceMapFactorAtPositionWithHostile",
            new Type[] { typeof(AbstractActor), typeof(Vector3), typeof(float), typeof(MoveType), typeof(ICombatant) })]
        public static class PreferNoCloserThanMinDistToHostileFactor_EvaluateInfluenceMapFactorAtPosition_BattleArmor
        {
            public static bool Prefix(PreferNoCloserThanMinDistToHostileFactor __instance, AbstractActor unit, Vector3 position, float angle, MoveType moveType, ICombatant hostileUnit, ref float __result)
            {
                if (unit.HasMountedUnits() ||
                    unit.ComponentAbilities.Any(x => x.Def.Id == ModInit.modSettings.BattleArmorMountID))
                {
                    var result = 9001 * (1 / AIUtil.DistanceToClosestEnemy(unit, position));
                    ModInit.modLog.LogTrace($"[PreferNoCloserThanMinDistToHostileFactor] Actor {unit.DisplayName} evaluating position {position}, should return {result}");
                    __result = result;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(PreferOptimalDistanceToHostileFactor), "EvaluateInfluenceMapFactorAtPositionWithHostile",
            new Type[] { typeof(AbstractActor), typeof(Vector3), typeof(float), typeof(MoveType), typeof(ICombatant) })]
        public static class PreferOptimalDistanceToHostileFactor_EvaluateInfluenceMapFactorAtPosition_BattleArmor
        {
            public static bool Prefix(PreferOptimalDistanceToHostileFactor __instance, AbstractActor unit, Vector3 position, float angle, MoveType moveType, ICombatant hostileUnit, ref float __result)
            {
                if (unit.HasMountedUnits() ||
                    unit.ComponentAbilities.Any(x => x.Def.Id == ModInit.modSettings.BattleArmorMountID))
                {
                    var result = 9001 * (1 / AIUtil.DistanceToClosestEnemy(unit, position));
                    __result = result;
                    ModInit.modLog.LogTrace($"[PreferOptimalDistanceToHostileFactor] Actor {unit.DisplayName} evaluating position {position}, should return {result}");
                    return false;
                }
                return true;
            }
        }

        //try IsMovementAvailable again, just for BA and BA carrying units?


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
                var battleArmorAbility =
                    ___unit.ComponentAbilities.FirstOrDefault(x => x.Def.Id == ModInit.modSettings.BattleArmorMountID);
                if (battleArmorAbility != null)
                {
                    if (battleArmorAbility.IsAvailable)
                    {
                        if (___unit.IsSwarmingUnit())
                        {
                            //if currently swarming, dont do anything else.
                            __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                            return false;
                        }

                        var closestEnemy = ___unit.GetClosestDetectedEnemy(___unit.CurrentPosition);

                        var distance = Vector3.Distance(___unit.CurrentPosition, closestEnemy.CurrentPosition);
                        var jumpdist = 0f;
                        if (___unit is Mech mech)
                        {
                            jumpdist = mech.JumpDistance;
                        }

                        var maxRange = new List<float>()
                        {
                            ___unit.MaxWalkDistance,
                            ___unit.MaxSprintDistance,
                            jumpdist,
                            battleArmorAbility.Def.IntParam2
                        }.Max();

                        if (___unit.IsMountedUnit())
                        {
                            if (distance <= 1.25 * maxRange)
                            {
                                var carrier = ___unit.Combat.FindActorByGUID(ModState.PositionLockMount[___unit.GUID]);
                                if (ModState.AiBattleArmorAbilityCmds.ContainsKey(___unit.GUID))
                                {
                                    if (ModState.AiBattleArmorAbilityCmds[___unit.GUID].active)
                                    {
                                        __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                                        return false;
                                    }

                                    var info = new BA_MountOrSwarmInvocation(battleArmorAbility, carrier, true);
                                    ModState.AiBattleArmorAbilityCmds[___unit.GUID] = info;
                                    __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                                    return false; //was true
                                }
                                else
                                {
                                    var info = new BA_MountOrSwarmInvocation(battleArmorAbility, carrier, true);
                                    ModState.AiBattleArmorAbilityCmds.Add(___unit.GUID, info);
                                    __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                                    return false; //was true
                                }
                            }

                            __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                            return false;
                        }

                        //if it isnt mounted, its on the ground and should try to swarm.
                        if (distance <= maxRange)
                        {
                            if (ModState.AiBattleArmorAbilityCmds.ContainsKey(___unit.GUID))
                            {
                                if (ModState.AiBattleArmorAbilityCmds[___unit.GUID].active)
                                {
                                    __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                                    return false;
                                }

                                var info = new BA_MountOrSwarmInvocation(battleArmorAbility, closestEnemy, true);
                                ModState.AiBattleArmorAbilityCmds[___unit.GUID] = info;
                                __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                                return false; //was true
                            }
                            else
                            {
                                var info = new BA_MountOrSwarmInvocation(battleArmorAbility, closestEnemy, true);
                                ModState.AiBattleArmorAbilityCmds.Add(___unit.GUID, info);
                                __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                                return false; //was true
                            }
                        }
                    }
                }

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
                if (unit.IsMountedUnit() && !ModState.AiBattleArmorAbilityCmds.ContainsKey(unit.GUID))
                {
                    __result = new ReserveActorInvocation(unit, ReserveActorAction.DONE,
                        unit.Combat.TurnDirector.CurrentRound);
                    return false;
                }

                if (unit.IsSwarmingUnit())
                {
                    var target = unit.Combat.FindActorByGUID(ModState.PositionLockSwarm[unit.GUID]);
                    ModInit.modLog.LogTrace($"[AITeam.makeInvocationFromOrders] Actor {unit.DisplayName} has active swarm attack on {target.DisplayName}");

                    var weps = unit.Weapons.Where(x => x.IsEnabled && x.HasAmmo).ToList();

                    var loc = ModState.BADamageTrackers[unit.GUID].BA_MountedLocations.Values.GetRandomElement();
                    var attackStackSequence = new AttackStackSequence(unit, target, unit.CurrentPosition,
                        unit.CurrentRotation, weps, MeleeAttackType.NotSet, loc, -1);
                    unit.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(attackStackSequence));

                    if (!unit.HasMovedThisRound)
                    {
                        unit.BehaviorTree.IncreaseSprintHysteresisLevel();
                    }

                    __result = new ReserveActorInvocation(unit, ReserveActorAction.DONE,
                        unit.Combat.TurnDirector.CurrentRound);
                    return false;
                }
                if (ModState.AiBattleArmorAbilityCmds.ContainsKey(unit.GUID))
                {
                    if (ModState.AiBattleArmorAbilityCmds[unit.GUID].active)
                    {
                        ModInit.modLog.LogTrace(
                            $"BA AI Swarm/Mount Ability DUMP: {ModState.AiBattleArmorAbilityCmds[unit.GUID].active}, {ModState.AiBattleArmorAbilityCmds[unit.GUID].targetActor.DisplayName}.");
                        ModInit.modLog.LogTrace(
                            $"BA AI Swarm/Mount Ability DUMP: {ModState.AiBattleArmorAbilityCmds[unit.GUID].ability} {ModState.AiBattleArmorAbilityCmds[unit.GUID].ability.Def.Id}, Combat is not null? {ModState.AiBattleArmorAbilityCmds[unit.GUID].ability.Combat != null}");

                        ModState.AiBattleArmorAbilityCmds[unit.GUID].ability.Activate(unit,
                            ModState.AiBattleArmorAbilityCmds[unit.GUID].targetActor);
                        ModInit.modLog.LogMessage(
                            $"activated {ModState.AiBattleArmorAbilityCmds[unit.GUID].ability.Def.Description.Id} on actor {ModState.AiBattleArmorAbilityCmds[unit.GUID].targetActor.DisplayName} {ModState.AiBattleArmorAbilityCmds[unit.GUID].targetActor.GUID}");

                        if (!unit.HasMovedThisRound)
                        {
                            unit.BehaviorTree.IncreaseSprintHysteresisLevel();
                        }

                        __result = new ReserveActorInvocation(unit, ReserveActorAction.DONE,
                            unit.Combat.TurnDirector.CurrentRound);
                        ModState.AiBattleArmorAbilityCmds.Remove(unit.GUID);
                        return false;
                    }
                }

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
