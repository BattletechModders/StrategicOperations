using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using BattleTech.Data;
using HBS.Collections;
using IRBTModUtils.CustomInfluenceMap;
using IRBTModUtils.Extension;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace StrategicOperations.Framework
{
    public class Classes
    {
        public class BA_Spawner
        {
            public AbstractActor Actor;
            public string ChosenBA;
            public Lance BaLance;
            public DataManager DM;
            public MechDef NewBattleArmorDef;
            public PilotDef NewPilotDef;
            public Team TeamSelection;
            public TagSet CustomEncounterTags;

            public BA_Spawner(AbstractActor actor, string chosenBA, Lance BaLance)
            {
                this.Actor = actor;
                this.ChosenBA = chosenBA;
                this.BaLance = BaLance;
                this.DM = UnityGameInstance.BattleTechGame.DataManager;
            }

            public void OnBADepsFailed()
            {
                ModInit.modLog.LogTrace($"Failed to load BA Dependencies for {ChosenBA}. This shouldnt happen!");
            }
            public void OnBADepsLoaded()
            {
                var newBattleArmor = ActorFactory.CreateMech(NewBattleArmorDef, NewPilotDef,
                    CustomEncounterTags, TeamSelection.Combat,
                    TeamSelection.GetNextSupportUnitGuid(), "", Actor.team.HeraldryDef);
                newBattleArmor.Init(Actor.CurrentPosition, Actor.CurrentRotation.eulerAngles.y, false);
                newBattleArmor.InitGameRep(null);
                TeamSelection.AddUnit(newBattleArmor);
                newBattleArmor.AddToTeam(TeamSelection);
                newBattleArmor.AddToLance(BaLance);
                BaLance.AddUnitGUID(newBattleArmor.GUID);
                newBattleArmor.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(
                    Actor.Combat.BattleTechGame, newBattleArmor, BehaviorTreeIDEnum.CoreAITree);
                newBattleArmor.OnPositionUpdate(Actor.CurrentPosition, Actor.CurrentRotation, -1, true, null, false);
                newBattleArmor.DynamicUnitRole = UnitRole.Brawler;
                UnitSpawnedMessage message = new UnitSpawnedMessage("FROM_ABILITY", newBattleArmor.GUID);
                Actor.Combat.MessageCenter.PublishMessage(message);

                Actor.MountBattleArmorToChassis(newBattleArmor);
                newBattleArmor.TeleportActor(Actor.CurrentPosition);

                ModState.PositionLockMount.Add(newBattleArmor.GUID, Actor.GUID);
                ModInit.modLog.LogMessage(
                    $"[SpawnBattleArmorAtActor] Added PositionLockMount with rider  {newBattleArmor.DisplayName} {newBattleArmor.GUID} and carrier {Actor.DisplayName} {Actor.GUID}.");
            }
            public void SpawnBattleArmorAtActor()
            {
                LoadRequest loadRequest = DM.CreateLoadRequest();
                loadRequest.AddBlindLoadRequest(BattleTechResourceType.MechDef, ChosenBA);
                ModInit.modLog.LogMessage($"Added loadrequest for MechDef: {ChosenBA}");
                loadRequest.ProcessRequests(1000U);

                var instanceGUID =
                    $"{Actor.Description.Id}_{Actor.team.Name}_{ChosenBA}";

                if (Actor.Combat.TurnDirector.CurrentRound <= 1)
                {
                    if (ModState.DeferredInvokeBattleArmor.All(x => x.Key != instanceGUID) && !ModState.DeferredBattleArmorSpawnerFromDelegate)
                    {
                        ModInit.modLog.LogTrace(
                            $"Deferred BA Spawner missing, creating delegate and returning. Delegate should spawn {ChosenBA}");

                        void DeferredInvokeBASpawn() =>
                            SpawnBattleArmorAtActor();

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

                TeamSelection = Actor.team;
                var alliedActors = Actor.Combat.AllMechs.Where(x => x.team == Actor.team);
                var chosenpilotSourceMech = alliedActors.GetRandomElement();
                var newPilotDefID = chosenpilotSourceMech.pilot.pilotDef.Description.Id;
                DM.PilotDefs.TryGet(newPilotDefID, out this.NewPilotDef);
                ModInit.modLog.LogMessage($"Attempting to spawn {ChosenBA} with pilot {NewPilotDef.Description.Callsign}.");
                DM.MechDefs.TryGet(ChosenBA, out NewBattleArmorDef);
                NewBattleArmorDef.Refresh();
                //var injectedDependencyLoadRequest = new DataManager.InjectedDependencyLoadRequest(dm);
                //newBattleArmorDef.GatherDependencies(dm, injectedDependencyLoadRequest, 1000U);
                //newBattleArmorDef.Refresh();
                CustomEncounterTags = new TagSet(TeamSelection.EncounterTags) {"SpawnedFromAbility"};

                if (!NewBattleArmorDef.DependenciesLoaded(1000U) || !NewPilotDef.DependenciesLoaded(1000U))
                {
                    DataManager.InjectedDependencyLoadRequest dependencyLoad = new DataManager.InjectedDependencyLoadRequest(DM);
                    dependencyLoad.RegisterLoadCompleteCallback(new Action(this.OnBADepsLoaded));
                    dependencyLoad.RegisterLoadFailedCallback(new Action(this.OnBADepsFailed)); // do failure handling here
                    if (!NewBattleArmorDef.DependenciesLoaded(1000U))
                    {
                        NewBattleArmorDef.GatherDependencies(DM, dependencyLoad, 1000U);
                    }
                    if (!NewPilotDef.DependenciesLoaded(1000U))
                    {
                        NewPilotDef.GatherDependencies(DM, dependencyLoad, 1000U);
                    }
                    DM.InjectDependencyLoader(dependencyLoad, 1000U);
                    return;
                }
                this.OnBADepsLoaded();
            }
        }
        
        public class BA_FactionAssoc
        {
            public List<string> FactionIDs = new List<string>();
            public float SpawnChanceBase = 0f;
            public float SpawnChanceDiffMod = 0f;
            public int MaxSquadsPerContract = 0;
            public Dictionary<string, int> InternalBattleArmorWeight = new Dictionary<string, int>();
            public Dictionary<string, int> MountedBattleArmorWeight = new Dictionary<string, int>();
            public Dictionary<string, int> HandsyBattleArmorWeight = new Dictionary<string, int>();
        }
        public class BA_DamageTracker
        {
            public string TargetGUID = ""; // guid of carrier unit.
            public bool IsSquadInternal;
            public Dictionary<int, int> BA_MountedLocations = new Dictionary<int, int>(); // key is BA unit chassis location (HD, CT, LT, RT, LA, RA), value is BA mounted ARMOR location on carrier.

            public BA_DamageTracker()
            {
                this.TargetGUID = "";
                this.IsSquadInternal = false;
                this.BA_MountedLocations = new Dictionary<int, int>();
            }

            public BA_DamageTracker(string targetGUID, bool internalSquad, Dictionary<int, int> mountedLocations)
            {
                this.TargetGUID = targetGUID;
                this.IsSquadInternal = internalSquad;
                this.BA_MountedLocations = mountedLocations;
            }
        }

        public class BA_TargetEffect
        {
            public string ID = "";
            public string Name = "";
            public string Description = "";

            [JsonIgnore]
            public List<EffectData> effects = new List<EffectData>();
            public List<JObject> effectDataJO = new List<JObject>();

        }
        public class SpawnCoords
        {
            public string ID;
            public Vector3 Loc;
            public float DistFromTarget;

            public SpawnCoords(string id, Vector3 loc, float distFromTarget)
            {
                this.ID = id;
                this.Loc = loc;
                this.DistFromTarget = distFromTarget;
            }
        }
        public class ColorSetting
        {
            public int r;
            public int g;
            public int b;

            public float Rf => r / 255f;
            public float Gf => g / 255f;
            public float Bf => b / 255f;
        }
        public class CmdUseStat
        {
            public string ID;
            public string stat;
            public string pilotID;
            public bool consumeOnUse;
            public int contractUses;
            public int simStatCount;

            public CmdUseStat(string id, string stat, bool consumeOnUse, int contractUses, int simStatCount, string pilotId = null)
            {
                this.ID = id;
                this.stat = stat;
                this.pilotID = pilotId;
                this.consumeOnUse = consumeOnUse;
                this.contractUses = contractUses;
                this.simStatCount = simStatCount;
            }
        }
        public class CmdUseInfo
        {
            public string UnitID;
            public string CommandName;
            public string UnitName;
            public int UseCost;
            public int AbilityUseCost;
            public int UseCostAdjusted => Mathf.RoundToInt((UseCost * ModInit.modSettings.commandUseCostsMulti) + AbilityUseCost);
            public int UseCount;
            public int TotalCost => UseCount * UseCostAdjusted;

            public CmdUseInfo(string unitId, string commandName, string unitName, int useCost, int abilityUseCost)
            {
                this.UnitID = unitId;
                this.CommandName = commandName;
                this.UnitName = unitName;
                this.UseCost = useCost;
                this.AbilityUseCost = abilityUseCost;
                this.UseCount = 1;
            }
        }

        public class AI_DealWithBAInvocation
        {
            public Ability ability;
            public AbstractActor targetActor;
            public bool active;

            public AI_DealWithBAInvocation()
            {
                this.ability = default(Ability);
                this.targetActor = default(AbstractActor);
                this.active = false;
            }
            public AI_DealWithBAInvocation(Ability cmdAbility, AbstractActor targetActor, bool active)
            {
                this.ability = cmdAbility;
                this.targetActor = targetActor;
                this.active = active;
            }
        }

        public class BA_MountOrSwarmInvocation
        {
            public Ability ability;
            public AbstractActor targetActor;
            public bool active;

            public BA_MountOrSwarmInvocation()
            {
                this.ability = default(Ability);
                this.targetActor = default(AbstractActor);
                this.active = false;
            }
            public BA_MountOrSwarmInvocation(Ability cmdAbility, AbstractActor targetActor, bool active)
            {
                this.ability = cmdAbility;
                this.targetActor = targetActor;
                this.active = active;
            }
        }

        public class AI_CmdInvocation
        {
            public Ability ability;
            public Vector3 vectorOne;
            public Vector3 vectorTwo;
            public bool active;
            public float dist;

            public AI_CmdInvocation()
            {
                this.ability = default(Ability);
                this.vectorOne = new Vector3();
                this.vectorTwo = new Vector3();
                this.active = false;
                this.dist = Vector3.Distance(vectorOne, vectorTwo);
            }
            public AI_CmdInvocation(Ability cmdAbility, Vector3 firstVector, Vector3 secondVector, bool active)
            {
                this.ability = cmdAbility;
                this.vectorOne = firstVector;
                this.vectorTwo = secondVector;
                this.active = active;
                this.dist = Vector3.Distance(vectorOne, vectorTwo);
            }
        }
        public class AI_SpawnBehavior
        {
            public string Tag;
            public string Behavior;
            public int MinRange;

            public AI_SpawnBehavior()
            {
                Tag = "DEFAULT";
                Behavior = "DEFAULT";
                MinRange = 50;
            }
        }
        public class AI_FactionCommandAbilitySetting
        {
            public string AbilityDefID;
            public float AddChance;
            public float DiffMod;
        }

        public class PendingStrafeWave
        {
            public int RemainingWaves;
            public Ability Ability;
            public Team Team;
            public Vector3 PositionA;
            public Vector3 PositionB;
            public float Radius;
            public string ActorResource;
            public Team NeutralTeam;
            public Lance CmdLance;
            public PilotDef SupportPilotDef;
            public HeraldryDef SupportHeraldryDef;
            public DataManager DM;

            public PendingStrafeWave(int remainingWaves, Ability ability, Team team, Vector3 positionA, Vector3 positionB, float radius, string actorResource, Team neutralTeam, Lance cmdLance, PilotDef supportPilotDef, HeraldryDef supportHeraldryDef, DataManager dm)
            {
                this.RemainingWaves = remainingWaves;
                this.Ability = ability;
                this.Team = team;
                this.PositionA = positionA;
                this.PositionB = positionB;
                this.Radius = radius;
                this.ActorResource = actorResource;
                this.NeutralTeam = neutralTeam;
                this.CmdLance = cmdLance;
                this.SupportPilotDef = supportPilotDef;
                this.SupportHeraldryDef = supportHeraldryDef;
                this.DM = dm;
            }
        }
    }
}
