using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using Abilifier.Patches;
using BattleTech;
using BattleTech.Data;
using BattleTech.Framework;
using BattleTech.UI;
using CustAmmoCategories;
using CustomAmmoCategoriesPatches;
using CustomUnits;
using Harmony;
using HBS.Collections;
using UnityEngine;
using static StrategicOperations.Framework.Classes;
using Log = CustomAmmoCategoriesLog.Log;
using Random = UnityEngine.Random;

namespace StrategicOperations.Framework
{
    public static class Utils
    {
        public static bool IsComponentPlayerControllable(this TagSet tagset, out bool forced)
        {
            if (tagset.Any(x => x == "StratOps_player_control_enable"))
            {
                forced = true;
                return true;
            }
            if (tagset.Any(x => x == "StratOps_player_control_disable"))
            {
                forced = true;
                return false;
            }

            forced = false;
            return true;
        }
        public static bool ShouldPlayerControlSpawn(Team team, Ability ability, string quid)
        {
            var sim = UnityGameInstance.BattleTechGame.Simulation;
            if (sim == null) return false;
            if (!team.IsLocalPlayer) return false;
            if (ModInit.modSettings.PlayerControlSpawns) return true;

            if (ModState.StoredCmdParams.ContainsKey(quid))
            {
                if (ModState.StoredCmdParams[quid].PlayerControl &&
                    ModState.StoredCmdParams[quid].PlayerControlOverridden) return true;
                if (!ModState.StoredCmdParams[quid].PlayerControl &&
                    ModState.StoredCmdParams[quid].PlayerControlOverridden) return false;
            }

            if (ModInit.modSettings.PlayerControlSpawnAbilities.Contains(ability.Def.Id)) return true;
            if (ModInit.modSettings.PlayerControlSpawnAbilitiesBlacklist.Contains(ability.Def.Id)) return false;

            return sim.CompanyStats.GetValue<bool>("StratOps_ControlSpawns");
        }

        public static bool CheckOrInitSpawnControl(this SimGameState sim)
        {
            if (!sim.CompanyStats.ContainsStatistic("StratOps_ControlSpawns"))
            {
                sim.CompanyStats.AddStatistic<bool>("StratOps_ControlSpawns", false);
                return false;
            }
            return sim.CompanyStats.GetValue<bool>("StratOps_ControlSpawns");
        }
        public static string Generate2PtCMDQuasiGUID(this Ability ability, Vector3 positionA, Vector3 positionB)
        {
            var sim = UnityGameInstance.BattleTechGame.Simulation;
            return $"STRAFE_ID_{ability.Def.Id}_{positionA}_{positionB}";
        }

        public static void ForceUnitToLastActualPhase(this AbstractActor actor)
        {
            if (actor.Combat.TurnDirector.IsInterleaved && actor.Initiative != actor.Combat.TurnDirector.LastPhase)
            {
                actor.Initiative = actor.Combat.TurnDirector.LastPhase;
                actor.Combat.MessageCenter.PublishMessage(new ActorPhaseInfoChanged(actor.GUID));
            }
            actor.ProcessHesitationSBI();
        }

        public static void ProcessHesitationSBI(this AbstractActor actor)
        {
            if (actor.StatCollection.ContainsStatistic("SBI_STATE_HESITATION"))
            {
                var currentHesitation = actor.StatCollection.GetValue<int>("SBI_STATE_HESITATION");
                var actorHesitationPhaseMod = actor.StatCollection.GetValue<int>("SBI_MOD_HESITATION") * -1; // invert from SBI
                var phasesMoved = Math.Abs(actor.Combat.TurnDirector.CurrentPhase - actor.Combat.TurnDirector.LastPhase);
                var hesitationPenalty = (ModInit.modSettings.SBI_HesitationMultiplier * phasesMoved) + actorHesitationPhaseMod;
                var roundedPenalty = Mathf.RoundToInt(hesitationPenalty);
                var finalHesitation = currentHesitation + roundedPenalty;
                actor.StatCollection.ModifyStat<int>(actor.GUID, -1, "SBI_STATE_HESITATION", StatCollection.StatOperation.Set, finalHesitation);
                ModInit.modLog?.Info?.Write(
                    $"[ProcessHesitationSBI] Used quick-reserve with SBI. Final hesitation set to {finalHesitation} from: Multiplier setting {ModInit.modSettings.SBI_HesitationMultiplier} x PhasesMoved {phasesMoved} + Actor Hesitation Mod {actorHesitationPhaseMod} + Current hesitation {currentHesitation} (rounded)");
            }
        }

       public static AbstractActor FindMeAnOpforUnit(this AbstractActor actor)
        {
            var opforTeam = actor.Combat.Teams.FirstOrDefault(x => x.GUID == "be77cadd-e245-4240-a93e-b99cc98902a5");
            if (opforTeam != null)
            {
                var opforUnit = opforTeam.units.FirstOrDefault(x => !x.IsDead);
                if (opforUnit != null)
                {
                    return opforUnit;
                }
            }
            return null;
        }
        public static float GetAvoidStrafeChanceForTeam(this ICombatant combatant)
        {
            var actors = combatant.Combat.GetAllLivingActors();
            var cumAA = 0f;
            var unitDivisor = 0;
            foreach (var unit in actors)
            {
                if (unit.team.IsFriendly(combatant.team))
                {
                    cumAA += unit.GetAAAFactor();
                    unitDivisor++;
                    ModInit.modLog?.Debug?.Write($"unit {unit.DisplayName} is friendly of {combatant.DisplayName}. Added AA factor {unit.GetAAAFactor()}; total is now {cumAA} from {unitDivisor} units");
                }
            }

            if (unitDivisor == 0) return 0f;
            var finalAA = cumAA / unitDivisor;
            ModInit.modLog?.Debug?.Write($"final AA value for {combatant.DisplayName} and team {combatant.team.DisplayName}: {finalAA}");
            return finalAA;
        }

        public static float GetAAAFactor(this AbstractActor actor)
        {
            return actor.StatCollection.GetValue<bool>("UseAAAFactor") ? actor.StatCollection.GetValue<float>("AAAFactor") : 0f;
        }
        public static void PublishInvocationExternal(this MessageCenter messageCenter, MessageCenterMessage invocation)
        {
            messageCenter.AddSubscriber(MessageCenterMessageType.InvocationStackSequenceCreated, new ReceiveMessageCenterMessage(HandleInvocationStackSequenceCreatedExternal));
            messageCenter.PublishMessage(invocation);
            messageCenter.RemoveSubscriber(MessageCenterMessageType.InvocationStackSequenceCreated, new ReceiveMessageCenterMessage(HandleInvocationStackSequenceCreatedExternal));
            
            //if (this.Orders == null && this.SelectionType != SelectionType.DoneWithMech)
            //{
            //    Debug.LogError("No Orders assigned from invocation!");
            //}
        }

