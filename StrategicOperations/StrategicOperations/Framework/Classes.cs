﻿using System;
using System.Collections.Generic;
using System.Linq;
using Abilifier.Patches;
using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using CBTBehaviorsEnhanced.MeleeStates;
using CustomComponents;
using CustomUnits;
using HBS.Collections;
using IRBTModUtils.CustomInfluenceMap;
using IRTweaks.Modules.Combat;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using static MonoMod.Cil.RuntimeILReferenceBag.FastDelegateInvokers;

namespace StrategicOperations.Framework
{
    public class StrategicInfluenceMapFactors
    {
        public class CustomHostileFactors
        {
            public class PreferAvoidStandingInAirstrikeAreaWithHostile : CustomInfluenceMapHostileFactor
            {
                public override bool IgnoreFactorNormalization => true;

                public override string Name => "Prefer not standing in the area of an incoming airstrike";

                public PreferAvoidStandingInAirstrikeAreaWithHostile()
                {
                }

                public override float EvaluateInfluenceMapFactorAtPositionWithHostile(AbstractActor unit, Vector3 position, float angle, MoveType moveType, ICombatant hostileUnit)
                {
                    ModInit.modLog?.Trace?.Write(Name);
                    if (!AI_Utils.IsPositionWithinAnAirstrike(unit, position))
                    {
                        IgnoreFactorNormalization = false;
                        return 1f;
                    }

                    return -9001f;
                }

                public override float GetRegularMoveWeight(AbstractActor actor)
                {
                    return 1f;
                }

                public override float GetSprintMoveWeight(AbstractActor actor)
                {
                    return 1f;
                }
            }

            public class PreferCloserToResupplyWithHostile : CustomInfluenceMapHostileFactor
            {
                public override bool IgnoreFactorNormalization => true;

                public override string Name =>
                    "Units with missing ammo or <60% armor prefer getting within range of resupply";

                public PreferCloserToResupplyWithHostile()
                {
                }

                public override float EvaluateInfluenceMapFactorAtPositionWithHostile(AbstractActor unit, Vector3 position, float angle, MoveType moveType, ICombatant hostileUnit)
                {
                    ModInit.modLog?.Trace?.Write(Name);

                    if (unit.AreAnyWeaponsOutOfAmmo() || unit.SummaryArmorCurrent / unit.StartingArmor <= 0.6f)
                    {
                        var distToResupply = unit.GetDistanceToClosestDetectedResupply(position);
                        if (distToResupply <= -5f)
                        {
                            IgnoreFactorNormalization = false;
                            return 0f;
                        }

                        return 9001 * (1 / distToResupply);
                    }

                    return 0f;
                }

                public override float GetRegularMoveWeight(AbstractActor actor)
                {
                    return 1f;
                }

                public override float GetSprintMoveWeight(AbstractActor actor)
                {
                    return 1f;
                }
            }

            public class PreferNearerToSwarmTargetsWithHostile : CustomInfluenceMapHostileFactor
            {
                public override bool IgnoreFactorNormalization => true;

                public override string Name => "Battle armor and their carriers prefer getting close to enemy units";

                public PreferNearerToSwarmTargetsWithHostile()
                {
                }

                public override float EvaluateInfluenceMapFactorAtPositionWithHostile(AbstractActor unit, Vector3 position, float angle, MoveType moveType, ICombatant hostileUnit)
                {
                    ModInit.modLog?.Trace?.Write(Name);
                    if (!unit.HasMountedUnits() && !unit.CanSwarm())
                    {
                        IgnoreFactorNormalization = false;
                        return 0f;
                    }

                    return 9001 * (1 / unit.DistanceToClosestDetectedEnemy(position));
                }

                public override float GetRegularMoveWeight(AbstractActor actor)
                {
                    return 1f;
                }

                public override float GetSprintMoveWeight(AbstractActor actor)
                {
                    return 1f;
                }
            }
        }

        public class CustomPositionFactors
        {
            public class PreferAvoidStandingInAirstrikeAreaPosition : CustomInfluenceMapPositionFactor
            {
                public override bool IgnoreFactorNormalization => true;

                public override string Name => "Prefer not standing in the area of an incoming airstrike";

                public PreferAvoidStandingInAirstrikeAreaPosition()
                {
                }

                public override float EvaluateInfluenceMapFactorAtPosition(AbstractActor unit, Vector3 position,
                    float angle, MoveType moveType, PathNode pathNode)
                {
                    ModInit.modLog?.Trace?.Write(Name);
                    if (!AI_Utils.IsPositionWithinAnAirstrike(unit, position))
                    {
                        IgnoreFactorNormalization = false;
                        return 1f;
                    }

                    return -9001f;
                }

                public override float GetRegularMoveWeight(AbstractActor actor)
                {
                    return 1f;
                }

                public override float GetSprintMoveWeight(AbstractActor actor)
                {
                    return 1f;
                }
            }

            public class PreferCloserToResupply : CustomInfluenceMapPositionFactor
            {
                public override bool IgnoreFactorNormalization => true;

                public override string Name =>
                    "Units with missing ammo or <60% armor prefer getting within range of resupply";

                public PreferCloserToResupply()
                {
                }

                public override float EvaluateInfluenceMapFactorAtPosition(AbstractActor unit, Vector3 position,
                    float angle, MoveType moveType, PathNode pathNode)
                {
                    ModInit.modLog?.Trace?.Write(Name);

                    if (unit.AreAnyWeaponsOutOfAmmo() || unit.SummaryArmorCurrent / unit.StartingArmor <= 0.6f)
                    {
                        var distToResupply = unit.GetDistanceToClosestDetectedResupply(position);
                        if (distToResupply <= -5f)
                        {
                            IgnoreFactorNormalization = false;
                            return 0f;
                        }

                        return 9001 * (1 / distToResupply);
                    }

                    return 0f;
                }

                public override float GetRegularMoveWeight(AbstractActor actor)
                {
                    return 1f;
                }

                public override float GetSprintMoveWeight(AbstractActor actor)
                {
                    return 1f;
                }
            }

            public class PreferNearerToSwarmTargets : CustomInfluenceMapPositionFactor
            {
                public override bool IgnoreFactorNormalization => true;

                public override string Name => "Battle armor and their carriers prefer getting close to enemy units";

                public PreferNearerToSwarmTargets()
                {
                }

                public override float EvaluateInfluenceMapFactorAtPosition(AbstractActor unit, Vector3 position,
                    float angle, MoveType moveType, PathNode pathNode)
                {
                    ModInit.modLog?.Trace?.Write(Name);
                    if (!unit.HasMountedUnits() && !unit.CanSwarm())
                    {
                        IgnoreFactorNormalization = false;
                        return 0f;
                    }

                    return 9001 * (1 / unit.DistanceToClosestDetectedEnemy(position));
                }

