using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using Abilifier.Patches;
using BattleTech;
using BattleTech.DataObjects;
using BattleTech.UI;
using CustAmmoCategories;
using CustomUnits;
using Harmony;
using IRTweaks.Modules.Combat;
using StrategicOperations.Framework;
using UnityEngine;
using static StrategicOperations.Framework.Classes;

namespace StrategicOperations.Patches
{
    public class AI_Patches
    {
        [HarmonyPatch(typeof(CanMoveAndShootWithoutOverheatingNode), "Tick")]
        public static class
            CanMoveAndShootWithoutOverheatingNode_Tick_Patch 
            // may need to be CanMoveAndShootWithoutOverheatingNode. was IsMovementAvailableForUnitNode
        {
           public static bool Prefix(CanMoveAndShootWithoutOverheatingNode __instance, ref BehaviorTreeResults __result)
            {
                if (!__instance.unit.Combat.TurnDirector.IsInterleaved) return true;

                if (__instance.unit.AreAnyWeaponsOutOfAmmo() || __instance.unit.SummaryArmorCurrent / __instance.unit.StartingArmor <= 0.6f)
                {
                    var resupplyAbility = __instance.unit.ComponentAbilities.FirstOrDefault(x =>
                            x.Def.Id == ModInit.modSettings.ResupplyConfig.ArmorSupplyAmmoDefId);
                    if (resupplyAbility != null)
                    {
                        var closestResupply = __instance.unit.GetClosestDetectedResupply();
                        if (closestResupply != null)
                        {
                            var distToResupply =
                                Vector3.Distance(closestResupply.CurrentPosition, __instance.unit.CurrentPosition);
                            if (distToResupply <= resupplyAbility.Def.IntParam2)
                            {
                                if (ModState.StrategicActorTargetInvocationCmds.ContainsKey(__instance.unit.GUID))
                                {
                                    if (ModState.StrategicActorTargetInvocationCmds[__instance.unit.GUID].active)
                                    {
                                        __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                                        return false;
                                    }

                                    var info = new StrategicActorTargetInvocation(resupplyAbility, closestResupply,
                                        true);
                                    ModState.StrategicActorTargetInvocationCmds[__instance.unit.GUID] = info;
                                    __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                                    return false; //was true
                                }
                                else
                                {
                                    var info = new StrategicActorTargetInvocation(resupplyAbility, closestResupply,
                                        true);
                                    ModState.StrategicActorTargetInvocationCmds.Add(__instance.unit.GUID, info);
                                    __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                                    return false; //was true
                                }
                            }
                        }
                    }
                }

                if (__instance.unit.IsAirlifted())
                {
                    ModInit.modLog?.Trace?.Write(
                        $"[CanMoveAndShootWithoutOverheatingNode] Actor {__instance.unit.DisplayName} is currently being airlifted. Doing nothing.");
                    __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                    return false;
                }

                var battleArmorAbility =
                    __instance.unit.ComponentAbilities.FirstOrDefault(x =>
                        x.Def.Id == ModInit.modSettings.BattleArmorMountAndSwarmID);
                if (battleArmorAbility != null)// && __instance.unit.canSwarm())
                {
                    if (battleArmorAbility.IsAvailable)
                    {
                        if (__instance.unit.IsSwarmingUnit())
                        {
                            ModInit.modLog?.Trace?.Write(
                                $"[CanMoveAndShootWithoutOverheatingNode] Actor {__instance.unit.DisplayName} is currently swarming. Doing nothing.");
                            //if currently swarming, dont do anything else.
                            __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                            return false;
                        }

                        var closestEnemy = __instance.unit.GetClosestDetectedSwarmTarget(__instance.unit.CurrentPosition);
                        if (closestEnemy != null)
                        {

                            var distance = Vector3.Distance(__instance.unit.CurrentPosition, closestEnemy.CurrentPosition);
                            var jumpdist = 0f;
                            if (__instance.unit is Mech mech)
                            {
                                jumpdist = mech.JumpDistance;
                                if (float.IsNaN(jumpdist)) jumpdist = 0f;
                            }

                            var maxRange = new List<float>()
                            {
                                __instance.unit.MaxWalkDistance,
                                __instance.unit.MaxSprintDistance,
                                jumpdist,
                                battleArmorAbility.Def.IntParam2
                            }.Max();

                            ModInit.modLog?.Trace?.Write(
                                $"[CanMoveAndShootWithoutOverheatingNode] Actor {__instance.unit.DisplayName} maxRange to be used is {maxRange}, largest of: MaxWalkDistance - {__instance.unit.MaxWalkDistance}, MaxSprintDistance - {__instance.unit.MaxSprintDistance}, JumpDistance - {jumpdist}, and Ability Override {battleArmorAbility.Def.IntParam2}");

                            if (__instance.unit.IsMountedUnit())
                            {
                                ModInit.modLog?.Trace?.Write(
                                    $"[CanMoveAndShootWithoutOverheatingNode] Actor {__instance.unit.DisplayName} is currently mounted. Evaluating range to nearest enemy.");
                                if (distance <= 1.25 * maxRange ||
                                    (!__instance.unit.canSwarm() && distance <= AIUtil.GetMaxWeaponRange(__instance.unit)))
                                {
                                    ModInit.modLog?.Trace?.Write(
                                        $"[CanMoveAndShootWithoutOverheatingNode] Actor {__instance.unit.DisplayName} is {distance} from nearest enemy, maxrange was {maxRange} * 1.25.");
                                    var carrier =
                                        __instance.unit.Combat.FindActorByGUID(ModState.PositionLockMount[__instance.unit.GUID]);
                                    if (ModState.StrategicActorTargetInvocationCmds.ContainsKey(__instance.unit.GUID))
                                    {
                                        if (ModState.StrategicActorTargetInvocationCmds[__instance.unit.GUID].active)
                                        {
                                            __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                                            return false;
                                        }

                                        var info = new StrategicActorTargetInvocation(battleArmorAbility, closestEnemy,
                                            true, true);
                                        ModState.StrategicActorTargetInvocationCmds[__instance.unit.GUID] = info;
                                        __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                                        return false; //was true
                                    }
                                    else
                                    {
                                        var info = new StrategicActorTargetInvocation(battleArmorAbility, closestEnemy,
                                            true, true);
                                        ModState.StrategicActorTargetInvocationCmds.Add(__instance.unit.GUID, info);
                                        __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                                        return false; //was true
                                    }
                                }

                                __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                                return false;
                            }

                            //if it isnt mounted, its on the ground and should try to swarm if it can.
                            if (distance <= maxRange && !(closestEnemy is TrooperSquad) && __instance.unit.canSwarm())
                            {
                                ModInit.modLog?.Trace?.Write(
                                    $"[CanMoveAndShootWithoutOverheatingNode] Actor {__instance.unit.DisplayName} is on the ground, trying to swarm at {distance} from nearest enemy, maxrange was {maxRange} * 1.25.");
                                if (ModState.StrategicActorTargetInvocationCmds.ContainsKey(__instance.unit.GUID))
                                {
                                    if (ModState.StrategicActorTargetInvocationCmds[__instance.unit.GUID].active)
                                    {
                                        __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                                        return false;
                                    }

                                    var info = new StrategicActorTargetInvocation(battleArmorAbility, closestEnemy,
                                        true);
                                    ModState.StrategicActorTargetInvocationCmds[__instance.unit.GUID] = info;
                                    __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                                    return false; //was true
                                }
                                else
                                {
                                    var info = new StrategicActorTargetInvocation(battleArmorAbility, closestEnemy,
                                        true);
                                    ModState.StrategicActorTargetInvocationCmds.Add(__instance.unit.GUID, info);
                                    __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                                    return false; //was true
                                }
                            }
                        }
                        else
                        {
                            ModInit.modLog?.Trace?.Write(
                                $"[CanMoveAndShootWithoutOverheatingNode] Some nerd running all LAMS or VTOLs, can't swarm anything; should just use vanilla tree.");
                        }
                    }
                }

                if (__instance.unit.HasSwarmingUnits())
                {
                    var deswarm = __instance.unit.GetDeswarmerAbilityForAI();
                    if (deswarm != null && deswarm?.Def?.Description?.Id != null)
                    {
                        if (ModState.AiDealWithBattleArmorCmds.ContainsKey(__instance.unit.GUID))
                        {
                            if (ModState.AiDealWithBattleArmorCmds[__instance.unit.GUID].active)
                            {
                                __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                                return false;
                            }

                            var info = new AI_DealWithBAInvocation(deswarm, __instance.unit, true);
                            ModState.AiDealWithBattleArmorCmds[__instance.unit.GUID] = info;
                            __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                            return false; //was true
                        }
                        else
                        {
                            var info = new AI_DealWithBAInvocation(deswarm, __instance.unit, true);
                            ModState.AiDealWithBattleArmorCmds.Add(__instance.unit.GUID, info);
                            __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                            return false; //was true
                        }
                    }
                }

                if (!ModState.AiCmds.ContainsKey(__instance.unit.GUID))
                {
                    var cmdAbility = __instance.unit.ComponentAbilities.FirstOrDefault(x => x.Def.Resource == AbilityDef.ResourceConsumed.CommandAbility);
                    if (cmdAbility != null)
                    {
                        if (cmdAbility.Def.specialRules == AbilityDef.SpecialRules.Strafe)
                        {
                            var strafeVal = AI_Utils.EvaluateStrafing(__instance.unit, out var abilityStrafe, out var vector1Strafe,
                                out var vector2Strafe, out var startUnit);

                            if (startUnit == null)
                            {
                                //__result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                                return true;
                            }

                            var skipStrafeChance = startUnit.GetAvoidStrafeChanceForTeam(); //should i save the thing?
                            //ModState.startUnitFromInvocation = startUnit;
                            ModInit.modLog?.Trace?.Write($"final AA value for {startUnit.team.DisplayName}: {skipStrafeChance}");
                            if (skipStrafeChance < ModInit.modSettings.strafeAAFailThreshold)
                            {
                                if (strafeVal >= ModInit.modSettings.AI_InvokeStrafeThreshold)
                                {
                                    var info = new AI_CmdInvocation(abilityStrafe, vector1Strafe, vector2Strafe, true);
                                    ModState.AiCmds.Add(__instance.unit.GUID, info);
                                    __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                                    return false;
                                }
                            }
                        }
                        else if (cmdAbility.Def.specialRules == AbilityDef.SpecialRules.SpawnTurret)
                        {
                            var spawnVal = AI_Utils.EvaluateSpawn(__instance.unit, out var abilitySpawn, out var vector1Spawn,
                                out var vector2Spawn);
                            if (spawnVal >= ModInit.modSettings.AI_InvokeSpawnThreshold)
                            {
                                var info = new AI_CmdInvocation(abilitySpawn, vector1Spawn, vector2Spawn, true);
                                ModState.AiCmds.Add(__instance.unit.GUID, info);
                                __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                                return false;
                            }
                        }
                    }
                }
                else
                {
                    if (ModState.AiCmds[__instance.unit.GUID].active)
                    {
                        __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                        return false;
                    }

                    if (ModState.AiCmds[__instance.unit.GUID].ability.Def.specialRules ==
                        AbilityDef.SpecialRules.Strafe)
                    {
                        var strafeVal = AI_Utils.EvaluateStrafing(__instance.unit, out var abilityStrafe, out var vector1Strafe,
                            out var vector2Strafe, out var startUnit);
                        if (startUnit == null)
                        {
                            //__result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                            return true;
                        }
                        var skipStrafeChance = startUnit.GetAvoidStrafeChanceForTeam();
                        ModInit.modLog?.Trace?.Write($"final AA value for {startUnit.team.DisplayName}: {skipStrafeChance}");
                        //                    ModState.startUnitFromInvocation = startUnit;
                        if (skipStrafeChance < ModInit.modSettings.strafeAAFailThreshold)
                        {
                            if (strafeVal > ModInit.modSettings.AI_InvokeStrafeThreshold)
                            {
                                var info = new AI_CmdInvocation(abilityStrafe, vector1Strafe, vector2Strafe, true);
                                ModState.AiCmds[__instance.unit.GUID] = info;
                                __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                                return false;
                            }
                        }
                    }
                    else if (ModState.AiCmds[__instance.unit.GUID].ability.Def.specialRules ==
                             AbilityDef.SpecialRules.SpawnTurret)
                    {
                        var spawnVal = AI_Utils.EvaluateSpawn(__instance.unit, out var abilitySpawn, out var vector1Spawn,
                            out var vector2Spawn);

                        if (spawnVal >= ModInit.modSettings.AI_InvokeSpawnThreshold)
                        {
                            var info = new AI_CmdInvocation(abilitySpawn, vector1Spawn, vector2Spawn, true);
                            ModState.AiCmds[__instance.unit.GUID] = info;
                            __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                            return false;
                        }
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(AITeam), "makeInvocationFromOrders")]
        public static class AITeam_makeInvocationFromOrders_patch
        {
            public static bool Prefix(AITeam __instance, AbstractActor unit, OrderInfo order,
                ref InvocationMessage __result)
            {
                if (unit.HasSwarmingUnits()){

                    if (unit is FakeVehicleMech && !unit.HasMovedThisRound && order.OrderType == OrderType.Move || order.OrderType == OrderType.JumpMove || order.OrderType == OrderType.SprintMove)
                    {
                        var ability = unit.GetDeswarmerAbilityForAI(true);
                        if (ability.IsAvailable && !ability.IsActive)
                        {
                            ability.Activate(unit, unit);
                            ModInit.modLog?.Info?.Write($"{unit.DisplayName} {unit.GUID} is vehicle being swarmed. Found movement order, activating erratic maneuvers ability.");
                            return true;
                        }
                    }

                    if (ModState.AiDealWithBattleArmorCmds.ContainsKey(unit.GUID))
                    {
                        ModState.AiDealWithBattleArmorCmds[unit.GUID].ability.Activate(unit, unit);
                        //     ModState.AiDealWithBattleArmorCmds[unit.GUID].targetActor);

                        ModInit.modLog?.Info?.Write(
                            $"activated {ModState.AiDealWithBattleArmorCmds[unit.GUID].ability.Def.Description.Id} on actor {unit.DisplayName} {unit.GUID}");

                        if (!unit.HasMovedThisRound)
                        {
                            unit.BehaviorTree.IncreaseSprintHysteresisLevel();
                        }

                        __result = new ReserveActorInvocation(unit, ReserveActorAction.DONE,
                            unit.Combat.TurnDirector.CurrentRound);
                        ModState.AiDealWithBattleArmorCmds.Remove(unit.GUID);
                        return false;
                    }
                }

                if (unit.IsMountedUnit() && !ModState.StrategicActorTargetInvocationCmds.ContainsKey(unit.GUID))
                {
                    if (unit.CanDeferUnit)
                    {
                        __result = new ReserveActorInvocation(unit, ReserveActorAction.DEFER, unit.Combat.TurnDirector.CurrentRound);
                        return false; // changed to defer to get BA to reserve down?
                    }
                    __result = new ReserveActorInvocation(unit, ReserveActorAction.DONE, unit.Combat.TurnDirector.CurrentRound);
                    return false; // changed to defer to get BA to reserve down?
                }

                if (unit.IsSwarmingUnit() && !unit.HasFiredThisRound)
                {
                    var target = unit.Combat.FindActorByGUID(ModState.PositionLockSwarm[unit.GUID]);
                    if (unit.IsFlaggedForDeath || unit.IsDead)
                    {
                        unit.HandleDeath(target.GUID);
                        return false;
                    }

                    ModInit.modLog?.Info?.Write(
                        $"[AITeam.makeInvocationFromOrders] Actor {unit.DisplayName} has active swarm attack on {target.DisplayName}");
                    if (unit.GetAbilityUsedFiring())
                    {
                        foreach (var weapon in unit.Weapons)
                        {
                            weapon.DisableWeapon();
                        }
                    }
                    else
                    {
                        foreach (var weapon in unit.Weapons)
                        {
                            weapon.EnableWeapon();
                        }
                    }

                    var weps = unit.Weapons.Where(x => x.IsEnabled && x.HasAmmo).ToList();

                    var loc = ModState.BADamageTrackers[unit.GUID].BA_MountedLocations.Values.GetRandomElement();
                    //var attackStackSequence = new AttackStackSequence(unit, target, unit.CurrentPosition, unit.CurrentRotation, weps, MeleeAttackType.NotSet, loc, -1);
                    //unit.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(attackStackSequence));
                    var vent = unit.HasVentCoolantAbility && unit.CanVentCoolant;
                    __result = new AttackInvocation(unit, target, weps, MeleeAttackType.NotSet, loc)
                    {
                        ventHeatBeforeAttack = vent
                    }; // making a regular attack invocation here, instead of stacksequence + reserve

                    //if (!unit.HasMovedThisRound)
                    //{
                    //    unit.BehaviorTree.IncreaseSprintHysteresisLevel();
                    //}
                    //__result = new ReserveActorInvocation(unit, ReserveActorAction.DONE, unit.Combat.TurnDirector.CurrentRound);
                    return false;
                }

                if (ModState.StrategicActorTargetInvocationCmds.ContainsKey(unit.GUID))
                {
                    if (ModState.StrategicActorTargetInvocationCmds[unit.GUID].active)
                    {
                        ModInit.modLog?.Debug?.Write(
                            $"BA AI Swarm/Mount Ability DUMP: {ModState.StrategicActorTargetInvocationCmds[unit.GUID].active}, {ModState.StrategicActorTargetInvocationCmds[unit.GUID].targetActor.DisplayName}.");
                        ModInit.modLog?.Debug?.Write(
                            $"BA AI Swarm/Mount Ability DUMP: {ModState.StrategicActorTargetInvocationCmds[unit.GUID].ability} {ModState.StrategicActorTargetInvocationCmds[unit.GUID].ability.Def.Id}, Combat is not null? {ModState.StrategicActorTargetInvocationCmds[unit.GUID].ability.Combat != null}");

                        if (unit.IsMountedUnit())
                        {
                            var carrier = unit.Combat.FindActorByGUID(ModState.PositionLockMount[unit.GUID]);
                            ModState.StrategicActorTargetInvocationCmds[unit.GUID].ability.Activate(unit,
                                    carrier);
                        }

                        ModInit.modLog?.Info?.Write(
                            $"[makeInvocationFromOrders] activated {ModState.StrategicActorTargetInvocationCmds[unit.GUID].ability.Def.Description.Id} on actor {ModState.StrategicActorTargetInvocationCmds[unit.GUID].targetActor.DisplayName} {ModState.StrategicActorTargetInvocationCmds[unit.GUID].targetActor.GUID}");
                        ModInit.modLog?.Trace?.Write(
                            $"[makeInvocationFromOrders] checking if swarm success {unit.IsSwarmingUnit()}, and has fired this round {unit.HasFiredThisRound}");


                        __result = new StrategicMovementInvocation(unit, true, ModState.StrategicActorTargetInvocationCmds[unit.GUID].targetActor, false, true);

                        if (false)
                        {
                            if (unit.IsSwarmingUnit() && ModInit.modSettings.AttackOnSwarmSuccess &&
                                !unit.HasFiredThisRound) // maybe move this whole shit do the Strategic Move invocation, check for success, and make the first attack invocation there, at OnComplete?
                            {
                                ModInit.modLog?.Trace?.Write(
                                    $"[makeInvocationFromOrders] - found freshly swarmed unit; trying to make attack invocation for same round. Fingies crossed!");
                                var target = unit.Combat.FindActorByGUID(ModState.PositionLockSwarm[unit.GUID]);
                                foreach (var weapon in unit.Weapons)
                                {
                                    weapon.EnableWeapon();
                                }

                                var weps = unit.Weapons.Where(x => x.IsEnabled && x.HasAmmo).ToList();

                                var loc = ModState.BADamageTrackers[unit.GUID].BA_MountedLocations.Values
                                    .GetRandomElement();
                                //var attackStackSequence = new AttackStackSequence(unit, target, unit.CurrentPosition, unit.CurrentRotation, weps, MeleeAttackType.NotSet, loc, -1);
                                //unit.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(attackStackSequence));
                                var vent = unit.HasVentCoolantAbility && unit.CanVentCoolant;
                                __result = new AttackInvocation(unit, target, weps, MeleeAttackType.NotSet, loc)
                                {
                                    ventHeatBeforeAttack = vent
                                }; // making a regular attack invocation here, instead of stacksequence + reserve
                            }

                            else
                            {
                                if (!unit.HasMovedThisRound)
                                {
                                    unit.BehaviorTree.IncreaseSprintHysteresisLevel();
                                }

                                __result = new ReserveActorInvocation(unit, ReserveActorAction.DONE,
                                    unit.Combat.TurnDirector.CurrentRound);
                            }
                        }

                        ModState.StrategicActorTargetInvocationCmds.Remove(unit.GUID);
                        return false;
                    }
                }

                if (!ModState.AiCmds.ContainsKey(unit.GUID)) return true;
                if (!ModState.AiCmds[unit.GUID].active) return true;
                var quid = ModState.AiCmds[unit.GUID].ability
                    .Generate2PtCMDQuasiGUID(unit.GUID, ModState.AiCmds[unit.GUID].vectorOne,
                        ModState.AiCmds[unit.GUID].vectorTwo);
                if (ModState.StoredCmdParams.ContainsKey(quid)) return true;
                //ModState.PopupActorResource = AI_Utils.AssignRandomSpawnAsset(ModState.AiCmds[unit.GUID].ability, unit.team.FactionValue.Name, out var waves);
                //ModState.StrafeWaves = waves;
                var newParams = new CmdInvocationParams();
                newParams.ActorResource  = AI_Utils.AssignRandomSpawnAsset(ModState.AiCmds[unit.GUID].ability, unit.team.FactionValue.Name,
                    out var waves);
                newParams.StrafeWaves = waves;

                if (!string.IsNullOrEmpty(ModState.AiCmds[unit.GUID].ability.Def.getAbilityDefExtension()
                        .CMDPilotOverride))
                {
                    newParams.PilotOverride = ModState.AiCmds[unit.GUID].ability.Def.getAbilityDefExtension()
                        .CMDPilotOverride;
                }

                ModState.StoredCmdParams.Add(quid, newParams);
                //assign waves here if needed
                ModInit.modLog?.Debug?.Write(
                    $"AICMD DUMP: {ModState.AiCmds[unit.GUID].active}, {ModState.AiCmds[unit.GUID].vectorOne}, {ModState.AiCmds[unit.GUID].vectorTwo}.");
                ModInit.modLog?.Debug?.Write(
                    $"CMD Ability DUMP: {ModState.AiCmds[unit.GUID].ability} {ModState.AiCmds[unit.GUID].ability.Def.Id}, Combat is not null? {ModState.AiCmds[unit.GUID].ability.Combat != null}");

                ModState.AiCmds[unit.GUID].ability.Activate(unit, ModState.AiCmds[unit.GUID].vectorOne,
                    ModState.AiCmds[unit.GUID].vectorTwo);
                ModInit.modLog?.Info?.Write(
                    $"activated {ModState.AiCmds[unit.GUID].ability.Def.Description.Id} at pos {ModState.AiCmds[unit.GUID].vectorOne.x}, {ModState.AiCmds[unit.GUID].vectorOne.y}, {ModState.AiCmds[unit.GUID].vectorOne.z} and {ModState.AiCmds[unit.GUID].vectorTwo.x}, {ModState.AiCmds[unit.GUID].vectorTwo.y}, {ModState.AiCmds[unit.GUID].vectorTwo.z}, dist = {ModState.AiCmds[unit.GUID].dist}");

                if (!unit.HasMovedThisRound)
                {
                    unit.BehaviorTree.IncreaseSprintHysteresisLevel();
                }

                __result = new ReserveActorInvocation(unit, ReserveActorAction.DONE,
                    unit.Combat.TurnDirector.CurrentRound);
                ModState.AiCmds.Remove(unit.GUID); //somehow spawned BA doesn't always reserve on correct round?
                return false;
                // invoke ability from modstate and then create/use a Brace/Reserve order.

            }
        }

        [HarmonyPatch(typeof(AIUtil),
            "UnitHasVisibilityToTargetFromCurrentPosition")] //need to add optionally depends on lowvis to make this work for RT
        public static class AIUtil_UnitHasVisibilityToTargetFromCurrentPosition
        {
            public static void Postfix(AbstractActor attacker, ICombatant target, ref bool __result)
            {
                if (target is AbstractActor actor)
                {
                    if (actor.IsSwarmingUnit() || actor.IsMountedUnit()) // i could force visibility to zero for BA? unsure what i want to do here since i dont think the AI is smart enough to directly target the building. have to see how it plays/maybe give garrisoned BA some DR or something.
                    {
                        ModInit.modLog?.Debug?.Write(
                            $"[AIUtil.UnitHasVisibilityToTargetFromCurrentPosition] DUMP: Target {target.DisplayName} is either mounted or swarming, forcing AI visibility to zero for this node.");
                        __result = false;
                    }
                }
            }
        }
        
        [HarmonyPatch()]
        public static class Mech_CanEngageTarget
        {
            public static MethodBase TargetMethod()
            {
                var methods = AccessTools.GetDeclaredMethods(typeof(Mech));
                foreach (MethodInfo info in methods)
                {
                    if (info.Name == "CanEngageTarget")
                    {
                        var paramInfo = info.GetParameters();
                        
                        if (paramInfo.Length == 2)
                        {
                            return info;
                        }
                    }
                }
                return null;
            }
            public static void Postfix(Mech __instance, ICombatant target, ref string debugMsg, ref bool __result)
            {
                //debugMsg = default(string);
                if (target is AbstractActor actor)
                {
                    if (actor.IsSwarmingUnit() || actor.IsMountedUnit() || actor.HasSwarmingUnits()) // added swarming check so AI doesnt shoot at units being swarmed// same as UnitHasVisibilityToTargetFromCurrentPosition; let the AI shoot at garrison direclty?
                    {
                        __result = false;
                    }
                }
            }
        }
    }
}
