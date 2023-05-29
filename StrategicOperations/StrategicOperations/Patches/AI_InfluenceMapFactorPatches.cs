using System;
using BattleTech;
using CustomUnits;
using StrategicOperations.Framework;
using UnityEngine;

namespace StrategicOperations.Patches
{
    public class AI_InfluenceMapFactorPatches
    {
        [HarmonyPatch(typeof(PreferFarthestAwayFromClosestHostilePositionFactor),
            "EvaluateInfluenceMapFactorAtPosition",
            new Type[] { typeof(AbstractActor), typeof(Vector3), typeof(float), typeof(MoveType), typeof(PathNode) })]
        public static class
            PreferFarthestAwayFromClosestHostilePositionFactor_EvaluateInfluenceMapFactorAtPosition_BattleArmor
        {
            public static void Prefix(ref bool __runOriginal, PreferFarthestAwayFromClosestHostilePositionFactor __instance, AbstractActor unit,
                Vector3 position, float angle, MoveType moveType_unused, PathNode pathNode_unused, ref float __result)
            {
                if (!__runOriginal) return;
                if (unit.HasMountedUnits() || (unit.CanSwarm() && unit is TrooperSquad))
                {
                    var result = 9001 * (1 / unit.DistanceToClosestDetectedEnemy(position));
                    ModInit.modLog?.Debug?.Write(
                        $"[PreferFarthestAwayFromClosestHostilePositionFactor] Actor {unit.DisplayName} evaluating position {position}, should return {result}");
                    __result = result;
                    __runOriginal = false;
                    return;
                }

                if (unit.AreAnyWeaponsOutOfAmmo() || unit.SummaryArmorCurrent / unit.StartingArmor <= 0.6f)
                {
                    var distToResupply = unit.GetDistanceToClosestDetectedResupply(position);
                    if (distToResupply <= -5f)
                    {
                        __runOriginal = true;
                        return;
                    }
                    var result = 9001 * (1 / distToResupply);
                    __result = result;
                    __runOriginal = false;
                    return;
                }
                __runOriginal = true;
                return;
            }
        }

        [HarmonyPatch(typeof(PreferLowerMovementFactor), "EvaluateInfluenceMapFactorAtPosition",
            new Type[] { typeof(AbstractActor), typeof(Vector3), typeof(float), typeof(MoveType), typeof(PathNode) })]
        public static class PreferLowerMovementFactor_EvaluateInfluenceMapFactorAtPosition_BattleArmor
        {
            public static void Prefix(ref bool __runOriginal, PreferFarthestAwayFromClosestHostilePositionFactor __instance, AbstractActor unit,
                Vector3 position, float angle, MoveType moveType_unused, PathNode pathNode_unused, ref float __result)
            {
                if (!__runOriginal) return;
                if (unit.HasMountedUnits() || (unit.CanSwarm() && unit is TrooperSquad))
                {
                    var result = 9001 * (1 / unit.DistanceToClosestDetectedEnemy(position));
                    ModInit.modLog?.Debug?.Write(
                        $"[PreferLowerMovementFactor] Actor {unit.DisplayName} evaluating position {position}, should return {result}");
                    __result = result;
                    __runOriginal = false;
                    return;
                }
                if (unit.AreAnyWeaponsOutOfAmmo() || unit.SummaryArmorCurrent / unit.StartingArmor <= 0.6f)
                {
                    var distToResupply = unit.GetDistanceToClosestDetectedResupply(position);
                    if (distToResupply <= -5f)
                    {
                        __runOriginal = true;
                        return;
                    }
                    var result = 9001 * (1 / distToResupply);
                    __result = result;
                    __runOriginal = false;
                    return;
                }
                __runOriginal = true;
                return;
            }
        }

        [HarmonyPatch(typeof(PreferOptimalDistanceToAllyFactor), "EvaluateInfluenceMapFactorAtPositionWithAlly",
            new Type[] { typeof(AbstractActor), typeof(Vector3), typeof(float), typeof(ICombatant) })]
        public static class PreferOptimalDistanceToAllyFactor_EvaluateInfluenceMapFactorAtPosition_BattleArmor
        {
            public static void Prefix(ref bool __runOriginal, PreferFarthestAwayFromClosestHostilePositionFactor __instance, AbstractActor unit,
                Vector3 position, float angle, ICombatant allyUnit, ref float __result)
            {
                if (!__runOriginal) return;
                if (unit.HasMountedUnits() || (unit.CanSwarm() && unit is TrooperSquad))
                {
                    var result = 9001 * (1 / unit.DistanceToClosestDetectedEnemy(position));
                    ModInit.modLog?.Debug?.Write(
                        $"[PreferOptimalDistanceToAllyFactor] Actor {unit.DisplayName} evaluating position {position}, should return {result}");
                    __result = result;
                    __runOriginal = false;
                    return;
                }
                if (unit.AreAnyWeaponsOutOfAmmo() || unit.SummaryArmorCurrent / unit.StartingArmor <= 0.6f)
                {
                    var distToResupply = unit.GetDistanceToClosestDetectedResupply(position);
                    if (distToResupply <= -5f)
                    {
                        __runOriginal = true;
                        return;
                    }
                    var result = 9001 * (1 / distToResupply);
                    __result = result;
                    __runOriginal = false;
                    return;
                }
                __runOriginal = true;
                return;
            }
        }