                public override float GetRegularMoveWeight(AbstractActor actor)
                {
                    return 1f;
                }

                public override float GetSprintMoveWeight(AbstractActor actor)
                {
                    return 1f;
                }
            }
        }
    }

    [CustomComponent("InternalAmmoTonnage")]
    public class InternalAmmoTonnage : SimpleCustomComponent
    {
        public float InternalAmmoTons = 0.0f;
    }
    public class Classes
    {
//        public class AirliftWiggleConfig
//        {
//            public string AbilityDefID = "";
//            public float BaseSuccessChance = 0f;
//            public float MaxSuccessChance = 0f;
//            public float PilotingSuccessFactor = 0f;
//            public float PilotingDamageReductionFactor = 0f;
//        }


        public class AI_BeaconProxyInfo
        {
            public int StrafeWaves = 0;
            public string UnitDefID = "";
            public int Weight = 0;
        }

        public class AI_CmdInvocation
        {
            public Ability ability;
            public bool active;
            public float dist;
            public Vector3 vectorOne;
            public Vector3 vectorTwo;

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

        public class AI_DealWithBAInvocation
        {
            public Ability ability;
            public bool active;
            public AbstractActor targetActor;

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

        public class AI_SpawnBehavior
        {
            public string Behavior;
            public int MinRange;
            public string Tag;

            public AI_SpawnBehavior()
            {
                Tag = "DEFAULT";
                Behavior = "DEFAULT";
                MinRange = 50;
            }
        }

        public class AirliftTargetEffect
        {
            public string Description = "";
            public List<JObject> effectDataJO = new List<JObject>();

            [JsonIgnore]
            public List<EffectData> effects = new List<EffectData>();

            public bool FriendlyAirlift;
            public string ID = "";
            public string Name = "";
        }

        public class AirliftTracker
        {
            public string CarrierGUID; // guid of carrier unit.
            public bool IsCarriedInternal;
            public bool IsFriendly; // key is BA unit chassis location (HD, CT, LT, RT, LA, RA), value is BA mounted ARMOR location on carrier.
            public float Offset;

            public AirliftTracker()
            {
                this.CarrierGUID = "";
                this.IsCarriedInternal = false;
                this.IsFriendly = false;
                this.Offset = 0f;
            }

            public AirliftTracker(string targetGUID, bool internalCarry, bool isFriendly, float offset)
            {
                this.CarrierGUID = targetGUID;
                this.IsCarriedInternal = internalCarry;
                this.IsFriendly = isFriendly;
                this.Offset = offset;
            }
        }

        public class BA_DamageTracker
        {
            public Dictionary<int, int> BA_MountedLocations = new Dictionary<int, int>(); // key is BA unit chassis location (HD, CT, LT, RT, LA, RA), value is BA mounted ARMOR location on carrier.
            public bool IsSquadInternal;
            public string TargetGUID = ""; // guid of carrier unit.

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

        public class BA_DeswarmMovementInfo
        {
            public AbstractActor Carrier;
            public List<AbstractActor> SwarmingUnits;

            public BA_DeswarmMovementInfo()
            {
                this.Carrier = null;
                this.SwarmingUnits = new List<AbstractActor>();
            }

            public BA_DeswarmMovementInfo(AbstractActor carrier)
            {
                this.Carrier = carrier;
                this.SwarmingUnits = new List<AbstractActor>();
            }

            public BA_DeswarmMovementInfo(AbstractActor carrier, List<AbstractActor> swarmingUnits)
            {
                this.Carrier = carrier;
                this.SwarmingUnits = swarmingUnits;
            }
        }

        public class BA_FactionAssoc
        {
            public List<string> FactionIDs = new List<string>();
            public Dictionary<string, int> HandsyBattleArmorWeight = new Dictionary<string, int>();
            public Dictionary<string, int> InternalBattleArmorWeight = new Dictionary<string, int>();
            public int MaxSquadsPerContract = 0;
            public Dictionary<string, int> MountedBattleArmorWeight = new Dictionary<string, int>();
            public float SpawnChanceBase = 0f;
            public float SpawnChanceDiffMod = 0f;
        }

        public class BA_GarrisonInfo
        {
            public string BuildingGUID = ""; //guid of building
            public Vector3 OriginalSquadPos = new Vector3(); // original position of squad. when exiting, squad will move here because pathing sucks and i hate it.

            public BA_GarrisonInfo(BattleTech.Building building, TrooperSquad squad)
            {
                BuildingGUID = building.GUID;
                OriginalSquadPos = squad.CurrentPosition;
            }
        }


        public class BA_TargetEffect
        {
            public string Description = "";
            public List<JObject> effectDataJO = new List<JObject>();

            [JsonIgnore]
            public List<EffectData> effects = new List<EffectData>();

            public string ID = "";
            public string Name = "";
            public ConfigOptions.BA_TargetEffectType TargetEffectType = ConfigOptions.BA_TargetEffectType.BOTH;
        }

        public class BAPairingInfo
        {
            public int CapacityInitial;
            public List<string> PairedBattleArmor = new List<string>();

            public BAPairingInfo(int capacityInitial, string pairedBattleArmor = null)
            {
                this.CapacityInitial = capacityInitial;
                if (pairedBattleArmor != null) this.PairedBattleArmor.Add(pairedBattleArmor);
            }
        }

        public class CmdInvocationParams
        {
            public string ActorResource;
            public bool IsStrafeAOE;
            public string PilotOverride;
            public bool PlayerControl;
            public bool PlayerControlOverridden;
            public string QUID = "";
            public AbilityDef.SpecialRules Rules;
            public int StrafeWaves;

            public CmdInvocationParams(int strafeWaves, string actorResource, string pilotOverride,
                AbilityDef.SpecialRules rules, string quid = "", bool isStrafeAOE = false, bool playerControl = false, bool playerControlOverridden = false)
            {
                QUID = quid;
                StrafeWaves = strafeWaves;
                ActorResource = actorResource;
                PilotOverride = pilotOverride;
                Rules = rules;
                IsStrafeAOE = isStrafeAOE;
                PlayerControl = playerControl;
                PlayerControlOverridden = playerControlOverridden;
            }

            public CmdInvocationParams()
            {
                QUID = "";
                StrafeWaves = 0;
                ActorResource = "";
                PilotOverride = "";
                Rules = AbilityDef.SpecialRules.NotSet;
                IsStrafeAOE = false;
                PlayerControl = false;
                PlayerControlOverridden = false;
            }

