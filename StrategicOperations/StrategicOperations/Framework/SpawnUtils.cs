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
        public static void SpawnBattleArmorAtActor(AbstractActor actor, string chosenBA, Lance baLance)
        {
            var dm = actor.Combat.DataManager;
            LoadRequest loadRequest = dm.CreateLoadRequest();
            loadRequest.AddBlindLoadRequest(BattleTechResourceType.MechDef, chosenBA);
            ModInit.modLog.LogMessage($"Added loadrequest for MechDef: {chosenBA}");
            loadRequest.ProcessRequests(1000);

            var instanceGUID =
                $"{actor.Description.Id}_{actor.team.Name}_{chosenBA}";

            if (actor.Combat.TurnDirector.CurrentRound <= 1)
            {
                if (ModState.DeferredInvokeBattleArmor.All(x => x.Key != instanceGUID) && !ModState.DeferredBattleArmorSpawnerFromDelegate)
                {
                    ModInit.modLog.LogMessage(
                        $"Deferred BA Spawner missing, creating delegate and returning. Delegate should spawn {chosenBA}");

                    void DeferredInvokeBASpawn() =>
                        SpawnBattleArmorAtActor(actor, chosenBA, baLance);

                    var kvp = new KeyValuePair<string, Action>(instanceGUID, DeferredInvokeBASpawn);
                    ModState.DeferredInvokeBattleArmor.Add(kvp);
                    foreach (var value in ModState.DeferredInvokeBattleArmor)
                    {
                        ModInit.modLog.LogTrace(
                            $"there is a delegate {value.Key} here, with value {value.Value}");
                    }
                    return;
                }
            }

            var teamSelection = actor.team;
            var alliedActors = actor.Combat.AllMechs.Where(x => x.team == actor.team);
            var chosenpilotSourceMech = alliedActors.GetRandomElement();
            var newPilotDefID = chosenpilotSourceMech.pilot.pilotDef.Description.Id;
            dm.PilotDefs.TryGet(newPilotDefID, out var newPilotDef);
            ModInit.modLog.LogMessage($"Attempting to spawn {chosenBA} with pilot {newPilotDef.Description.Callsign}.");
            dm.MechDefs.TryGet(chosenBA, out var newBattleArmorDef);
            newBattleArmorDef.Refresh();
            var customEncounterTags = new TagSet(teamSelection.EncounterTags);
            customEncounterTags.Add("SpawnedFromAbility");
            var newBattleArmor = ActorFactory.CreateMech(newBattleArmorDef, newPilotDef,
                customEncounterTags, teamSelection.Combat,
                teamSelection.GetNextSupportUnitGuid(), "", actor.team.HeraldryDef);
            newBattleArmor.Init(actor.CurrentPosition, actor.CurrentRotation.eulerAngles.y, false);
            newBattleArmor.InitGameRep(null);
            teamSelection.AddUnit(newBattleArmor);
            newBattleArmor.AddToTeam(teamSelection);
            newBattleArmor.AddToLance(baLance);
            baLance.AddUnitGUID(newBattleArmor.GUID);
            newBattleArmor.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(
                actor.Combat.BattleTechGame, newBattleArmor, BehaviorTreeIDEnum.CoreAITree);
            newBattleArmor.OnPositionUpdate(actor.CurrentPosition, actor.CurrentRotation, -1, true, null, false);
            newBattleArmor.DynamicUnitRole = UnitRole.Brawler;
            UnitSpawnedMessage message = new UnitSpawnedMessage("FROM_ABILITY", newBattleArmor.GUID);
            actor.Combat.MessageCenter.PublishMessage(message);

            actor.MountBattleArmorToChassis(newBattleArmor);
            //newBattleArmor.GameRep.IsTargetable = false;
            newBattleArmor.TeleportActor(actor.CurrentPosition);

            ModState.PositionLockMount.Add(newBattleArmor.GUID, actor.GUID);
            ModInit.modLog.LogMessage(
                $"[SpawnBattleArmorAtActor] Added PositionLockMount with rider  {newBattleArmor.DisplayName} {newBattleArmor.GUID} and carrier {actor.DisplayName} {actor.GUID}.");
        }

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