        public static void HandleInvocationStackSequenceCreatedExternal(this MessageCenterMessage message)
        {
           // InvocationStackSequenceCreated invocationStackSequenceCreated = message as InvocationStackSequenceCreated;
            //message.Orders = invocationStackSequenceCreated.StackSequence; //we dont need orders for the movement sequence. maybe.
        }

        public static void UpdateRangeIndicator(this CombatTargetingReticle reticle, Vector3 newPosition, bool minRangeShow, bool maxRangeShow)
        {
            if (minRangeShow)
            {
                reticle.MinRangeHolder.SetActive(false);
                reticle.MinRangeHolder.transform.position = newPosition;
                reticle.MinRangeHolder.SetActive(true);
            }

            if (maxRangeShow)
            {
                reticle.MaxRangeHolder.SetActive(false);
                reticle.MaxRangeHolder.transform.position = newPosition;
                reticle.MaxRangeHolder.SetActive(true);
            }
        }

        public static bool IsCustomUnitVehicle(this ICombatant combatant)
        {
            if (!combatant.StatCollection.ContainsStatistic("CUFakeVehicle")) return false;
            return combatant.StatCollection.GetValue<bool>("CUFakeVehicle");
        }

        public static Vector3 GetHexFromVector(this Vector3 point)
        {
            var combat = UnityGameInstance.BattleTechGame.Combat;
            var hex = combat.HexGrid.GetClosestPointOnGrid(point);
            hex.y = combat.MapMetaData.GetLerpedHeightAt(hex, false);
            return hex;
        }

        public static object FetchUnitFromDataManager(this DataManager dm, string id)
        {
            if (id.StartsWith("mechdef_"))
            {
                dm.MechDefs.TryGet(id, out MechDef result);
                {
                    return result as MechDef;
                }
            }
            else if (id.StartsWith("vehicledef_"))
            {
                dm.VehicleDefs.TryGet(id, out VehicleDef result);
                {
                    return result as VehicleDef;
                }
            }
            else if (id.StartsWith("turretdef_"))
            {
                dm.TurretDefs.TryGet(id, out TurretDef result);
                {
                    return result as TurretDef;
                }
            }

            return null;
        }

        public static float DistanceToClosestDetectedEnemy(this AbstractActor actor, Vector3 loc)
        {
            var enemy = actor.GetClosestDetectedEnemy(loc);
            if (enemy == null) return 9999f;
            float magnitude = (enemy.CurrentPosition - loc).magnitude;
            return magnitude;
        }
        public static AbstractActor GetClosestDetectedEnemy(this AbstractActor actor, Vector3 loc)
        {
            var enemyUnits = new List<AbstractActor>();
            enemyUnits.AddRange(actor.team.VisibilityCache.GetAllDetectedEnemies(actor).Where(x=>!x.IsDead && !x.IsFlaggedForDeath));
            var num = -1f;
            AbstractActor closestActor = null;
            foreach (var enemy in enemyUnits)
            {
                var magnitude = (loc - enemy.CurrentPosition).magnitude;
                if (num < 0f || magnitude < num)
                {
                    num = magnitude;
                    closestActor = enemy;
                }
            }
            return closestActor;
        }

        public static AbstractActor GetClosestDetectedSwarmTarget(this AbstractActor actor, Vector3 loc)
        {
            var enemyUnits = new List<AbstractActor>();
            enemyUnits.AddRange(actor.team.VisibilityCache.GetAllDetectedEnemies(actor).Where(x => !x.IsDead && !x.IsFlaggedForDeath));
            var num = -1f;
            AbstractActor closestActor = null;
            foreach (var enemy in enemyUnits)
            {
                if (!enemy.getIsUnSwarmable())
                {
                    var magnitude = (loc - enemy.CurrentPosition).magnitude;
                    if (num < 0f || magnitude < num)
                    {
                        num = magnitude;
                        closestActor = enemy;
                    }
                }
            }
            return closestActor;
        }

        public static AbstractActor GetClosestDetectedFriendly(Vector3 loc, AbstractActor actor)
        {
            var friendlyUnits = actor.team.VisibilityCache.GetAllFriendlies(actor).Where(x=> !x.IsDead && !x.IsFlaggedForDeath);
            var num = -1f;
            AbstractActor closestActor = null;
            foreach (var friendly in friendlyUnits)
            {
                var magnitude = (loc - friendly.CurrentPosition).magnitude;
                if (num < 0f || magnitude < num)
                {
                    num = magnitude;
                    closestActor = friendly;
                }
            }
            return closestActor;
        }

        public static List<AbstractActor> GetAllDetectedEnemies(this SharedVisibilityCache cache, AbstractActor actor)
        {
            var detectedEnemies = new List<AbstractActor>();
            foreach (var enemy in actor.Combat.GetAllLivingActors())
            {
                if (cache.CachedVisibilityToTarget(enemy).VisibilityLevel > 0 && actor.team.IsEnemy(enemy.team) && !enemy.IsDead && !enemy.IsFlaggedForDeath)
                {
                    ModInit.modLog?.Debug?.Write($"unit {enemy.DisplayName} is enemy of {actor.DisplayName}.");
                    detectedEnemies.Add(enemy);
                }
            }
            return detectedEnemies;
        }

        public static List<AbstractActor> GetAllEnemiesWithinRange(this AbstractActor actor, float range)
        {
            var detectedEnemies = new List<AbstractActor>();
            foreach (var enemy in actor.Combat.GetAllLivingActors())
            {
                if (actor.team.IsEnemy(enemy.team) && !enemy.IsDead && !enemy.IsFlaggedForDeath)
                {
                    if (Vector3.Distance(actor.CurrentPosition, enemy.CurrentPosition) <= range)
                    {
                        ModInit.modLog?.Debug?.Write($"unit {enemy.DisplayName} is enemy of {actor.DisplayName}.");
                        detectedEnemies.Add(enemy);
                    }
                }
            }
            return detectedEnemies;
        }

        public static List<AbstractActor> GetAllFriendliesWithinRange(this AbstractActor actor, float range)
        {
            var detectedFriendlies = new List<AbstractActor>();
            foreach (var friendly in actor.team.units)
            {
                if ((friendly.team.IsLocalPlayer || friendly.team.IsFriendly(actor.team)) && !friendly.IsDead && !friendly.IsFlaggedForDeath)
                {
                    if (Vector3.Distance(actor.CurrentPosition, friendly.CurrentPosition) <= range)
                    {
                        ModInit.modLog?.Debug?.Write($"unit {friendly.DisplayName} is friendly of {actor.DisplayName}.");
                        detectedFriendlies.Add(friendly);
                    }
                }
            }
            return detectedFriendlies;
        }

