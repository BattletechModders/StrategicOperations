using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using MissionControl;
using MissionControl.Logic;
using UnityEngine;
using Main = BattleTech.Main;
using UnityGameInstance = BattleTech.UnityGameInstance;

namespace StrategicOperations.Framework
{
    class SpawnUtils
    {
        public static class StratSpawn
        {
            private static GameObject lance;
            private static GameObject orientationTarget;
            private static GameObject lookTarget;
            private static SceneManipulationLogic.LookDirection lookDirection;
            private static float mustBeBeyondDistance = 50f;
            private static float mustBeWithinDistance = 100f;
            private static int bailoutCount = 0;
            private static int BAILOUT_MAX = 2;
            private static Vector3 originalOrigin = Vector3.zero;
            private static int ADJACENT_NODE_LIMITED = 20;
            private static List<Vector3> checkedAdjacentPoints = new List<Vector3>();

            private static List<Vector3> invalidSpawnLocations = new List<Vector3>();

            private static int AttemptCountMax { get; set; } = 5;
            private static int AttemptCount { get; set; } = 0;
            private static int TotalAttemptMax { get; set; } = 20;
            private static int TotalAttemptCount { get; set; } = 0;


            public static Vector3 GetClosestValidPathFindingHex(GameObject originGo, Vector3 origin, string identifier,
                Vector3 pathfindingTarget, int radius = 3)
            {
                if (originalOrigin == Vector3.zero)
                {
                    originalOrigin = origin;
                }

                if (radius > 12)
                {
                    if (bailoutCount >= BAILOUT_MAX)
                    {
                        origin = originalOrigin.GetClosestHexLerpedPointOnGrid();
                        Main.LogDebugWarning(string.Format(
                            "[GetClosestValidPathFindingHex] No valid points found. Returning original origin with fixed height of '{0}'",
                            origin));
                        checkedAdjacentPoints.Clear();
                        bailoutCount = 0;
                        originalOrigin = Vector3.zero;
                        return origin;
                    }

                    bailoutCount++;
                    Main.LogDebugWarning(
                        "[GetClosestValidPathFindingHex] No valid points found. Casting net out to another location'");
                    radius = 3;
                    Vector3 randomPositionFromBadPathfindTarget =
                        SceneUtils.GetRandomPositionWithinBounds(pathfindingTarget, 96f);
                    Main.LogDebugWarning(string.Format(
                        "[GetClosestValidPathFindingHex] New location to test for pathfind target is '{0}'",
                        randomPositionFromBadPathfindTarget));
                    pathfindingTarget = randomPositionFromBadPathfindTarget;
                }

                Vector3 validOrigin = PathfindFromPointToSpawn(originGo, origin, radius, identifier, pathfindingTarget);
                if (validOrigin == Vector3.zero)
                {
                    Main.LogDebugWarning(string.Format(
                        "[GetClosestValidPathFindingHex] No valid points found. Expanding search radius from radius '{0}' to '{1}'",
                        radius, radius * 2));
                    origin = GetClosestValidPathFindingHex(originGo, origin, identifier, pathfindingTarget, radius * 2);
                    checkedAdjacentPoints.Clear();
                    return origin;
                }

                Main.LogDebug(string.Format("[GetClosestValidPathFindingHex] Returning final point '{0}'",
                    validOrigin));
                checkedAdjacentPoints.Clear();
                return validOrigin;
            }