            public CmdInvocationParams(CmdInvocationParams cmdParams)
            {
                QUID = cmdParams.QUID;
                StrafeWaves = cmdParams.StrafeWaves;
                ActorResource = cmdParams.ActorResource;
                PilotOverride = cmdParams.PilotOverride;
                Rules = cmdParams.Rules;
                IsStrafeAOE = cmdParams.IsStrafeAOE;
                PlayerControl = cmdParams.PlayerControl;
                PlayerControlOverridden = cmdParams.PlayerControlOverridden;
            }
        }

        public class CmdUseInfo
        {
            public int AbilityUseCost;
            public string CommandName;
            public string UnitID;
            public string UnitName;
            public int UseCost;
            public int UseCount;
            public int TotalCost => UseCount * UseCostAdjusted;
            public int UseCostAdjusted => Mathf.RoundToInt((UseCost * ModInit.modSettings.commandUseCostsMulti) + AbilityUseCost);

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

        public class CmdUseStat
        {
            public bool consumeOnUse;
            public int contractUses;
            public string ID;
            public string pilotID;
            public int simStatCount;
            public string stat;

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

        public class ColorSetting
        {
            public int b;
            public int g;
            public int r;
            public float Bf => b / 255f;
            public float Gf => g / 255f;

            public Color ProcessedColor => new Color(Rf, Gf, Bf);
            //public int a;

            public float Rf => r / 255f;
            //public float Af => a / 255f;
        }

        public class ConfigOptions
        {
            public enum BA_TargetEffectType
            {
                MOUNT_INT,
                MOUNT_EXT,
                SWARM,
                GARRISON,
                BOTH,
                MOUNTTARGET,
                SWARMTARGET,
                BOTHTARGET
            }

            public class AI_FactionCommandAbilitySetting
            {
                public string AbilityDefID = "";
                public float AddChance = 0f;
                public List<AI_BeaconProxyInfo> AvailableBeacons = new List<AI_BeaconProxyInfo>();
                public List<string> ContractBlacklist = new List<string>();
                public float DiffMod = 0f;
                public List<string> FactionIDs = new List<string>();
                public int MaxUsersAddedPerContract = 0;
            }

            public class BA_DeswarmAbilityConfig // key will be AbilityDefID
            {
                //public string AbilityDefID = "";
                public float BaseSuccessChance = 0f;
                public int Clusters = 1;
                public int InitPenalty = 0;
                public float MaxSuccessChance = 0f;
                public float PilotingSuccessFactor = 0f;
                public float TotalDamage = 0f;

                public BA_DeswarmAbilityConfig(){}
            }

            public class BA_DeswarmMovementConfig
            {
                public string AbilityDefID = "";
                public float BaseSuccessChance = 0f;
                public float EvasivePipsFactor = 0f;
                public float JumpMovementModifier = 0f;
                public float LocationDamageOverride = 0f;
                public float MaxSuccessChance = 0f;
                public float PilotingDamageReductionFactor = 0f;
                public bool UseDFADamage = false;
                public  BA_DeswarmMovementConfig(){}
            }

            public class BeaconExclusionConfig
            {
                public bool ExcludedAISpawn = true;
                public bool ExcludedAIStrafe = true;
                public bool ExcludedPlayerSpawn = true;
                public bool ExcludedPlayerStrafe = true;
            }

            public class ResupplyConfigOptions
            {
                public float ArmorRepairMax = 0.75f;
                public string ArmorSupplyAmmoDefId = "";
                public float BasePhasesToResupply = 1;
                public List<string> InternalSPAMMYBlackList = new List<string>();
                public string InternalSPAMMYDefId = "";
                public string ResupplyAbilityID = "";
                public string ResupplyIndicatorAsset = "";
                public ColorSetting ResupplyIndicatorColor = new ColorSetting();
                public string ResupplyIndicatorInRangeAsset = "";
                public ColorSetting ResupplyIndicatorInRangeColor = new ColorSetting();
                public float ResupplyPhasesPerAmmoTonnage = 1f;
                public float ResupplyPhasesPerArmorPoint = 0.25f;
                public string ResupplyUnitTag = "";
                public string SPAMMYAmmoDefId = "";
                public List<string> SPAMMYBlackList = new List<string>();
                public Dictionary<string, float> UnitTagFactor = new Dictionary<string, float>();
            }
        }

        public class CustomSpawner
        {
            public AbstractActor Actor;
            public string ChosenPilot;
            public string ChosenUnit;
            public CombatGameState Combat;
            public TagSet CustomEncounterTags;
            public Lance CustomLance;
            public DataManager DM;
            public Vector3 Loc2 = Vector3.zero;
            public PilotDef NewPilotDef;
            public MechDef NewUnitDef;
            public string ParentSequenceIDForStrafe = "";
            public bool PlayerControl = false;
            public Ability SourceAbility;
            public Team SourceTeam;
            public Vector3 SpawnLoc = Vector3.zero;
            public Quaternion SpawnRotation = new Quaternion(0f, 0f, 0f, 0f);
            public PendingStrafeWave StrafeWave;
            public HeraldryDef SupportHeraldryDef;
            public Team TeamSelection;

            public CustomSpawner(CombatGameState combat, AbstractActor actor, string chosen, Lance custLance)
            {
                this.Combat = combat;
                this.Actor = actor;
                this.ChosenUnit = chosen;
                this.CustomLance = custLance;
                this.DM = UnityGameInstance.BattleTechGame.DataManager;
            }

            public CustomSpawner(Team team, Ability ability, CombatGameState combat, string chosen, Lance custLance, Team teamSelection, Vector3 loc, Quaternion rotation, HeraldryDef heraldry, string chosenPilot, bool playerControl)
            {
                this.SourceTeam = team;
                this.SourceAbility = ability;
                this.Combat = combat;
                this.ChosenUnit = chosen;
                this.CustomLance = custLance;
                this.TeamSelection = teamSelection;
                this.DM = UnityGameInstance.BattleTechGame.DataManager;
                this.SpawnLoc = loc;
                this.SpawnRotation = rotation;
                this.SupportHeraldryDef = heraldry;
                this.ChosenPilot = chosenPilot;
                this.PlayerControl = playerControl;
            }