        [HarmonyPatch(typeof(PreferHigherExpectedDamageToHostileFactor),
            "EvaluateInfluenceMapFactorAtPositionWithHostile",
            new Type[] { typeof(AbstractActor), typeof(Vector3), typeof(float), typeof(MoveType), typeof(ICombatant) })]
        public static class PreferHigherExpectedDamageToHostileFactor_EvaluateInfluenceMapFactorAtPosition_BattleArmor
        {
            public static void Prefix(ref bool __runOriginal, PreferFarthestAwayFromClosestHostilePositionFactor __instance, AbstractActor unit,
                Vector3 position, float angle, MoveType moveType, ICombatant hostileUnit, ref float __result)
            {
                if (!__runOriginal) return;
                if (unit.HasMountedUnits() || (unit.CanSwarm() && unit is TrooperSquad))
                {
                    var result = 9001 * (1 / unit.DistanceToClosestDetectedEnemy(position));
                    ModInit.modLog?.Debug?.Write(
                        $"[PreferFarthestAwayFromClosestHostilePositionFactor] Actor {unit.DisplayName} evaluating position {position}, should return {result}");
                    __result = result;
                    __runOriginal = false;
                    return;
                }
                if (unit.AreAnyWeaponsOutOfAmmo() || unit.SummaryArmorCurrent / unit.StartingArmor <= 0.6f)
                {
                    var distToResupply = unit.GetDistanceToClosestDetectedResupply(position);
                    if (distToResupply <= -5f)
                    {
                        __runOriginal = true;
                        return;
                    }
                    var result = 9001 * (1 / distToResupply);
                    __result = result;
                    __runOriginal = false;
                    return;
                }
                __runOriginal = true;
                return;
            }
        }

        [HarmonyPatch(typeof(PreferNoCloserThanMinDistToHostileFactor),
            "EvaluateInfluenceMapFactorAtPositionWithHostile",
            new Type[] { typeof(AbstractActor), typeof(Vector3), typeof(float), typeof(MoveType), typeof(ICombatant) })]
        public static class PreferNoCloserThanMinDistToHostileFactor_EvaluateInfluenceMapFactorAtPosition_BattleArmor
        {
            public static void Prefix(ref bool __runOriginal, PreferNoCloserThanMinDistToHostileFactor __instance, AbstractActor unit,
                Vector3 position, float angle, MoveType moveType, ICombatant hostileUnit, ref float __result)
            {
                if (!__runOriginal) return;
                if (unit.HasMountedUnits() || (unit.CanSwarm() && unit is TrooperSquad))
                {
                    var result = 9001 * (1 / unit.DistanceToClosestDetectedEnemy(position));
                    ModInit.modLog?.Debug?.Write(
                        $"[PreferNoCloserThanMinDistToHostileFactor] Actor {unit.DisplayName} evaluating position {position}, should return {result}");
                    __result = result;
                    __runOriginal = false;
                    return;
                }
                if (unit.AreAnyWeaponsOutOfAmmo() || unit.SummaryArmorCurrent / unit.StartingArmor <= 0.6f)
                {
                    var distToResupply = unit.GetDistanceToClosestDetectedResupply(position);
                    if (distToResupply <= -5f)
                    {
                        __runOriginal = true;
                        return;
                    }
                    var result = 9001 * (1 / distToResupply);
                    __result = result;
                    __runOriginal = false;
                    return;
                }
                __runOriginal = true;
                return;
            }
        }

        [HarmonyPatch(typeof(PreferOptimalDistanceToHostileFactor), "EvaluateInfluenceMapFactorAtPositionWithHostile",
            new Type[] { typeof(AbstractActor), typeof(Vector3), typeof(float), typeof(MoveType), typeof(ICombatant) })]
        public static class PreferOptimalDistanceToHostileFactor_EvaluateInfluenceMapFactorAtPosition_BattleArmor
        {
            public static void Prefix(ref bool __runOriginal, PreferOptimalDistanceToHostileFactor __instance, AbstractActor unit,
                Vector3 position, float angle, MoveType moveType, ICombatant hostileUnit, ref float __result)
            {
                if (!__runOriginal) return;
                if (unit.HasMountedUnits() || (unit.CanSwarm() && unit is TrooperSquad))
                {
                    var result = 9001 * (1 / unit.DistanceToClosestDetectedEnemy(position));
                    __result = result;
                    ModInit.modLog?.Debug?.Write(
                        $"[PreferOptimalDistanceToHostileFactor] Actor {unit.DisplayName} evaluating position {position}, should return {result}");
                    __runOriginal = false;
                    return;
                }
                if (unit.AreAnyWeaponsOutOfAmmo() || unit.SummaryArmorCurrent / unit.StartingArmor <= 0.6f)
                {
                    var distToResupply = unit.GetDistanceToClosestDetectedResupply(position);
                    if (distToResupply <= -5f)
                    {
                        __runOriginal = true;
                        return;
                    }
                    var result = 9001 * (1 / distToResupply);
                    __result = result;
                    __runOriginal = false;
                    return;
                }
                __runOriginal = true;
                return;
            }
        }
    }
}