        public static List<AbstractActor> GetAllFriendlies (this SharedVisibilityCache cache, AbstractActor actor)
        {
            var friendlyActors = new List<AbstractActor>();
            foreach (var friendly in actor.Combat.GetAllLivingActors())
            {
                if (actor.team.IsFriendly(friendly.team) && !friendly.IsDead && !friendly.IsFlaggedForDeath)
                {
                    ModInit.modLog?.Debug?.Write($"unit {friendly.DisplayName} is friendly of {actor.DisplayName}.");
                    friendlyActors.Add(friendly);
                }
            }
            return friendlyActors;
        }

        public static List<AbstractActor> GetAllEnemies(this Team team)
        {
            var enemyActors = new List<AbstractActor>();
            foreach (var enemy in team.Combat.GetAllLivingActors())
            {
                if (team.IsEnemy(enemy.team) && !enemy.IsDead && !enemy.IsFlaggedForDeath)
                {
                    ModInit.modLog?.Debug?.Write($"unit {enemy.DisplayName} is enemy of {team.DisplayName}.");
                    enemyActors.Add(enemy);
                }
            }
            return enemyActors;
        }

        public static Vector3[] MakeCircle(Vector3 start, int numOfPoints, float radius)
        {
            if (ModInit.modSettings.debugFlares) Utils.SpawnDebugFlare(start, "vfxPrfPrtl_artillerySmokeSignal_loop",3);
            var vectors = new List<Vector3>();
            for (int i = 0; i < numOfPoints; i++)
            {
                var radians = 2 * Mathf.PI / numOfPoints * i;
                var vertical = Mathf.Sin(radians);
                var horizontal = Mathf.Cos(radians);
                var spawnDir = new Vector3(horizontal, 0, vertical);

                var newPos = start + spawnDir * radius;
                vectors.Add(newPos);
                if (ModInit.modSettings.debugFlares) Utils.SpawnDebugFlare(newPos, "vfxPrfPrtl_artillerySmokeSignal_loop", 3);
                ModInit.modLog?.Debug?.Write($"Distance from possibleStart to ray endpoint is {Vector3.Distance(start, newPos)}.");
            }

            return vectors.ToArray();
        }

        public static List<Rect> MakeRectangle(Vector3 start, Vector3 end, float width)
        {
            
            var rectangles = new List<Rect>();
            Vector3 line = end - start;
            float length = Vector3.Distance(start, end);
            ModInit.modLog?.Debug?.Write($"Rectangle length should be {length}.");
            Vector3 left = Vector3.Cross(line, Vector3.up).normalized;
            Vector3 right = -left;
            var startLeft = start + (left * width);
            var startRight = start + (right * width);
            var rectLeft = new Rect(startLeft.x, startLeft.y, width, length);
            var rectRight = new Rect(startRight.x, startRight.y, width, length);
            rectangles.Add(rectLeft);
            rectangles.Add(rectRight);
            return rectangles;//.ToArray();
        }

        public static Vector3 LerpByDistance(Vector3 start, Vector3 end, float x)
        {
            return x * Vector3.Normalize(end - start) + start;
        }

        public static HeraldryDef SwapHeraldryColors(HeraldryDef def, DataManager dataManager, Action loadCompleteCallback = null)
        {
            var secondaryID = def.primaryMechColorID;
            var tertiaryID = def.secondaryMechColorID;
            var primaryID = def.tertiaryMechColorID;

            ModInit.modLog?.Trace?.Write($"Creating new heraldry for support. {primaryID} was tertiary, now primary. {secondaryID} was primary, now secondary. {tertiaryID} was secondary, now tertiary.");
            var newHeraldry = new HeraldryDef(def.Description, def.textureLogoID, primaryID, secondaryID, tertiaryID);

            newHeraldry.DataManager = dataManager;
            LoadRequest loadRequest = dataManager.CreateLoadRequest(delegate (LoadRequest request)
            {
                newHeraldry.Refresh();
                loadCompleteCallback?.Invoke();
            }, false);
            loadRequest.AddBlindLoadRequest(BattleTechResourceType.Texture2D, newHeraldry.textureLogoID, new bool?(false));
            loadRequest.AddBlindLoadRequest(BattleTechResourceType.Sprite, newHeraldry.textureLogoID, new bool?(false));
            loadRequest.AddBlindLoadRequest(BattleTechResourceType.ColorSwatch, newHeraldry.primaryMechColorID, new bool?(false));
            loadRequest.AddBlindLoadRequest(BattleTechResourceType.ColorSwatch, newHeraldry.secondaryMechColorID, new bool?(false));
            loadRequest.AddBlindLoadRequest(BattleTechResourceType.ColorSwatch, newHeraldry.tertiaryMechColorID, new bool?(false));
            loadRequest.ProcessRequests(10U);
            newHeraldry.Refresh();
            return newHeraldry;
        }
        public static Lance CreateOrFetchCMDLance(Team team)
        {
            if (!team.lances.Any(x => x.GUID.EndsWith($"{team.GUID}_StratOps")))
            {
                Lance lance = new Lance(team, Array.Empty<LanceSpawnerRef>());
                var lanceGuid = $"{LanceSpawnerGameLogic.GetLanceGuid(Guid.NewGuid().ToString())}_{team.GUID}_StratOps";
                lance.lanceGuid = lanceGuid;
                var combat = UnityGameInstance.BattleTechGame.Combat;
                combat.ItemRegistry.AddItem(lance);
                team.lances.Add(lance);
                ModInit.modLog?.Info?.Write($"Created lance {lance.DisplayName} for Team {team.DisplayName}.");
                return lance;
            }
            return team.lances.FirstOrDefault(x => x.GUID.EndsWith($"{team.GUID}_StratOps"));
        }
        public static void CooldownAllCMDAbilities()
        {
            for (int i = 0; i < ModState.CommandAbilities.Count; i++)
            {
                ModState.CommandAbilities[i].ActivateMiniCooldown();
            }
        }