            public CustomSpawner(string parentSequenceID, PendingStrafeWave wave)
            {
                this.StrafeWave = wave;
                this.Combat = wave.Ability.Combat;
                this.ChosenUnit = wave.ActorResource;
                this.CustomLance = wave.CmdLance;
                this.TeamSelection = wave.NeutralTeam;
                this.DM = UnityGameInstance.BattleTechGame.DataManager;
                this.SpawnLoc = wave.PositionA;
                this.SupportHeraldryDef = wave.SupportHeraldryDef;
                this.NewPilotDef = wave.SupportPilotDef;
                this.ParentSequenceIDForStrafe = parentSequenceID;
            }

            public void OnBADepsLoaded()
            {
                var newBattleArmor = ActorFactory.CreateMech(NewUnitDef, NewPilotDef,
                    CustomEncounterTags, TeamSelection.Combat,
                    TeamSelection.GetNextSupportUnitGuid(), "", Actor.team.HeraldryDef);
                newBattleArmor.Init(Actor.CurrentPosition, Actor.CurrentRotation.eulerAngles.y, true);
                newBattleArmor.InitGameRep(null);
                TeamSelection.AddUnit(newBattleArmor);
                newBattleArmor.AddToLance(CustomLance);
                CustomLance.AddUnitGUID(newBattleArmor.GUID);
                newBattleArmor.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(
                    Actor.Combat.BattleTechGame, newBattleArmor, BehaviorTreeIDEnum.CoreAITree);
                newBattleArmor.OnPlayerVisibilityChanged(VisibilityLevel.None);
                newBattleArmor.OnPositionUpdate(Actor.CurrentPosition, Actor.CurrentRotation, -1, true, null, false);
                newBattleArmor.DynamicUnitRole = UnitRole.Brawler;
                UnitSpawnedMessage message = new UnitSpawnedMessage("FROM_ABILITY", newBattleArmor.GUID);
                Actor.Combat.MessageCenter.PublishMessage(message);
                
                newBattleArmor.TeleportActor(Actor.CurrentPosition);
                //ModState.PositionLockMount.Add(newBattleArmor.GUID, Actor.GUID);
                if (newBattleArmor is TrooperSquad squad)
                {
                    squad.AttachToCarrier(Actor, true);
                }
                //Actor.Combat.ItemRegistry.AddItem(newBattleArmor);
                Actor.Combat.RebuildAllLists();
                ModInit.modLog?.Info?.Write(
                    $"[SpawnBattleArmorAtActor] Added PositionLockMount with rider  {newBattleArmor.DisplayName} {newBattleArmor.GUID} and carrier {Actor.DisplayName} {Actor.GUID}.");
            }

            public void OnBeaconDepsLoaded()
            {
                var newUnit= ActorFactory.CreateMech(NewUnitDef, NewPilotDef,
                    CustomEncounterTags, TeamSelection.Combat,
                    TeamSelection.GetNextSupportUnitGuid(), "", SupportHeraldryDef);
                newUnit.Init(SpawnLoc, SpawnRotation.eulerAngles.y, PlayerControl);
                newUnit.InitGameRep(null);
                TeamSelection.AddUnit(newUnit);
                newUnit.AddToTeam(TeamSelection);
                newUnit.AddToLance(CustomLance);
                CustomLance.AddUnitGUID(newUnit.GUID);
                if (PlayerControl)
                {
                    newUnit.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(
                        Combat.BattleTechGame, newUnit, BehaviorTreeIDEnum.DoNothingTree);
                    ModState.PlayerSpawnGUIDs.Add(newUnit.GUID);
                    newUnit.encounterTags.Add("player_unit");
                }
                else
                {
                    newUnit.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(
                        Combat.BattleTechGame, newUnit, BehaviorTreeIDEnum.CoreAITree);
                }
                //newUnit.OnPlayerVisibilityChanged(VisibilityLevel.None);
                newUnit.OnPositionUpdate(SpawnLoc, SpawnRotation, -1, true, null, false);
                newUnit.DynamicUnitRole = UnitRole.Brawler;
                UnitSpawnedMessage message = new UnitSpawnedMessage("FROM_ABILITY", newUnit.GUID);
                Combat.MessageCenter.PublishMessage(message);

                var underMap = newUnit.CurrentPosition;
                underMap.y = -1000f;
                newUnit.TeleportActor(underMap);
                //Combat.ItemRegistry.AddItem(newUnit);
                Combat.RebuildAllLists();
                EncounterLayerParent encounterLayerParent = Combat.EncounterLayerData.gameObject.GetComponentInParent<EncounterLayerParent>();
                DropPodUtils.DropPodSpawner dropSpawner = encounterLayerParent.gameObject.GetComponent<DropPodUtils.DropPodSpawner>();
                if (dropSpawner == null) { dropSpawner = encounterLayerParent.gameObject.AddComponent<DropPodUtils.DropPodSpawner>(); }

                dropSpawner.Unit = newUnit;
                dropSpawner.Combat = Combat;
                dropSpawner.Parent = UnityGameInstance.BattleTechGame.Combat.EncounterLayerData
                    .GetComponentInParent<EncounterLayerParent>();
                dropSpawner.DropPodPosition = SpawnLoc;
                dropSpawner.DropPodRotation = SpawnRotation;

                ModInit.modLog?.Trace?.Write($"DropPodAnim location {SpawnLoc} is also {dropSpawner.DropPodPosition}");
                //ModInit.modLog?.Trace?.Write($"Is dropAnim null fuckin somehow? {dropSpawner == null}");
                dropSpawner.DropPodVfxPrefab = dropSpawner.Parent.DropPodVfxPrefab;
                dropSpawner.DropPodLandedPrefab = dropSpawner.Parent.dropPodLandedPrefab;
                dropSpawner.LoadDropPodPrefabs(dropSpawner.DropPodVfxPrefab, dropSpawner.DropPodLandedPrefab);
                ModInit.modLog?.Trace?.Write($"loaded prefabs success");
                dropSpawner.StartCoroutine(dropSpawner.StartDropPodAnimation(0f));
                ModInit.modLog?.Trace?.Write($"started drop pod anim");
                if (PlayerControl)
                {
                    CameraControl.Instance.HUD.MechWarriorTray.RefreshTeam(Combat.LocalPlayerTeam);
                }
                if (SourceTeam.IsLocalPlayer && (ModInit.modSettings.commandUseCostsMulti > 0 ||
                                               SourceAbility.Def.getAbilityDefExtension().CBillCost > 0))
                {
                    var unitName = "";
                    var unitCost = 0;
                    var unitID = "";

                    unitName = NewUnitDef.Description.UIName;
                    unitID = NewUnitDef.Description.Id;
                    unitCost = NewUnitDef.Chassis.Description.Cost;

                    if (ModState.CommandUses.All(x => x.UnitID != ChosenUnit))
                    {
                        var commandUse =
                            new CmdUseInfo(unitID, SourceAbility.Def.Description.Name, unitName, unitCost,
                                SourceAbility.Def.getAbilityDefExtension().CBillCost);

                        ModState.CommandUses.Add(commandUse);
                        ModInit.modLog?.Info?.Write(
                            $"Added usage cost for {commandUse.CommandName} - {commandUse.UnitName}. UnitUseCost (unadjusted): {unitCost}. Ability Use Cost: {SourceAbility.Def.getAbilityDefExtension().CBillCost}");
                    }
                    else
                    {
                        var cmdUse = ModState.CommandUses.FirstOrDefault(x => x.UnitID == ChosenUnit);
                        if (cmdUse == null)
                        {
                            ModInit.modLog?.Info?.Write($"ERROR: cmdUseInfo was null");
                        }
                        else
                        {
                            cmdUse.UseCount += 1;
                            ModInit.modLog?.Info?.Write(
                                $"Added usage cost for {cmdUse.CommandName} - {cmdUse.UnitName}. UnitUseCost (unadjusted): {unitCost}. Ability Use Cost: {SourceAbility.Def.getAbilityDefExtension().CBillCost}. Now used {cmdUse.UseCount} times.");
                        }
                    }
                }
            }

