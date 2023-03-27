using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using BattleTech.Data;
using CustomUnits;
using Harmony;
using HBS.Collections;
using UnityEngine;
using static HoudiniEngineUnity.HEU_MaterialData;

namespace StrategicOperations.Framework
{
    public static class SpawnUtils
    {
        public static Vector3 FetchRandomAdjacentHexFromVector(this Vector3 startVector, AbstractActor sourceActor, AbstractActor targetActor, int minRange, int maxRange)
        {
            var points = sourceActor.Combat.HexGrid.GetAdjacentPointsOnGrid(startVector);
            var validPoints = new List<Vector3>();
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 resultPos = Vector3.zero;
                var sprintGrid = sourceActor.Pathing.SprintingGrid;//Traverse.Create(actor.Pathing).Property("WalkingGrid").GetValue<PathNodeGrid>();
                var pathNode = sprintGrid.GetClosestPathNode(points[i], 0f, 1000f, points[i], ref resultPos,
                    out var resultAngle, false, false);
                if (pathNode != null)
                {
                    var list = sprintGrid.BuildPathFromEnd(pathNode, 1000f, resultPos, points[i], null, out var costLeft, out resultPos, out resultAngle);
                    if (list != null && list.Count > 0)
                    {
                        validPoints.Add(resultPos);
                    }
                }
            }
            var point = sourceActor.CurrentPosition;
            point.y = sourceActor.Combat.MapMetaData.GetLerpedHeightAt(point, false);
            if (validPoints.Count > 0)
            {
                var usablePoints = new List<Classes.SpawnCoords>();
                foreach (var validPoint in validPoints)
                {
                    var distNodeToTarget = Vector3.Distance(validPoint, startVector);
                    var distNodeToSource = Vector3.Distance(validPoint, sourceActor.CurrentPosition);
                    ModInit.modLog?.Trace?.Write($"point {validPoint} is {distNodeToTarget} from target vs min {minRange} and max {maxRange}.");
                    if (distNodeToTarget >= minRange && distNodeToSource <= maxRange)
                    {
                        var coord = new Classes.SpawnCoords(Guid.NewGuid().ToString(), validPoint, distNodeToTarget);
                        usablePoints.Add(coord);
                        ModInit.modLog?.Trace?.Write($"Added point {validPoint} to potential list.");
                    }
                }

                if (usablePoints.Count == 0)
                {
                    ModInit.modLog?.Trace?.Write($"No usable point. Just plonking the actor down on top of the target.");
                    return targetActor.CurrentPosition;
                }

                usablePoints.Sort((x, y) => x.DistFromTarget.CompareTo(y.DistFromTarget));

                point = usablePoints[0].Loc;
                point.y = sourceActor.Combat.MapMetaData.GetLerpedHeightAt(point, false);
                return point;
            }
            return point;
        }

        public static void ResetPathGridSpawn(this Pathing pathing, Vector3 origin, float beginAngle, AbstractActor actor, bool justStoodUp)
        {
            pathing.OwningActor = actor;
            pathing.PathingCaps = actor.PathingCaps;
            pathing.MovementCaps = actor.MovementCaps;
            float num = 1f - (justStoodUp ? (0.75f - (float)pathing.OwningActor.SkillPiloting / pathing.Combat.Constants.PilotingConstants.PilotingDivisor) : 0f);
            pathing.WalkingGrid.ResetPathGrid(origin, beginAngle, pathing.PathingCaps, actor.MaxWalkDistanceInital() * num, MoveType.Walking);
            pathing.SprintingGrid.ResetPathGrid(origin, beginAngle, pathing.PathingCaps, actor.MaxSprintDistanceInital() * num, MoveType.Sprinting);
            pathing.BackwardGrid.ResetPathGrid(origin, beginAngle, pathing.PathingCaps, actor.MaxBackwardDistance * num, MoveType.Backward);
            pathing.IsLockedToDest = false;
        }

        public static Vector3 FindValidSpawn(AbstractActor target, AbstractActor source, int minRange, int maxRange)
        {
            var pathing = new Pathing(target);
            pathing.ResetPathGridSpawn(target.CurrentPosition, target.CurrentRotation.eulerAngles.y, target, false);
            pathing.UpdateBuild();
            var sprintGrid = pathing.SprintingGrid;//Traverse.Create(pathing).Property("SprintingGrid").GetValue<PathNodeGrid>();
            var nodes = sprintGrid.open;//Traverse.Create(sprintGrid).Field("pathNodes").GetValue<PathNode[,]>();
            nodes.AddRange(sprintGrid.neighbors);
            var usableNodes = new List<Classes.SpawnCoords>();
            ModInit.modLog?.Trace?.Write($"There are {nodes.Count} nodes to check.");
            ModInit.modLog?.Trace?.Write($"dumping debug info: maxCost {pathing.MaxCost}, sprintgrid max distance {sprintGrid.MaxDistance} actor used to build max sprint {pathing.OwningActor.MaxSprintDistance}, actor max sprint initial {pathing.OwningActor.MaxSprintDistanceInital()}.");
            foreach (var node in nodes)
            {
                ModInit.modLog?.Trace?.Write($"Checking node {node}.");
                if (node != null)
                {
                    ModInit.modLog?.Trace?.Write($"Checking node {node.Position} for valid dest and occupying actors.");
                    if (node.IsValidDestination && node.OccupyingActor == null)
                    {
                        var distNodeToTarget = Vector3.Distance(node.Position, target.CurrentPosition);
                        var distNodeToSource = Vector3.Distance(node.Position, source.CurrentPosition);
                        ModInit.modLog?.Trace?.Write($"node {node.Position} is {distNodeToTarget} from target vs min {minRange} and max {maxRange}.");
                        if (distNodeToTarget >= minRange && distNodeToSource <= maxRange)
                        {
                            var coord = new Classes.SpawnCoords(Guid.NewGuid().ToString(), node.Position, distNodeToTarget);
                            usableNodes.Add(coord);
                            ModInit.modLog?.Trace?.Write($"Added node {node.Position} to potential list.");
                        }
                    }
                }
            }

            if (usableNodes.Count == 0)
            {
                ModInit.modLog?.Trace?.Write($"No usable nodes. Just plonking the actor down on top of the target.");
                return target.CurrentPosition;
            }

            usableNodes.Sort((x, y) => x.DistFromTarget.CompareTo(y.DistFromTarget));

            if (ModInit.modSettings.debugFlares && ModInit.modSettings.enableTrace)
            {
                ModInit.modLog?.Trace?.Write($"Doing a useless loop and printing 1st 10 locs");
                {
                    for (int i = 0; i < usableNodes.Count || i < 10; i++)
                    {
                        ModInit.modLog?.Trace?.Write($"Distance at index {i} is {usableNodes[i].DistFromTarget}");
                    }
                }
            }
            return usableNodes[0].Loc;
        }
    }
}