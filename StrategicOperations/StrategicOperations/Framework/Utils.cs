using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using BattleTech;
using BattleTech.Data;
using BattleTech.Framework;
using BattleTech.Rendering;
using Harmony;
using UnityEngine;
using static StrategicOperations.Framework.Classes;

namespace StrategicOperations.Framework
{
    public static class Utils
    {
        public static object FetchUnitFromDM(this DataManager dm, string id)
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

        public static List<AbstractActor> GetAllDetectedEnemies(this SharedVisibilityCache cache, AbstractActor actor)
        {
            var detectedEnemies = new List<AbstractActor>();
            foreach (var enemy in actor.Combat.AllActors)
            {
                if (cache.CachedVisibilityToTarget(enemy).VisibilityLevel > 0 && actor.Combat.HostilityMatrix.IsEnemy(actor.team, enemy.team))
                {
                    detectedEnemies.Add(enemy);
                }
            }
            return detectedEnemies;
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
                ModInit.modLog.LogTrace($"Distance from possibleStart to ray endpoint is {Vector3.Distance(start, newPos)}.");
            }

            return vectors.ToArray();
        }

        public static Rect[] MakeRectangle(Vector3 start, Vector3 end, float width)
        {
            
            var rectangles = new List<Rect>();
            Vector3 line = end - start;
            float length = Vector3.Distance(start, end);
            ModInit.modLog.LogTrace($"Rectangle length should be {length}.");
            Vector3 left = Vector3.Cross(line, Vector3.up).normalized;
            Vector3 right = -left;
            var startLeft = start + (left * width);
            var startRight = start + (right * width);
            var rectLeft = new Rect(startLeft.x, startLeft.y, width, length);
            var rectRight = new Rect(startRight.x, startRight.y, width, length);
            rectangles.Add(rectLeft);
            rectangles.Add(rectRight);
            return rectangles.ToArray();
        }

        public static Vector3 LerpByDistance(Vector3 start, Vector3 end, float x)
        {
            return x * Vector3.Normalize(end - start) + start;
        }

        public static T GetRandomFromList<T>(List<T> list)
        {
            if (list.Count == 0) return (T) new object();
            var idx = UnityEngine.Random.Range(0, list.Count);
            return list[idx];
        }

        public static HeraldryDef SwapHeraldryColors(HeraldryDef def, DataManager dataManager, Action loadCompleteCallback = null)
        {
            var secondaryID = def.primaryMechColorID;
            var tertiaryID = def.secondaryMechColorID;
            var primaryID = def.tertiaryMechColorID;

            ModInit.modLog.LogMessage($"Creating new heraldry for support. {primaryID} was tertiary, now primary. {secondaryID} was primary, now secondary. {tertiaryID} was secondary, now tertiary.");
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
        public static Lance CreateCMDLance(Team team)
        {
            Lance lance = new Lance(team, new LanceSpawnerRef[0]);
            var lanceGuid = LanceSpawnerGameLogic.GetLanceGuid(Guid.NewGuid().ToString());
            lance.lanceGuid = lanceGuid;
            var combat = UnityGameInstance.BattleTechGame.Combat;
            combat.ItemRegistry.AddItem(lance);
            team.lances.Add(lance);
            ModInit.modLog.LogMessage($"Created lance {lance.DisplayName} for Team {team.DisplayName}.");
            return lance;
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

        public static void CreateOrUpdateAISupportTeam()
        {
            AITeam aiteam = null;
            var combat = UnityGameInstance.BattleTechGame.Combat;
            aiteam = new AITeam("Opfor Support", Color.black, Guid.NewGuid().ToString(), true, combat);
            combat.TurnDirector.AddTurnActor(aiteam);
            combat.ItemRegistry.AddItem(aiteam);
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

                            if (ModState.deploymentAssetsStats.All(x => x.ID != id))
                            {
                                var value = sgs.CompanyStats.GetValue<int>(stat);
                                var newStat = new CmdUseStat(id, stat, consumeOnUse, value, value, pilotID);
                                ModState.deploymentAssetsStats.Add(newStat);
                                ModInit.modLog.LogMessage($"Added {id} to deploymentAssetsDict with value {value}.");
                                beacons.Add(mechComponentRef);
                            }
                            var assetStatInfo = ModState.deploymentAssetsStats.FirstOrDefault(x => x.ID == id);
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
//                                ModInit.modLog.LogMessage($"{id} != {type}, ignoring.");
                                continue;
                            }

                            if (!string.IsNullOrEmpty(allowedUnitTags) && mechComponentRef.Def.ComponentTags.All(x=>x != allowedUnitTags))
                            {
                                continue;
                            }
                            var pilotID = mechComponentRef.Def.ComponentTags.FirstOrDefault(x =>
                                    x.StartsWith("StratOpsPilot_"))
                                ?.Remove(0, 14);
                            if (ModState.deploymentAssetsStats.All(x => x.ID != id))
                            {
                                var value = sgs.CompanyStats.GetValue<int>(stat);
                                var newStat = new CmdUseStat(id, stat, consumeOnUse, value, value, pilotID);
                                ModState.deploymentAssetsStats.Add(newStat);
                                ModInit.modLog.LogMessage($"Added {id} to deploymentAssetsDict with value {value}.");
                                beacons.Add(mechComponentRef);
                            }
                            var assetStatInfo = ModState.deploymentAssetsStats.FirstOrDefault(x => x.ID == id);
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
            ModInit.modLog.LogMessage($"Adding deploy protection to {actor.DisplayName}.");
            
                if (actor is Turret turret)
                {
                    ModInit.modLog.LogMessage($"{actor.DisplayName} is a turret, skipping.");
                    return;
                }

                if (ModInit.modSettings.deployProtection > 0)
                {
                    ModInit.modLog.LogMessage($"Adding {ModInit.modSettings.deployProtection} evasion pips");
                    actor.EvasivePipsCurrent = ModInit.modSettings.deployProtection;
                    Traverse.Create(actor).Property("EvasivePipsTotal").SetValue(actor.EvasivePipsCurrent);
                }
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
            for (int i = 0; i < numFlares; i++)
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

        public static void DP_AnimationComplete(string encounterObjectGUID)
        {
            EncounterLayerParent.EnqueueLoadAwareMessage(new DropshipAnimationCompleteMessage(LanceSpawnerGameLogic.GetDropshipGuid(encounterObjectGUID)));
        }

        public static MethodInfo _activateSpawnTurretMethod = AccessTools.Method(typeof(Ability), "ActivateSpawnTurret");
        public static MethodInfo _activateStrafeMethod = AccessTools.Method(typeof(Ability), "ActivateStrafe");
        public static MethodInfo _despawnActorMethod = AccessTools.Method(typeof(AbstractActor), "DespawnActor");
    }
}
