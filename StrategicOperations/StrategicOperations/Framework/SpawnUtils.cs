using System;
using System.Collections.Generic;
using BattleTech;
using Harmony;
using UnityEngine;

namespace StrategicOperations.Framework
{
    public static class SpawnUtils
    {
        public static Vector3 FindValidSpawn(AbstractActor target, AbstractActor source, int minRange, int maxRange)
        {
            var pathing = new Pathing(target);
            pathing.ResetPathGrid(target.CurrentPosition, target.CurrentRotation.eulerAngles.y, target, false);
            pathing.UpdateBuild();

            var sprintGrid = Traverse.Create(pathing).Property("SprintingGrid").GetValue<PathNodeGrid>();
            var nodes = Traverse.Create(sprintGrid).Field("pathNodes").GetValue<PathNode[,]>();
            var usableNodes = new List<Classes.SpawnCoords>();
            ModInit.modLog.LogTrace($"There are {nodes.Length} nodes to check.");
            foreach (var node in nodes)
            {
                ModInit.modLog.LogTrace($"Checking node {node}.");
                if (node != null)
                {
                    ModInit.modLog.LogTrace($"Checking node {node.Position} for valid dest and occupying actors.");
                    if (node.IsValidDestination && node.OccupyingActor == null)
                    {
                        var distNodeToTarget = Vector3.Distance(node.Position, target.CurrentPosition);
                        var distNodeToSource = Vector3.Distance(node.Position, source.CurrentPosition);
                        ModInit.modLog.LogTrace($"node {node.Position} is {distNodeToTarget} from target vs min {minRange} and max {maxRange}.");
                        if (distNodeToTarget >= minRange && distNodeToSource <= maxRange)
                        {
                            var coord = new Classes.SpawnCoords(Guid.NewGuid().ToString(), node.Position, distNodeToTarget);
                            usableNodes.Add(coord);
                            ModInit.modLog.LogTrace($"Added node {node.Position} to potential list.");
                        }
                    }
                }
            }

            if (usableNodes.Count == 0)
            {
                ModInit.modLog.LogTrace($"No usable nodes. Just plonking the actor down on top of the target.");
                return target.CurrentPosition;
            }

            usableNodes.Sort((x, y) => x.DistFromTarget.CompareTo(y.DistFromTarget));

            if (ModInit.modSettings.debugFlares && ModInit.modSettings.enableTrace)
            {
                ModInit.modLog.LogTrace($"Doing a useless loop and printing 1st 10 locs");
                {
                    for (int i = 0; i < usableNodes.Count || i < 10; i++)
                    {
                        ModInit.modLog.LogTrace($"Distance at index {i} is {usableNodes[i].DistFromTarget}");
                    }
                }
            }

            return usableNodes[0].Loc;

        }
    }
}