            public void OnStrafeDepsLoaded()
            {
                var newUnit = ActorFactory.CreateMech(NewUnitDef, NewPilotDef,
                    CustomEncounterTags, TeamSelection.Combat,
                    TeamSelection.GetNextSupportUnitGuid(), "", SupportHeraldryDef);
                newUnit.Init(StrafeWave.NeutralTeam.OffScreenPosition, 0f, false);
                newUnit.InitGameRep(null);
                TeamSelection.AddUnit(newUnit);
                newUnit.AddToTeam(TeamSelection);
                newUnit.AddToLance(CustomLance);
                CustomLance.AddUnitGUID(newUnit.GUID);
                newUnit.GameRep.gameObject.SetActive(true);
                newUnit.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(
                        this.StrafeWave.Ability.Combat.BattleTechGame, newUnit,
                        BehaviorTreeIDEnum.DoNothingTree);
                var eventID = Guid.NewGuid().ToString();
                ModInit.modLog?.Info?.Write($"Initializing Strafing Run (wave) with id {eventID}!");
                TB_StrafeSequence eventSequence =
                    new TB_StrafeSequence(this.ParentSequenceIDForStrafe, eventID, newUnit, SpawnLoc,
                        this.StrafeWave.PositionB, this.StrafeWave.Radius, this.StrafeWave.Team, ModState.IsStrafeAOE, this.StrafeWave.Ability.Def.IntParam1);
                TurnEvent tEvent = new TurnEvent(eventID, Combat,
                    this.StrafeWave.Ability.Def.ActivationETA, null, eventSequence, this.StrafeWave.Ability.Def, false);
                Combat.TurnDirector.AddTurnEvent(tEvent);


                if (this.StrafeWave.Team.IsLocalPlayer && (ModInit.modSettings.commandUseCostsMulti > 0 ||
                                                           this.StrafeWave.Ability.Def.getAbilityDefExtension().CBillCost > 0))
                {
                    var unitName = "";
                    var unitCost = 0;
                    var unitID = "";
                    unitName = NewUnitDef.Description.UIName;
                    unitID = NewUnitDef.Description.Id;
                    unitCost = NewUnitDef.Chassis.Description.Cost;
                    ModInit.modLog?.Info?.Write($"Usage cost will be {unitCost}");

                    if (ModState.CommandUses.All(x => x.UnitID != this.StrafeWave.ActorResource))
                    {

                        var commandUse = new CmdUseInfo(unitID, this.StrafeWave.Ability.Def.Description.Name, unitName,
                            unitCost, this.StrafeWave.Ability.Def.getAbilityDefExtension().CBillCost);

                        ModState.CommandUses.Add(commandUse);
                        ModInit.modLog?.Info?.Write(
                            $"Added usage cost for {commandUse.CommandName} - {commandUse.UnitName}");
                    }
                    else
                    {
                        var cmdUse = ModState.CommandUses.FirstOrDefault(x => x.UnitID == this.StrafeWave.ActorResource);
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

            public void OnStratOpsDepsFailed()
            {
                ModInit.modLog?.Trace?.Write($"Failed to load Dependencies for {ChosenUnit}. This shouldnt happen!");
            }

            public void SpawnBattleArmorAtActor()
            {
                LoadRequest loadRequest = DM.CreateLoadRequest();
                loadRequest.AddBlindLoadRequest(BattleTechResourceType.MechDef, ChosenUnit);
                ModInit.modLog?.Info?.Write($"Added loadrequest for MechDef: {ChosenUnit}");
                loadRequest.ProcessRequests(1000U);

                var instanceGUID =
                    $"{Actor.Description.Id}_{Actor.team.Name}_{ChosenUnit}";

                if (Actor.Combat.TurnDirector.CurrentRound <= 1)
                {
                    if (ModState.DeferredInvokeBattleArmor.All(x => x.Key != instanceGUID) && !ModState.DeferredBattleArmorSpawnerFromDelegate)
                    {
                        ModInit.modLog?.Trace?.Write(
                            $"Deferred BA Spawner missing, creating delegate and returning. Delegate should spawn {ChosenUnit}");

                        void DeferredInvokeBASpawn() =>
                            SpawnBattleArmorAtActor();

                        var kvp = new KeyValuePair<string, Action>(instanceGUID, DeferredInvokeBASpawn);
                        ModState.DeferredInvokeBattleArmor.Add(kvp);
                        foreach (var value in ModState.DeferredInvokeBattleArmor)
                        {
                            ModInit.modLog?.Trace?.Write(
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
                ModInit.modLog?.Info?.Write($"Attempting to spawn {ChosenUnit} with pilot {NewPilotDef?.Description?.Callsign}.");
                DM.MechDefs.TryGet(ChosenUnit, out NewUnitDef);
                NewUnitDef.Refresh();
                //var injectedDependencyLoadRequest = new DataManager.InjectedDependencyLoadRequest(dm);
                //newBattleArmorDef.GatherDependencies(dm, injectedDependencyLoadRequest, 1000U);
                //newBattleArmorDef.Refresh();
                CustomEncounterTags = new TagSet(TeamSelection.EncounterTags) { "SpawnedFromAbility" };

                if (NewPilotDef != null && (!NewUnitDef.DependenciesLoaded(1000U) || !NewPilotDef.DependenciesLoaded(1000U)))
                {
                    DataManager.InjectedDependencyLoadRequest dependencyLoad = new DataManager.InjectedDependencyLoadRequest(DM);
                    dependencyLoad.RegisterLoadCompleteCallback(new Action(this.OnBADepsLoaded));
                    dependencyLoad.RegisterLoadFailedCallback(new Action(this.OnStratOpsDepsFailed)); // do failure handling here
                    if (!NewUnitDef.DependenciesLoaded(1000U))
                    {
                        NewUnitDef.GatherDependencies(DM, dependencyLoad, 1000U);
                    }
                    if (!NewPilotDef.DependenciesLoaded(1000U))
                    {
                        NewPilotDef.GatherDependencies(DM, dependencyLoad, 1000U);
                    }
                    DM.InjectDependencyLoader(dependencyLoad, 1000U);
                }
                else
                {
                    this.OnBADepsLoaded();
                }
                
            }

            public void SpawnBeaconUnitAtLocation()
            {
                LoadRequest loadRequest = DM.CreateLoadRequest();
                loadRequest.AddBlindLoadRequest(BattleTechResourceType.MechDef, ChosenUnit);
                ModInit.modLog?.Info?.Write($"Added loadrequest for MechDef: {ChosenUnit}");
                loadRequest.AddBlindLoadRequest(BattleTechResourceType.PilotDef, ChosenPilot);
                ModInit.modLog?.Info?.Write($"Added loadrequest for PilotDef: {ChosenPilot}");
                
                loadRequest.ProcessRequests(1000U);

                if (false) // maybe dont need? initial invoke should already be deferred, and we cant do this round 1 anyway
                {
                    var instanceGUID =
                        $"{Actor.Description.Id}_{Actor.team.Name}_{ChosenUnit}";

                    if (Actor.Combat.TurnDirector.CurrentRound <= 1)
                    {
                        if (ModState.DeferredInvokeBattleArmor.All(x => x.Key != instanceGUID) &&
                            !ModState.DeferredBattleArmorSpawnerFromDelegate)
                        {
                            ModInit.modLog?.Trace?.Write(
                                $"Deferred BA Spawner missing, creating delegate and returning. Delegate should spawn {ChosenUnit}");

                            void DeferredInvokeBASpawn() =>
                                SpawnBattleArmorAtActor();

                            var kvp = new KeyValuePair<string, Action>(instanceGUID, DeferredInvokeBASpawn);
                            ModState.DeferredInvokeBattleArmor.Add(kvp);
                            foreach (var value in ModState.DeferredInvokeBattleArmor)
                            {
                                ModInit.modLog?.Trace?.Write(
                                    $"there is a delegate {value.Key} here, with value {value.Value}");
                            }

                            return;
                        }
                    }
                }

                //var alliedActors = Combat.AllMechs.Where(x => x.team.IsFriendly(TeamSelection));
                //var chosenpilotSourceMech = alliedActors.GetRandomElement();
                //var newPilotDefID = chosenpilotSourceMech.pilot.pilotDef.Description.Id;
                //DM.PilotDefs.TryGet(newPilotDefID, out this.NewPilotDef);
                ModInit.modLog?.Info?.Write($"Attempting to spawn {ChosenUnit} with pilot {NewPilotDef?.Description?.Callsign}.");
                DM.MechDefs.TryGet(ChosenUnit, out NewUnitDef);
                if (NewUnitDef == null)
                {
                    ModInit.modLog?.Info?.Write($"[ERROR] Unable to fetch NewUnitDef from DataManager. Shit gon broke.");
                    return;
                }
                NewUnitDef.Refresh();
                DM.PilotDefs.TryGet(ChosenPilot, out NewPilotDef);
                if (NewPilotDef == null)
                {
                    ModInit.modLog?.Info?.Write(
                        $"[ERROR] Unable to fetch pilotDef from DataManager. Shit gon broke.");
                    return;
                }

                //var injectedDependencyLoadRequest = new DataManager.InjectedDependencyLoadRequest(dm);
                //newBattleArmorDef.GatherDependencies(dm, injectedDependencyLoadRequest, 1000U);
                //newBattleArmorDef.Refresh();
                CustomEncounterTags = new TagSet(TeamSelection.EncounterTags) {"SpawnedFromAbility"};

                if (!NewUnitDef.DependenciesLoaded(1000U) || !NewPilotDef.DependenciesLoaded(1000U))
                {
                    DataManager.InjectedDependencyLoadRequest dependencyLoad =
                        new DataManager.InjectedDependencyLoadRequest(DM);
                    dependencyLoad.RegisterLoadCompleteCallback(new Action(this.OnBeaconDepsLoaded));
                    dependencyLoad.RegisterLoadFailedCallback(
                        new Action(this.OnStratOpsDepsFailed)); // do failure handling here
                    if (!NewUnitDef.DependenciesLoaded(1000U))
                    {
                        NewUnitDef.GatherDependencies(DM, dependencyLoad, 1000U);
                    }

                    if (!NewPilotDef.DependenciesLoaded(1000U))
                    {
                        NewPilotDef.GatherDependencies(DM, dependencyLoad, 1000U);
                    }

                    DM.InjectDependencyLoader(dependencyLoad, 1000U);
                }
                else
                {
                    this.OnBeaconDepsLoaded();
                }
            }

            public void SpawnStrafingUnit()
            {
                LoadRequest loadRequest = DM.CreateLoadRequest();
                loadRequest.AddBlindLoadRequest(BattleTechResourceType.MechDef, this.StrafeWave.ActorResource);
                ModInit.modLog?.Info?.Write($"Added loadrequest for MechDef: {this.StrafeWave.ActorResource}");
                loadRequest.ProcessRequests(1000U);

                if (false) // maybe dont need? initial invoke should already be deferred, and we cant do this round 1 anyway
                {
                    var instanceGUID =
                        $"{Actor.Description.Id}_{Actor.team.Name}_{ChosenUnit}";

                    if (Actor.Combat.TurnDirector.CurrentRound <= 1)
                    {
                        if (ModState.DeferredInvokeBattleArmor.All(x => x.Key != instanceGUID) &&
                            !ModState.DeferredBattleArmorSpawnerFromDelegate)
                        {
                            ModInit.modLog?.Trace?.Write(
                                $"Deferred BA Spawner missing, creating delegate and returning. Delegate should spawn {ChosenUnit}");

                            void DeferredInvokeBASpawn() =>
                                SpawnBattleArmorAtActor();

                            var kvp = new KeyValuePair<string, Action>(instanceGUID, DeferredInvokeBASpawn);
                            ModState.DeferredInvokeBattleArmor.Add(kvp);
                            foreach (var value in ModState.DeferredInvokeBattleArmor)
                            {
                                ModInit.modLog?.Trace?.Write(
                                    $"there is a delegate {value.Key} here, with value {value.Value}");
                            }

                            return;
                        }
                    }
                }

                //var alliedActors = Combat.AllMechs.Where(x => x.team.IsFriendly(TeamSelection));
                //var chosenpilotSourceMech = alliedActors.GetRandomElement();
                //var newPilotDefID = chosenpilotSourceMech.pilot.pilotDef.Description.Id;
                //DM.PilotDefs.TryGet(newPilotDefID, out this.NewPilotDef);
                ModInit.modLog?.Info?.Write($"Attempting to spawn {ChosenUnit} with pilot {NewPilotDef?.Description?.Callsign}.");
                DM.MechDefs.TryGet(ChosenUnit, out NewUnitDef);
                NewUnitDef.Refresh();
                //var injectedDependencyLoadRequest = new DataManager.InjectedDependencyLoadRequest(dm);
                //newBattleArmorDef.GatherDependencies(dm, injectedDependencyLoadRequest, 1000U);
                //newBattleArmorDef.Refresh();
                CustomEncounterTags = new TagSet(TeamSelection.EncounterTags) { "SpawnedFromAbility" };

                if (NewPilotDef != null && (!NewUnitDef.DependenciesLoaded(1000U) || !NewPilotDef.DependenciesLoaded(1000U)))
                {
                    DataManager.InjectedDependencyLoadRequest dependencyLoad = new DataManager.InjectedDependencyLoadRequest(DM);
                    dependencyLoad.RegisterLoadCompleteCallback(new Action(this.OnStrafeDepsLoaded));
                    dependencyLoad.RegisterLoadFailedCallback(new Action(this.OnStratOpsDepsFailed)); // do failure handling here
                    if (!NewUnitDef.DependenciesLoaded(1000U))
                    {
                        NewUnitDef.GatherDependencies(DM, dependencyLoad, 1000U);
                    }
                    if (!NewPilotDef.DependenciesLoaded(1000U))
                    {
                        NewPilotDef.GatherDependencies(DM, dependencyLoad, 1000U);
                    }
                    DM.InjectDependencyLoader(dependencyLoad, 1000U);
                }
                else
                {
                    this.OnStrafeDepsLoaded();
                }
            }
        }

        public class PendingBAPairingInfo
        {
            public string BAPilotID;
            public LanceLoadoutMechItem MechItem;

            public PendingBAPairingInfo(string baPilotId, LanceLoadoutMechItem lanceLoadoutMechItem)
            {
                this.BAPilotID = baPilotId;
                this.MechItem = lanceLoadoutMechItem;
            }
        }

        public class PendingStrafeWave
        {
            public Ability Ability;
            public string ActorResource;
            public Lance CmdLance;
            public DataManager DM;
            public List<Rect> FootPrintRects;
            public Team NeutralTeam;
            public Vector3 PositionA;
            public Vector3 PositionB;
            public float Radius;
            public int RemainingWaves;
            public HeraldryDef SupportHeraldryDef;
            public PilotDef SupportPilotDef;
            public Team Team;

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
                this.FootPrintRects = Utils.MakeRectangle(positionA, positionB, radius);
            }
        }

        public class SpawnCoords
        {
            public float DistFromTarget;
            public string ID;
            public Vector3 Loc;

            public SpawnCoords(string id, Vector3 loc, float distFromTarget)
            {
                this.ID = id;
                this.Loc = loc;
                this.DistFromTarget = distFromTarget;
            }
        }

        public class StrategicActorTargetInvocation
        {
            public Ability ability;
            public bool active;
            public bool isFriendlyDismount;
            public AbstractActor targetActor;

            public StrategicActorTargetInvocation()
            {
                this.ability = default(Ability);
                this.targetActor = default(AbstractActor);
                this.isFriendlyDismount = false;
                this.active = false;
            }

            public StrategicActorTargetInvocation(Ability cmdAbility, AbstractActor targetActor, bool active, bool isFriendlyDismount = false)
            {
                this.ability = cmdAbility;
                this.targetActor = targetActor;
                this.isFriendlyDismount = isFriendlyDismount;
                this.active = active;
            }
        }

        public class StrategicMovementInvocation : AbstractActorMovementInvocation
        {
            public new bool AbilityConsumesFiring;
            public new string ActorGUID = "";
            public new Vector3 FinalOrientation;
            public bool IsFriendly;
            public bool IsMountOrSwarm;
            public new string MeleeTargetGUID = "";
            public ICombatant MoveTarget;
            public new MoveType MoveType;
            public new List<WayPoint> Waypoints = new List<WayPoint>();
            public StrategicMovementInvocation(){}

            public StrategicMovementInvocation(AbstractActor actor, bool abilityConsumesFiring, ICombatant moveTarget, bool isFriendly, bool isMountOrSwarm) : base(actor, abilityConsumesFiring)
            {
                Pathing pathing = actor.Pathing;

                this.MoveTarget = moveTarget;
                pathing.SetSprinting();
                pathing.UpdateFreePath(moveTarget.CurrentPosition, moveTarget.CurrentPosition, false, false);
                pathing.UpdateLockedPath(moveTarget.CurrentPosition, moveTarget.CurrentPosition, false);
                pathing.LockPosition();
                this.ActorGUID = actor.GUID;
                this.AbilityConsumesFiring = abilityConsumesFiring;
                List<WayPoint> collection = ActorMovementSequence.ExtractWaypointsFromPath(actor, pathing.CurrentPath, pathing.ResultDestination, pathing.CurrentMeleeTarget, this.MoveType);
                this.Waypoints = new List<WayPoint>(collection);
                this.MoveType = pathing.MoveType;
                this.FinalOrientation = moveTarget.GameRep.transform.forward;
                this.MeleeTargetGUID = "";
                this.IsFriendly = isFriendly;
                this.IsMountOrSwarm = isMountOrSwarm;
            }

            public override bool Invoke(CombatGameState combat)
            {
                InvocationMessage.logger.Log("Invoking a STRATEGIC MOVE!");
                AbstractActor abstractActor = combat.FindActorByGUID(this.ActorGUID);
                if (abstractActor == null)
                {
                    InvocationMessage.logger.LogError(string.Format("MechMovement.Invoke Actor with GUID {0} not found!", this.ActorGUID));
                    return false;
                }
                ICombatant combatant = null;
                if (!string.IsNullOrEmpty(this.MeleeTargetGUID))
                {
                    combatant = combat.FindCombatantByGUID(this.MeleeTargetGUID, false);
                    if (combatant == null)
                    {
                        InvocationMessage.logger.LogError(string.Format("MechMovement.Invoke ICombatant with GUID {0} not found!", this.MeleeTargetGUID));
                        return false;
                    }
                }

                StrategicMovementSequence stackSequence = new StrategicMovementSequence(abstractActor, this.Waypoints, this.FinalOrientation, this.MoveType, combatant, this.AbilityConsumesFiring, this.MoveTarget, this.IsFriendly, this.IsMountOrSwarm);
                base.PublishStackSequence(combat.MessageCenter, stackSequence, this);
                return true;
            }
        }

        public class StrategicMovementSequence : ActorMovementSequence
        {
            public bool IsFriendly;
            public bool MountSwarmBA; //handle if airlifting unit dies?
            public ICombatant Target;
            public override bool ConsumesActivation => !MountSwarmBA;//!this.MountSwarmBA; //this might fuck up attack on swarm. grr.

            //public new virtual bool ForceActivationEnd => false;

            public StrategicMovementSequence(AbstractActor actor, List<WayPoint> waypoints, Vector3 finalOrientation, MoveType moveType, ICombatant meleeTarget, bool consumesFiring, ICombatant target, bool friendly, bool mountORswarm) : base(actor, waypoints, finalOrientation, moveType, meleeTarget, consumesFiring)
            {
                this.Target = target;
                this.IsFriendly = friendly;
                this.MountSwarmBA = mountORswarm;
            }

            public override void CompleteOrders()
            {
                base.owningActor.AutoBrace = false;
                base.CompleteOrders();
                base.owningActor.ResetPathing(false);

                if (base.owningActor.team.IsLocalPlayer)
                {}

                if (MountSwarmBA && owningActor is TrooperSquad squad)
                {
                    if (this.Target is BattleTech.Building building)
                    {
                        ModInit.modLog?.Info?.Write(
                            $"[StrategicMovementSequence] Called for BA movement to garrison building {building.DisplayName}.");
                        squad.ProcessGarrisonBuilding(building);
                        return;
                    }
                }

                if (this.Target is AbstractActor targetActor)
                {
                    if (MountSwarmBA)
                    {
                        ModInit.modLog?.Info?.Write(
                            $"[StrategicMovementSequence] Called for BA movement to mount or swarm.");
                        if (base.owningActor is TrooperSquad squad2)
                        {
                            if (this.IsFriendly)
                            {
                                if (!squad2.IsMountedUnit())
                                {
                                    squad2.ProcessMountFriendly(targetActor);
                                    return;
                                }
                            }

                            if (!squad2.IsSwarmingUnit() && squad2.CanSwarm())
                            {
                                squad2.ProcessSwarmEnemy(targetActor);
                            }

                            if (ModInit.modSettings.AttackOnSwarmSuccess && squad2.IsSwarmingUnit())
                            {
                                if (squad2.GetAbilityUsedFiring())
                                {
                                    ModInit.modLog?.Info?.Write($"[StrategicMovementSequence] Actor {squad2.DisplayName} has used an ability that consumed firing, not generating swarm.");
                                    return;
                                }
                                if (!squad2.team.IsLocalPlayer)
                                {
                                    foreach (var weapon in squad2.Weapons)
                                    {
                                        weapon.EnableWeapon();
                                    }
                                }
                                
                                var weps = squad2.Weapons.Where(x => x.IsEnabled && x.HasAmmo).ToList();

                                //                var baselineAccuracyModifier = actor.StatCollection.GetValue<float>("AccuracyModifier");
                                //                actor.StatCollection.Set<float>("AccuracyModifier", -99999.0f);
                                //                ModInit.modLog?.Trace?.Write($"[AbstractActor.DoneWithActor] Actor {actor.DisplayName} getting baselineAccuracyModifer set to {actor.AccuracyModifier}");

                                var loc = ModState.BADamageTrackers[squad2.GUID].BA_MountedLocations.Values.GetRandomElement();

                                ModInit.modLog?.Info?.Write(
                                    $"[StrategicMovementSequence - CompleteOrders] Creating attack sequence on successful swarm attack targeting location {loc}.");

                                if (squad2 is Mech unitMech && ModInit.modSettings.MeleeOnSwarmAttacks)
                                {
                                    if (!ModState.SwarmMeleeSequences.ContainsKey(squad2.GUID))
                                    {
                                        ModState.SwarmMeleeSequences.Add(squad2.GUID, loc);
                                    }
                                    var meleeState = CBTBehaviorsEnhanced.ModState.AddorUpdateMeleeState(squad2, targetActor.CurrentPosition, targetActor, true);
                                    if (meleeState != null)
                                    {
                                        MeleeAttack highestDamageAttackForUI = meleeState.GetHighestDamageAttackForUI();
                                        CBTBehaviorsEnhanced.ModState.AddOrUpdateSelectedAttack(squad2, highestDamageAttackForUI);
                                    }
                                    MessageCenterMessage meleeInvocationMessage = new MechMeleeInvocation(unitMech, targetActor, weps, targetActor.CurrentPosition);
                                    squad2.Combat.MessageCenter.PublishInvocationExternal(meleeInvocationMessage);
                                }
                                else
                                {
                                    var attackStackSequence = new AttackStackSequence(squad2, targetActor, squad2.CurrentPosition,
                                        squad2.CurrentRotation, weps, MeleeAttackType.NotSet, loc, -1);
                                    squad2.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(attackStackSequence));

                                    //                actor.StatCollection.Set<float>("AccuracyModifier", baselineAccuracyModifier);
                                    //                ModInit.modLog?.Trace?.Write($"[AbstractActor.DoneWithActor] Actor {actor.DisplayName} resetting baselineAccuracyModifer to {actor.AccuracyModifier}");
                                   
                                }
                            }

                            //doattacksequencehere

                        }
                        else
                        {
                            ModInit.modLog?.Info?.Write(
                                $"[StrategicMovementSequence] ERROR: called sequence for BA, but actor is not TrooperSquad.");
                            return;
                        }
                    }
                    else if (!MountSwarmBA)
                    {
                        ModInit.modLog?.Info?.Write(
                            $"[StrategicMovementSequence] Called for airlift/dropoff for Target {this.Target.DisplayName}.");

                        if (targetActor.IsAirlifted())
                        {
                            targetActor.DetachFromAirliftCarrier(base.OwningActor, IsFriendly);
                            return;
                        }

                        if (!targetActor.IsAirlifted())
                        {
                            targetActor.AttachToAirliftCarrier(base.OwningActor, IsFriendly);
                        }
                    }
                }
            }
        }
    }
}
