using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using BattleTech.Data;
using Harmony;
using HBS.Collections;
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

            var sprintGrid = pathing.SprintingGrid;//Traverse.Create(pathing).Property("SprintingGrid").GetValue<PathNodeGrid>();
            var nodes = sprintGrid.pathNodes;//Traverse.Create(sprintGrid).Field("pathNodes").GetValue<PathNode[,]>();
            var usableNodes = new List<Classes.SpawnCoords>();
            ModInit.modLog?.Trace?.Write($"There are {nodes.Length} nodes to check.");
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