        public static void CreateOrUpdateNeutralTeam()
        {
            AITeam aiteam = null;
            var combat = UnityGameInstance.BattleTechGame.Combat;
            if (combat.IsLoadingFromSave)
            {
                aiteam = (combat.GetLoadedTeamByGUID("61612bb3-abf9-4586-952a-0559fa9dcd75") as AITeam);
            }
            if (!combat.IsLoadingFromSave || aiteam == null)
            {
                aiteam = new AITeam("Player 1 Support", Color.yellow, "61612bb3-abf9-4586-952a-0559fa9dcd75", true, combat);
            }
            combat.TurnDirector.AddTurnActor(aiteam);
            combat.ItemRegistry.AddItem(aiteam);
        }

        public static void CreateOrUpdateCustomTeam()
        {
            AITeam aiteam = null;
            var combat = UnityGameInstance.BattleTechGame.Combat;
            aiteam = new AITeam("CustomTeamTest", Color.yellow, Guid.NewGuid().ToString(), true, combat);
            combat.TurnDirector.AddTurnActor(aiteam);
            combat.ItemRegistry.AddItem(aiteam);
        }

        public static Team CreateOrUpdateAISupportTeam(Team team)
        {
            AITeam aiteam = null;
            var combat = UnityGameInstance.BattleTechGame.Combat;
            aiteam = new AITeam("Opfor Support", Color.black, Guid.NewGuid().ToString(), true, combat);
            aiteam.FactionValue = team.FactionValue;
            combat.TurnDirector.AddTurnActor(aiteam);
            combat.ItemRegistry.AddItem(aiteam);
            team.SetSupportTeam(aiteam);
            return aiteam;
        }

        public static Team FetchAISupportTeam(Team team)
        {
            if (team.SupportTeam != null) return team.SupportTeam;
            return CreateOrUpdateAISupportTeam(team);
        }