            private static Vector3 PathfindFromPointToSpawn(GameObject originGo, Vector3 origin, int radius,
                string identifier, Vector3 pathfindingTarget)
            {
                var combat = UnityGameInstance.BattleTechGame.Combat;
                var encounterLayerData = combat.EncounterLayerData;

                var originOnGrid = origin.GetClosestHexLerpedPointOnGrid();
                var pathfindingPoint = pathfindingTarget.GetClosestHexLerpedPointOnGrid();
                Main.LogDebug(string.Format("[PathfindFromPointToPlayerSpawn] Using pathfinding point '{0}'",
                    pathfindingPoint));
                if (!PathFinderManager.Instance.IsSpawnValid(originGo, originOnGrid, pathfindingPoint, UnitType.Mech,
                    identifier))
                {
                    List<Vector3> adjacentPointsOnGrid =
                        combat.HexGrid.GetGridPointsAroundPointWithinRadius(originOnGrid, radius);
                    Main.LogDebug(string.Format("[PathfindFromPointToPlayerSpawn] Adjacent point count is '{0}'",
                        adjacentPointsOnGrid.Count));
                    adjacentPointsOnGrid = (from point in adjacentPointsOnGrid
                        where !checkedAdjacentPoints.Contains(point) && encounterLayerData.IsInEncounterBounds(point)
                        select point).ToList<Vector3>();
                    Main.LogDebug(string.Format(
                        "[PathfindFromPointToPlayerSpawn] Removed already checked points & out of bounds points. Adjacent point count is now '{0}'",
                        adjacentPointsOnGrid.Count));
                    adjacentPointsOnGrid.Shuffle<Vector3>();
                    var count = 0;
                    foreach (Vector3 point2 in adjacentPointsOnGrid)
                    {
                        if (count > ADJACENT_NODE_LIMITED)
                        {
                            Main.LogDebug(string.Format(
                                "[PathfindFromPointToPlayerSpawn] Adjacent point count limited exceeded (random selection of {0} / {1}). Bailing.",
                                ADJACENT_NODE_LIMITED, adjacentPointsOnGrid.Count));
                            break;
                        }

                        var validPoint = point2.GetClosestHexLerpedPointOnGrid();
                        Main.LogDebug(string.Format(
                            "[PathfindFromPointToPlayerSpawn] Testing an adjacent point of '{0}'", validPoint));

                        if (PathFinderManager.Instance.IsSpawnValid(originGo, validPoint, pathfindingPoint,
                            UnitType.Mech, identifier))
                        {
                            return validPoint;
                        }

                        if (PathFinderManager.Instance.IsProbablyABadPathfindTest(pathfindingPoint))
                        {
                            Main.LogDebug(
                                "[PathfindFromPointToPlayerSpawn] Estimated this is a bad pathfind setup so trying something new.");
                            radius = 100;
                            count = ADJACENT_NODE_LIMITED;
                        }

                        checkedAdjacentPoints.Add(point2);
                        count++;
                    }

                    return Vector3.zero;
                }

                Main.LogDebug(string.Format(
                    "[PathfindFromPointToPlayerSpawn] Spawn has been found valid by pathfinding '{0}'", originOnGrid));
                return originOnGrid;
            }

            public static Vector3 FindValidSpawn(Vector3 start, UnitType type, GameObject lookTarget,
                AbstractActor originatingActor)
            {

                var combat = UnityGameInstance.BattleTechGame.Combat;

                if (TotalAttemptCount >= TotalAttemptMax)
                {
                    return start;
                }

                var spawnLoc = GetClosestValidPathFindingHex(lookTarget, start, "OrientationTarget." + lookTarget.name,
                    originatingActor.CurrentPosition, 3);

                Vector3 newSpawnPosition =
                    SceneUtils.GetRandomPositionFromTarget(spawnLoc, mustBeBeyondDistance, mustBeWithinDistance);
                newSpawnPosition = GetClosestValidPathFindingHex(lookTarget, newSpawnPosition,
                    "NewRandomSpawnPositionFromOrientationTarget." + lookTarget.name, Vector3.zero, 3);

                if (combat.EncounterLayerData.IsInEncounterBounds(newSpawnPosition))
                {

                }




                if (MissionControl.PathFinderManager.Instance.IsSpawnValid(null, start,
                    start.GetClosestHexLerpedPointOnGrid(), type, Guid.NewGuid().ToString()))
                {

                }
            }


            private void CheckAttempts()
            {
                ModState.StratSpawn.AttemptCount++;
                ModState.StratSpawn.TotalAttemptCount++;
                if (ModState.StratSpawn.AttemptCount > ModState.StratSpawn.AttemptCountMax)
                {
                    ModState.StratSpawn.AttemptCount = 0;
                    ModInit.modLog.LogMessage(
                        $"[StratSpawn] Cannot find a suitable object spawn within the boundaries of {ModState.StratSpawn.minDistanceFromTarget} and {ModState.StratSpawn.maxDistanceFromTarget}. Widening search. CWolf is the bomb.");
                    ModState.StratSpawn.minDistanceFromTarget -= 10;
                    if (ModState.StratSpawn.minDistanceFromTarget <= 10) ModState.StratSpawn.minDistanceFromTarget = 10;
                    ModState.StratSpawn.maxDistanceFromTarget += 25;
                }
            }
        }
    }
}