        public static List<MechComponentRef> GetOwnedDeploymentBeacons()
        {
            var sgs = UnityGameInstance.BattleTechGame.Simulation;
            var beacons = new List<MechComponentRef>();
            foreach (var stat in ModInit.modSettings.deploymentBeaconEquipment)
            {
                if (sgs.CompanyStats.GetValue<int>(stat) > 0)
                {
                    string[] array = stat.Split(new char[]
                    {
                        '.'
                    });
                    if (string.CompareOrdinal(array[1], "MECHPART") != 0)
                    {
                        BattleTechResourceType battleTechResourceType = (BattleTechResourceType)Enum.Parse(typeof(BattleTechResourceType), array[1]);
                        if (battleTechResourceType != BattleTechResourceType.MechDef && sgs.DataManager.Exists(battleTechResourceType, array[2]))
                        {
                            bool flag = array.Length > 3 && string.Compare(array[3], "DAMAGED", StringComparison.Ordinal) == 0;
                            MechComponentDef componentDef = sgs.GetComponentDef(battleTechResourceType, array[2]);
                            MechComponentRef mechComponentRef = new MechComponentRef(componentDef.Description.Id, sgs.GenerateSimGameUID(), componentDef.ComponentType, ChassisLocations.None, -1, flag ? ComponentDamageLevel.NonFunctional : ComponentDamageLevel.Functional, false);
                            mechComponentRef.SetComponentDef(componentDef);

                            if (mechComponentRef.Def.ComponentTags.All(x => x != "CanSpawnTurret" && x != "CanStrafe")) continue;
                            var id = mechComponentRef.Def.ComponentTags.FirstOrDefault(x =>
                                x.StartsWith("mechdef_") || x.StartsWith("vehicledef_") ||
                                x.StartsWith("turretdef_"));

                            bool consumeOnUse = mechComponentRef.Def.ComponentTags.Any(x => x == "ConsumeOnUse");
                            var pilotID = mechComponentRef.Def.ComponentTags.FirstOrDefault(x =>
                                x.StartsWith("StratOpsPilot_"))
                                ?.Remove(0, 14);

                            if (ModState.DeploymentAssetsStats.All(x => x.ID != id))
                            {
                                var value = sgs.CompanyStats.GetValue<int>(stat);
                                var newStat = new CmdUseStat(id, stat, consumeOnUse, value, value, pilotID);
                                ModState.DeploymentAssetsStats.Add(newStat);
                                ModInit.modLog?.Info?.Write($"Added {id} to deploymentAssetsDict with value {value}.");
                                beacons.Add(mechComponentRef);
                            }
                            var assetStatInfo = ModState.DeploymentAssetsStats.FirstOrDefault(x => x.ID == id);
                            {
                                if (assetStatInfo != null)
                                {
                                    if (assetStatInfo.contractUses > 0)
                                    {
                                        beacons.Add(mechComponentRef);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return beacons;
        }

        public static List<MechComponentRef> GetOwnedDeploymentBeaconsOfByTypeAndTag(string type, string tag, string allowedUnitTags)
        {
            var sgs = UnityGameInstance.BattleTechGame.Simulation;
            var beacons = new List<MechComponentRef>();
            foreach (var stat in ModInit.modSettings.deploymentBeaconEquipment)
            {
                if (sgs.CompanyStats.GetValue<int>(stat) > 0)
                {
                    string[] array = stat.Split(new char[]
                    {
                        '.'
                    });
                    if (string.CompareOrdinal(array[1], "MECHPART") != 0)
                    {
                        BattleTechResourceType battleTechResourceType = (BattleTechResourceType)Enum.Parse(typeof(BattleTechResourceType), array[1]);
                        if (battleTechResourceType != BattleTechResourceType.MechDef && sgs.DataManager.Exists(battleTechResourceType, array[2]))
                        {
                            bool flag = array.Length > 3 && string.Compare(array[3], "DAMAGED", StringComparison.Ordinal) == 0;
                            MechComponentDef componentDef = sgs.GetComponentDef(battleTechResourceType, array[2]);
                            MechComponentRef mechComponentRef = new MechComponentRef(componentDef.Description.Id, sgs.GenerateSimGameUID(), componentDef.ComponentType, ChassisLocations.None, -1, flag ? ComponentDamageLevel.NonFunctional : ComponentDamageLevel.Functional, false);
                            mechComponentRef.SetComponentDef(componentDef);

                            if ((tag == "CanSpawnTurret" && mechComponentRef.Def.ComponentTags.All(x => x != "CanSpawnTurret")) || (tag == "CanStrafe" && mechComponentRef.Def.ComponentTags.All(x => x != "CanStrafe"))) continue;

                            bool consumeOnUse = mechComponentRef.Def.ComponentTags.Any(x => x == "ConsumeOnUse");

                            var id = mechComponentRef.Def.ComponentTags.FirstOrDefault(x =>
                                x.StartsWith("mechdef_") || x.StartsWith("vehicledef_") ||
                                x.StartsWith("turretdef_"));
                            if (!id.StartsWith(type))
                            {
//                                ModInit.modLog?.Info?.Write($"{id} != {type}, ignoring.");
                                continue;
                            }

                            if (!string.IsNullOrEmpty(allowedUnitTags) && mechComponentRef.Def.ComponentTags.All(x=>x != allowedUnitTags))
                            {
                                continue;
                            }
                            var pilotID = mechComponentRef.Def.ComponentTags.FirstOrDefault(x =>
                                    x.StartsWith("StratOpsPilot_"))
                                ?.Remove(0, 14);
                            if (ModState.DeploymentAssetsStats.All(x => x.ID != id))
                            {
                                var value = sgs.CompanyStats.GetValue<int>(stat);
                                var newStat = new CmdUseStat(id, stat, consumeOnUse, value, value, pilotID);
                                ModState.DeploymentAssetsStats.Add(newStat);
                                ModInit.modLog?.Info?.Write($"Added {id} to deploymentAssetsDict with value {value}.");
                                beacons.Add(mechComponentRef);
                            }
                            var assetStatInfo = ModState.DeploymentAssetsStats.FirstOrDefault(x => x.ID == id);
                            {
                                if (assetStatInfo != null)
                                {
                                    if (assetStatInfo.contractUses > 0)
                                    {
                                        beacons.Add(mechComponentRef);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return beacons;
        }

        public static void DeployEvasion(AbstractActor actor)
        {
            ModInit.modLog?.Info?.Write($"Adding deploy protection to {actor.DisplayName}.");
            
            if (actor is Turret turret)
            {
                ModInit.modLog?.Info?.Write($"{actor.DisplayName} is a turret, skipping.");
                return;
            }

            if (ModInit.modSettings.deployProtection > 0)
            {
                ModInit.modLog?.Info?.Write($"Adding {ModInit.modSettings.deployProtection} evasion pips");
                actor.EvasivePipsCurrent = ModInit.modSettings.deployProtection;
                //Traverse.Create(actor).Property("EvasivePipsTotal").SetValue(actor.EvasivePipsCurrent);
                actor.EvasivePipsTotal = actor.EvasivePipsCurrent;
                actor.Combat.MessageCenter.PublishMessage(new EvasiveChangedMessage(actor.GUID, actor.EvasivePipsCurrent));
            }
        }

        public static void MountedEvasion(this AbstractActor actor, AbstractActor carrier)
        {
            ModInit.modLog?.Info?.Write($"Adding BA mounted protection to {actor.DisplayName}.");

            if (actor is Turret turret)
            {
                ModInit.modLog?.Info?.Write($"{actor.DisplayName} is a turret, skipping.");
                return;
            }

            var carrierEvasion = carrier.EvasivePipsCurrent;
            ModInit.modLog?.Info?.Write($"Setting evasion to {carrierEvasion} from carrier");
            actor.EvasivePipsCurrent = carrierEvasion;
            actor.EvasivePipsTotal = actor.EvasivePipsCurrent;
            //Traverse.Create(actor).Property("EvasivePipsTotal").SetValue(actor.EvasivePipsCurrent);
        }


        public static void SpawnDebugFlare(Vector3 position, string prefabName, int numPhases)
        {
            var combat = UnityGameInstance.BattleTechGame.Combat;
            position.y = combat.MapMetaData.GetLerpedHeightAt(position, false);
            List<ObjectSpawnData> list = new List<ObjectSpawnData>();
            ObjectSpawnData item = new ObjectSpawnData(prefabName, position, Quaternion.identity, true, false);
            list.Add(item);
            SpawnObjectSequence spawnObjectSequence = new SpawnObjectSequence(combat, list);
            combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(spawnObjectSequence));
            List<ObjectSpawnData> spawnedObjects = spawnObjectSequence.spawnedObjects;
            CleanupObjectSequence eventSequence = new CleanupObjectSequence(combat, spawnedObjects);
            TurnEvent tEvent = new TurnEvent(Guid.NewGuid().ToString(), combat, numPhases, null, eventSequence, default(AbilityDef), false);
            combat.TurnDirector.AddTurnEvent(tEvent);
        }

        public static void SpawnFlares(Ability ability, Vector3 positionA, Vector3 positionB, string prefabName,
            int numFlares, int numPhases, bool IsLocalPlayer)
        {
            if (ability.Combat.ActiveContract.ContractTypeValue.IsSkirmish) return;

            if (ability.Def.specialRules == AbilityDef.SpecialRules.SpawnTurret)
            {
                positionA.y = ability.Combat.MapMetaData.GetLerpedHeightAt(positionA, false);
                List<ObjectSpawnData> listSpawn = new List<ObjectSpawnData>();
                ObjectSpawnData item = new ObjectSpawnData(prefabName, positionA, Quaternion.identity, true, false);
                listSpawn.Add(item);
                SpawnObjectSequence spawnObjectSequenceSpawn = new SpawnObjectSequence(ability.Combat, listSpawn);
                ability.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(spawnObjectSequenceSpawn));
                List<ObjectSpawnData> spawnedObjectsSpawn = spawnObjectSequenceSpawn.spawnedObjects;
                CleanupObjectSequence eventSequenceSpawn = new CleanupObjectSequence(ability.Combat, spawnedObjectsSpawn);
                TurnEvent tEventSpawn = new TurnEvent(Guid.NewGuid().ToString(), ability.Combat, numPhases + 1, null, eventSequenceSpawn, default(AbilityDef), false);
                ability.Combat.TurnDirector.AddTurnEvent(tEventSpawn);
                return;
            }

            Vector3 b = (positionB - positionA) / Math.Max(numFlares - 1, 1);

            Vector3 line = positionB - positionA;
            Vector3 left = Vector3.Cross(line, Vector3.up).normalized;
            Vector3 right = -left;

            var startLeft = positionA + (left * ability.Def.FloatParam1);
            var startRight = positionA + (right * ability.Def.FloatParam1);

            Vector3 vector = positionA;

            vector.y = ability.Combat.MapMetaData.GetLerpedHeightAt(vector, false);
            startLeft.y = ability.Combat.MapMetaData.GetLerpedHeightAt(startLeft, false);
            startRight.y = ability.Combat.MapMetaData.GetLerpedHeightAt(startRight, false);
            List<ObjectSpawnData> list = new List<ObjectSpawnData>();

            //add endcap radii, also for babbies
            var start = vector - b;
            start.y = ability.Combat.MapMetaData.GetLerpedHeightAt(vector, false);
            ObjectSpawnData outsideStartFlare = new ObjectSpawnData(prefabName, start, Quaternion.identity, true, false);
            list.Add(outsideStartFlare);

            for (int i = 0; i < numFlares + 1; i++)
            {
                ObjectSpawnData item = new ObjectSpawnData(prefabName, vector, Quaternion.identity, true, false);
                list.Add(item);
                vector += b;
                vector.y = ability.Combat.MapMetaData.GetLerpedHeightAt(vector, false);
            }

            for (int i = 0; i < numFlares; i++)
            {
                ObjectSpawnData item = new ObjectSpawnData(prefabName, startLeft, Quaternion.identity, true, false);
                list.Add(item);
                startLeft += b;
                startLeft.y = ability.Combat.MapMetaData.GetLerpedHeightAt(startLeft, false);
            }

            for (int i = 0; i < numFlares; i++)
            {
                ObjectSpawnData item =
                    new ObjectSpawnData(prefabName, startRight, Quaternion.identity, true, false);
                list.Add(item);
                startRight += b;
                startRight.y = ability.Combat.MapMetaData.GetLerpedHeightAt(startRight, false);
            }

            SpawnObjectSequence spawnObjectSequence = new SpawnObjectSequence(ability.Combat, list);
            ability.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(spawnObjectSequence));
            List<ObjectSpawnData> spawnedObjects = spawnObjectSequence.spawnedObjects;
            CleanupObjectSequence eventSequence = new CleanupObjectSequence(ability.Combat, spawnedObjects);
 //           if (!IsLocalPlayer) numPhases += 1;
            TurnEvent tEvent = new TurnEvent(Guid.NewGuid().ToString(), ability.Combat, numPhases + 1, null,
                eventSequence, ability.Def, false);
            ability.Combat.TurnDirector.AddTurnEvent(tEvent);
            return;
        }

        public static void NotifyStrafeSequenceComplete(string parentID, string currentID)
        {
            if (ModState.PendingStrafeWaves.ContainsKey(parentID))
            {
                ModInit.modLog?.Info?.Write($"Strafe Sequence with parent {parentID} and ID {currentID} complete. Remaining waves: {ModState.PendingStrafeWaves[parentID].RemainingWaves}");
                if (ModState.PendingStrafeWaves[parentID].RemainingWaves > 0)
                {
                    ModState.PendingStrafeWaves[parentID].RemainingWaves--;
                    InitiateStrafe(parentID, ModState.PendingStrafeWaves[parentID]);
                }
                else
                {
                    ModInit.modLog?.Info?.Write($"Strafe Sequence with parent {parentID} and ID {currentID} complete. No remaining waves, removing from state.");
                    ModState.PendingStrafeWaves.Remove(parentID);
                }
            }
        }

        public static void DP_AnimationComplete(string encounterObjectGUID)
        {
            EncounterLayerParent.EnqueueLoadAwareMessage(new DropshipAnimationCompleteMessage(LanceSpawnerGameLogic.GetDropshipGuid(encounterObjectGUID)));
        }

        public static bool IsAOEStrafe(this MechComponentRef component, bool IsStrafe)
        {
            if (!IsStrafe) return false;
            if (component.Def.ComponentTags.Any(x => x == "IsAOEStrafe")) return true;
            return false;
        }
            
        public static void InitiateStrafe(string parentSequenceID, PendingStrafeWave wave)
        {

            if (wave.ActorResource.StartsWith("mechdef_") || wave.ActorResource.StartsWith("vehicledef_"))
            {
                var customSpawner = new CustomSpawner(parentSequenceID, wave);
                customSpawner.SpawnStrafingUnit();
                return;
            }

            if (false)
            {
                if (wave.ActorResource.StartsWith("mechdef_"))
                {
                    wave.DM.MechDefs.TryGet(wave.ActorResource, out var supportActorMechDef);
                    supportActorMechDef.Refresh();
                    var customEncounterTags = new TagSet(wave.NeutralTeam.EncounterTags);
                    customEncounterTags.Add("SpawnedFromAbility");
                    var supportActorMech = ActorFactory.CreateMech(supportActorMechDef,
                        wave.SupportPilotDef, customEncounterTags, wave.NeutralTeam.Combat,
                        wave.NeutralTeam.GetNextSupportUnitGuid(), "", wave.SupportHeraldryDef);
                    supportActorMech.Init(wave.NeutralTeam.OffScreenPosition, 0f, false);
                    supportActorMech.InitGameRep(null);
                    wave.NeutralTeam.AddUnit(supportActorMech);
                    supportActorMech.AddToTeam(wave.NeutralTeam);
                    supportActorMech.AddToLance(wave.CmdLance);
                    wave.CmdLance.AddUnitGUID(supportActorMech.GUID);
                    supportActorMech.GameRep.gameObject.SetActive(true);
                    supportActorMech.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(
                        wave.Ability.Combat.BattleTechGame, supportActorMech,
                        BehaviorTreeIDEnum.DoNothingTree);
                    var eventID = Guid.NewGuid().ToString();
                    ModInit.modLog?.Info?.Write($"Initializing Strafing Run (wave) with id {eventID}!");
                    TB_StrafeSequence eventSequence =
                        new TB_StrafeSequence(parentSequenceID, eventID, supportActorMech, wave.PositionA,
                            wave.PositionB, wave.Radius, wave.Team, ModState.IsStrafeAOE, wave.Ability.Def.IntParam1);
                    TurnEvent tEvent = new TurnEvent(eventID, wave.Ability.Combat,
                        wave.Ability.Def.ActivationETA, null, eventSequence, wave.Ability.Def, false);
                    wave.Ability.Combat.TurnDirector.AddTurnEvent(tEvent);


                    if (wave.Team.IsLocalPlayer && (ModInit.modSettings.commandUseCostsMulti > 0 ||
                                                    wave.Ability.Def.getAbilityDefExtension().CBillCost > 0))
                    {
                        var unitName = "";
                        var unitCost = 0;
                        var unitID = "";
                        unitName = supportActorMechDef.Description.UIName;
                        unitID = supportActorMechDef.Description.Id;
                        unitCost = supportActorMechDef.Chassis.Description.Cost;
                        ModInit.modLog?.Info?.Write($"Usage cost will be {unitCost}");

                        if (ModState.CommandUses.All(x => x.UnitID != wave.ActorResource))
                        {

                            var commandUse = new CmdUseInfo(unitID, wave.Ability.Def.Description.Name, unitName,
                                unitCost, wave.Ability.Def.getAbilityDefExtension().CBillCost);

                            ModState.CommandUses.Add(commandUse);
                            ModInit.modLog?.Info?.Write(
                                $"Added usage cost for {commandUse.CommandName} - {commandUse.UnitName}");
                        }
                        else
                        {
                            var cmdUse = ModState.CommandUses.FirstOrDefault(x => x.UnitID == wave.ActorResource);
                            if (cmdUse == null)
                            {
                                ModInit.modLog?.Info?.Write($"ERROR: cmdUseInfo was null");
                            }
                            else
                            {
                                cmdUse.UseCount += 1;
                                ModInit.modLog?.Info?.Write(
                                    $"Added usage cost for {cmdUse.CommandName} - {cmdUse.UnitName}, used {cmdUse.UseCount} times");
                            }
                        }
                    }
                }

                else if (wave.ActorResource.StartsWith("vehicledef_"))
                {
                    wave.DM.VehicleDefs.TryGet(wave.ActorResource, out var supportActorVehicleDef);
                    supportActorVehicleDef.Refresh();
                    var customEncounterTags = new TagSet(wave.NeutralTeam.EncounterTags);
                    customEncounterTags.Add("SpawnedFromAbility");
                    var supportActorVehicle = ActorFactory.CreateVehicle(supportActorVehicleDef,
                        wave.SupportPilotDef, customEncounterTags, wave.NeutralTeam.Combat,
                        wave.NeutralTeam.GetNextSupportUnitGuid(), "", wave.SupportHeraldryDef);
                    supportActorVehicle.Init(wave.NeutralTeam.OffScreenPosition, 0f, false);
                    supportActorVehicle.InitGameRep(null);
                    wave.NeutralTeam.AddUnit(supportActorVehicle);
                    supportActorVehicle.AddToTeam(wave.NeutralTeam);
                    supportActorVehicle.AddToLance(wave.CmdLance);
                    wave.CmdLance.AddUnitGUID(supportActorVehicle.GUID);
                    supportActorVehicle.GameRep.gameObject.SetActive(true);
                    supportActorVehicle.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(
                        wave.Ability.Combat.BattleTechGame, supportActorVehicle,
                        BehaviorTreeIDEnum.DoNothingTree);

                    var eventID = Guid.NewGuid().ToString();
                    ModInit.modLog?.Info?.Write($"Initializing Strafing Run (wave) with id {eventID}!");
                    TB_StrafeSequence eventSequence =
                        new TB_StrafeSequence(parentSequenceID, eventID, supportActorVehicle, wave.PositionA,
                            wave.PositionB, wave.Radius, wave.Team, ModState.IsStrafeAOE, wave.Ability.Def.IntParam1);
                    TurnEvent tEvent = new TurnEvent(eventID, wave.Ability.Combat,
                        wave.Ability.Def.ActivationETA, null, eventSequence, wave.Ability.Def, false);
                    wave.Ability.Combat.TurnDirector.AddTurnEvent(tEvent);

                    if (wave.Team.IsLocalPlayer && (ModInit.modSettings.commandUseCostsMulti > 0 ||
                                                    wave.Ability.Def.getAbilityDefExtension().CBillCost > 0))
                    {
                        var unitName = "";
                        var unitCost = 0;
                        var unitID = "";

                        unitName = supportActorVehicleDef.Description.UIName;
                        unitID = supportActorVehicleDef.Description.Id;
                        unitCost = supportActorVehicleDef.Chassis.Description.Cost;
                        ModInit.modLog?.Info?.Write(
                            $"Usage cost will be {unitCost} + {wave.Ability.Def.getAbilityDefExtension().CBillCost}");


                        if (ModState.CommandUses.All(x => x.UnitID != wave.ActorResource))
                        {

                            var commandUse = new CmdUseInfo(unitID, wave.Ability.Def.Description.Name, unitName,
                                unitCost, wave.Ability.Def.getAbilityDefExtension().CBillCost);

                            ModState.CommandUses.Add(commandUse);
                            ModInit.modLog?.Info?.Write(
                                $"Added usage cost for {commandUse.CommandName} - {commandUse.UnitName}");
                        }
                        else
                        {
                            var cmdUse = ModState.CommandUses.FirstOrDefault(x => x.UnitID == wave.ActorResource);
                            if (cmdUse == null)
                            {
                                ModInit.modLog?.Info?.Write($"ERROR: cmdUseInfo was null");
                            }
                            else
                            {
                                cmdUse.UseCount += 1;
                                ModInit.modLog?.Info?.Write(
                                    $"Added usage cost for {cmdUse.CommandName} - {cmdUse.UnitName}, used {cmdUse.UseCount} times");
                            }
                        }
                    }
                }
            }
            else if (wave.ActorResource.StartsWith("turretdef_"))
            {
                wave.DM.TurretDefs.TryGet(wave.ActorResource, out var supportActorTurretDef);
                supportActorTurretDef.Refresh();
                var customEncounterTags = new TagSet(wave.NeutralTeam.EncounterTags);
                customEncounterTags.Add("SpawnedFromAbility");
                var supportActorTurret = ActorFactory.CreateTurret(supportActorTurretDef,
                    wave.SupportPilotDef, customEncounterTags, wave.NeutralTeam.Combat,
                    wave.NeutralTeam.GetNextSupportUnitGuid(), "", wave.SupportHeraldryDef);
                supportActorTurret.Init(wave.NeutralTeam.OffScreenPosition, 0f, false);
                supportActorTurret.InitGameRep(null);
                wave.NeutralTeam.AddUnit(supportActorTurret);
                supportActorTurret.AddToTeam(wave.NeutralTeam);
                supportActorTurret.AddToLance(wave.CmdLance);
                wave.CmdLance.AddUnitGUID(supportActorTurret.GUID);
                supportActorTurret.GameRep.gameObject.SetActive(true);
                supportActorTurret.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(
                    wave.Ability.Combat.BattleTechGame, supportActorTurret,
                    BehaviorTreeIDEnum.DoNothingTree);

                var eventID = Guid.NewGuid().ToString();
                ModInit.modLog?.Info?.Write($"Initializing Strafing Run (wave) with id {eventID}!");
                TB_StrafeSequence eventSequence =
                    new TB_StrafeSequence(parentSequenceID, eventID, supportActorTurret, wave.PositionA, wave.PositionB, wave.Radius, wave.Team, ModState.IsStrafeAOE, wave.Ability.Def.IntParam1);
                TurnEvent tEvent = new TurnEvent(eventID, wave.Ability.Combat,
                    wave.Ability.Def.ActivationETA, null, eventSequence, wave.Ability.Def, false);
                wave.Ability.Combat.TurnDirector.AddTurnEvent(tEvent);

                if (wave.Team.IsLocalPlayer && (ModInit.modSettings.commandUseCostsMulti > 0 ||
                                                wave.Ability.Def.getAbilityDefExtension().CBillCost > 0))
                {
                    var unitName = "";
                    var unitCost = 0;
                    var unitID = "";

                    unitName = supportActorTurretDef.Description.UIName;
                    unitID = supportActorTurretDef.Description.Id;
                    unitCost = supportActorTurretDef.Chassis.Description.Cost;


                    if (ModState.CommandUses.All(x => x.UnitID != wave.ActorResource))
                    {

                        var commandUse = new CmdUseInfo(unitID, wave.Ability.Def.Description.Name, unitName,
                            unitCost, wave.Ability.Def.getAbilityDefExtension().CBillCost);

                        ModState.CommandUses.Add(commandUse);
                        ModInit.modLog?.Info?.Write(
                            $"Added usage cost for {commandUse.CommandName} - {commandUse.UnitName}. UnitUseCost (unadjusted): {unitCost}. wave.Ability Use Cost: {wave.Ability.Def.getAbilityDefExtension().CBillCost}");
                    }
                    else
                    {
                        var cmdUse = ModState.CommandUses.FirstOrDefault(x => x.UnitID == wave.ActorResource);
                        if (cmdUse == null)
                        {
                            ModInit.modLog?.Info?.Write($"ERROR: cmdUseInfo was null");
                        }
                        else
                        {
                            cmdUse.UseCount += 1;
                            ModInit.modLog?.Info?.Write(
                                $"Added usage cost for {cmdUse.CommandName} - {cmdUse.UnitName}. UnitUseCost (unadjusted): {unitCost}. wave.Ability Use Cost: {wave.Ability.Def.getAbilityDefExtension().CBillCost}. Now used {cmdUse.UseCount} times");
                        }
                    }
                }
            }
        }
        public static void PerformAttackStrafe(this TerrainAttackDeligate del, TB_StrafeSequence strafeSequence)
        {
            MechRepresentation gameRep = del.actor.GameRep as MechRepresentation;
            bool flag = gameRep != null;
            if (flag)
            {
                Log.LogWrite("ToggleRandomIdles false\n", false);
                gameRep.ToggleRandomIdles(false);
            }
            
            //del.HUD.SelectionHandler.DeselectActor(del.HUD.SelectionHandler.SelectedActor);
            //del.HUD.MechWarriorTray.HideAllChevrons();
            CombatSelectionHandler_TrySelectActor.SelectionForbidden = true;
            int seqId = del.actor.Combat.StackManager.NextStackUID;
            bool flag2 = del.actor.GUID == del.target.GUID;
            if (flag2)
            {
                Log.LogWrite("Registering terrain attack to " + seqId + "\n", false);
                del.actor.addTerrainHitPosition(del.targetPosition, del.LOFLevel < LineOfFireLevel.LOFObstructed);
            }
            else
            {
                Log.LogWrite("Registering friendly attack to " + seqId + "\n", false);
            }
            AttackDirector.AttackSequence attackSequence = del.actor.Combat.AttackDirector.CreateAttackSequence(seqId, del.actor, del.target, del.actor.CurrentPosition, del.actor.CurrentRotation, 0, del.weaponsList, MeleeAttackType.NotSet, 0, false);
            strafeSequence.attackSequences.Add(attackSequence.id);
            attackSequence.indirectFire = (del.LOFLevel < LineOfFireLevel.LOFObstructed);
            del.actor.Combat.AttackDirector.PerformAttack(attackSequence);
            attackSequence.ResetWeapons();
        }

        public static void ApplyCreatorEffects(this Ability ability, AbstractActor creator)
        {
            for (int i = 0; i < ability.Def.EffectData.Count; i++)
            {
                if (ability.Def.EffectData[i].targetingData.effectTriggerType == EffectTriggerType.OnActivation && ability.Def.EffectData[i].targetingData.effectTargetType == EffectTargetType.Creator)
                {
                    if (ability.Def.EffectData[i].effectType == EffectType.VFXEffect)
                    {
                        if (false)
                        {
                            List<ObjectSpawnData> list = new List<ObjectSpawnData>();
                            var pos = creator.CurrentPosition + Vector3.up;
                            ObjectSpawnData item = new ObjectSpawnData(ability.Def.EffectData[i].vfxData.vfxName, pos,
                                Quaternion.identity, true, false);
                            list.Add(item);

                            var duration = ability.Def.EffectData[i].durationData.duration;

                            SpawnObjectSequence spawnObjectSequence = new SpawnObjectSequence(creator.Combat, list);
                            creator.Combat.MessageCenter.PublishMessage(
                                new AddSequenceToStackMessage(spawnObjectSequence));
                            List<ObjectSpawnData> spawnedObjects = spawnObjectSequence.spawnedObjects;
                            CleanupObjectSequence cleanupSequence =
                                new CleanupObjectSequence(creator.Combat, spawnedObjects);
                            TurnEvent tEvent = new TurnEvent(Guid.NewGuid().ToString(), creator.Combat, duration, null,
                                cleanupSequence, ability.Def, false);
                            creator.Combat.TurnDirector.AddTurnEvent(tEvent);
                        }

                        var hitInfo = default(WeaponHitInfo);
                        hitInfo.numberOfShots = 1;
                        hitInfo.hitLocations = new int[1];
                        hitInfo.hitLocations[0] = 8;
                        hitInfo.hitPositions = new Vector3[1];

                        var attackPos = creator.GameRep.transform.position + Vector3.up * 100f;
                        var impactPos = creator.getImpactPositionSimple(attackPos, 8) + Vector3.up * 10f;

                        hitInfo.hitPositions[0] = impactPos;
                        hitInfo.attackerId = creator.GUID;
                        hitInfo.targetId = creator.GUID;
                        creator.Combat.EffectManager.CreateEffect(ability.Def.EffectData[i], ability.Def.EffectData[i].Description.Id, 0, creator, creator, hitInfo, 0, false);
                    }
                    else
                    {
                        //creator.Combat.EffectManager.CreateEffect(ability.Def.EffectData[i], ability.Def.EffectData[i].Description.Id, 0, creator, creator, default(WeaponHitInfo), 0, false);
                        creator.CreateEffect(ability.Def.EffectData[i], ability,
                            ability.Def.EffectData[i].Description.Id,
                            -1, creator);
                    }
                }
            }
        }
        
        public static MethodInfo _activateSpawnTurretMethod = AccessTools.Method(typeof(Ability), "ActivateSpawnTurret");
        public static MethodInfo _activateStrafeMethod = AccessTools.Method(typeof(Ability), "ActivateStrafe");
        public static MethodInfo _despawnActorMethod = AccessTools.Method(typeof(AbstractActor), "DespawnActor");
    }
}
