using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using Abilifier;
using BattleTech;
using UnityEngine;
using static StrategicOperations.Framework.Classes;

namespace StrategicOperations.Framework
{
    public static class ModState
    {
        public static List<string> TeamsWithResupply = new List<string>();
        public static AbstractActor CurrentGarrisonSquadForLOS = null;
        public static AbstractActor CurrentGarrisonSquadForLOF = null;
        public static List<CustomSpawner> CurrentContractBASpawners = new List<CustomSpawner>();

        public static float SwarmSuccessChance = 0f;
        public static float DeSwarmSuccessChance = 0f;
        
        public static Dictionary<string, Dictionary<string, List<string>>> CachedFactionAssociations = new Dictionary<string, Dictionary<string, List<string>>>();
        public static Dictionary<string, Dictionary<string, List<AI_BeaconProxyInfo>>> CachedFactionCommandBeacons = new Dictionary<string, Dictionary<string, List<AI_BeaconProxyInfo>>>(); // key1 is abilityID, key2 is faction name

        public static Dictionary<string, int> CurrentBattleArmorSquads = new Dictionary<string, int>();
        public static Dictionary<string, Dictionary<string,int>> CurrentCommandUnits = new Dictionary<string, Dictionary<string, int>>();

        public static List<ConfigOptions.AI_FactionCommandAbilitySetting> CurrentFactionSettingsList = new List<ConfigOptions.AI_FactionCommandAbilitySetting>();

        public static bool IsStrafeAOE = false;
        public static Dictionary<string, PendingStrafeWave> PendingStrafeWaves =
            new Dictionary<string, PendingStrafeWave>();
        public static List<ArmorLocation> MechArmorMountOrder = new List<ArmorLocation>();
        public static List<ArmorLocation> MechArmorSwarmOrder = new List<ArmorLocation>();

        public static List<VehicleChassisLocations> VehicleMountOrder = new List<VehicleChassisLocations>();

        public static Dictionary<string, BA_DamageTracker> 
            BADamageTrackers = new Dictionary<string, BA_DamageTracker>(); // key is GUID of BA squad

        //public static Dictionary<string, Vector3> SavedBAScale = new Dictionary<string, Vector3>(); // should always be 1,1,1

        public static Dictionary<string, Vector3> CachedUnitCoordinates = new Dictionary<string, Vector3>();
        public static Dictionary<string, BA_GarrisonInfo> PositionLockGarrison = new Dictionary<string, BA_GarrisonInfo>(); // key is mounted unit, value is building
        public static Dictionary<string, string> PositionLockMount = new Dictionary<string, string>(); // key is mounted unit, value is carrier
        public static Dictionary<string, string> PositionLockSwarm = new Dictionary<string, string>(); // key is mounted unit, value is carrier
        //public static Dictionary<string, string> PositionLockAirlift = new Dictionary<string, string>(); // key is mounted unit, value is carrier

        public static Dictionary<string, AirliftTracker> AirliftTrackers = new Dictionary<string, AirliftTracker>(); //Key is mounted unit, value has carrier

        public static Dictionary<string, int> ResupplyShutdownPhases = new Dictionary<string, int>();

        public static List<Ability> CommandAbilities = new List<Ability>();

        public static List<KeyValuePair<string, Action>>
            DeferredInvokeSpawns = new List<KeyValuePair<string, Action>>();

        public static List<KeyValuePair<string, Action>>
            DeferredInvokeBattleArmor = new List<KeyValuePair<string, Action>>();

        public static Dictionary<string, AbstractActor> DeferredDespawnersFromStrafe =
            new Dictionary<string, AbstractActor>();

        public static string DeferredActorResource = "";
        public static string PopupActorResource = "";
        public static int StrafeWaves;
        public static string PilotOverride = null;
        public static bool DeferredSpawnerFromDelegate;
        public static bool DeferredBattleArmorSpawnerFromDelegate;
        public static bool OutOfRange;

        public static Dictionary<string, AI_DealWithBAInvocation> AiDealWithBattleArmorCmds = new Dictionary<string, AI_DealWithBAInvocation>();

        public static Dictionary<string, AI_CmdInvocation> AiCmds = new Dictionary<string, AI_CmdInvocation>();

        public static Dictionary<string, StrategicActorTargetInvocation> StrategicActorTargetInvocationCmds = new Dictionary<string, StrategicActorTargetInvocation>();

        public static List<CmdUseInfo> CommandUses = new List<CmdUseInfo>();

        public static List<CmdUseStat> DeploymentAssetsStats = new List<CmdUseStat>();

        public static List<BA_TargetEffect> BA_MountSwarmEffects = new List<BA_TargetEffect>();
        public static List<BA_TargetEffect> OnGarrisonCollapseEffects = new List<BA_TargetEffect>();
        public static List<AirliftTargetEffect> AirliftEffects = new List<AirliftTargetEffect>();

        public static BA_DeswarmMovementInfo DeSwarmMovementInfo = new BA_DeswarmMovementInfo();

        public static void Initialize()
        {
            MechArmorMountOrder.Add(ArmorLocation.CenterTorso);
            MechArmorMountOrder.Add(ArmorLocation.CenterTorsoRear);
            MechArmorMountOrder.Add(ArmorLocation.RightTorso);
            MechArmorMountOrder.Add(ArmorLocation.RightTorsoRear);
            MechArmorMountOrder.Add(ArmorLocation.LeftTorso);
            MechArmorMountOrder.Add(ArmorLocation.LeftTorsoRear);

            MechArmorSwarmOrder.Add(ArmorLocation.CenterTorso);
            MechArmorSwarmOrder.Add(ArmorLocation.CenterTorsoRear);
            MechArmorSwarmOrder.Add(ArmorLocation.RightTorso);
            MechArmorSwarmOrder.Add(ArmorLocation.RightTorsoRear);
            MechArmorSwarmOrder.Add(ArmorLocation.LeftTorso);
            MechArmorSwarmOrder.Add(ArmorLocation.LeftTorsoRear);
            MechArmorSwarmOrder.Add(ArmorLocation.LeftArm); // LA, RA, LL, RL, HD are for swarm only
            MechArmorSwarmOrder.Add(ArmorLocation.RightArm);
            MechArmorSwarmOrder.Add(ArmorLocation.LeftLeg);
            MechArmorSwarmOrder.Add(ArmorLocation.RightLeg);
            MechArmorSwarmOrder.Add(ArmorLocation.Head);

            VehicleMountOrder.Add(VehicleChassisLocations.Front);
            VehicleMountOrder.Add(VehicleChassisLocations.Rear);
            VehicleMountOrder.Add(VehicleChassisLocations.Left);
            VehicleMountOrder.Add(VehicleChassisLocations.Right);
            VehicleMountOrder.Add(VehicleChassisLocations.Turret);


            BA_MountSwarmEffects = new List<BA_TargetEffect>();
            foreach (var BA_Effect in ModInit.modSettings.BATargetEffects)
            {
                ModInit.modLog?.Trace?.Write($"[Initializing BATargetEffects] Adding effects for {BA_Effect.ID}!");
                foreach (var jObject in BA_Effect.effectDataJO)
                {
                    var effectData = new EffectData();
                    effectData.FromJSON(jObject.ToString());
                    BA_Effect.effects.Add(effectData);
                    ModInit.modLog?.Trace?.Write($"EffectData statname: {effectData?.statisticData?.statName}");
                }
                BA_MountSwarmEffects.Add(BA_Effect);
            }

            OnGarrisonCollapseEffects = new List<BA_TargetEffect>();
            foreach (var BA_Effect in ModInit.modSettings.OnGarrisonCollapseEffects)
            {
                ModInit.modLog?.Trace?.Write($"[Initializing OnGarrisonCollapseEffects] Adding effects for {BA_Effect.ID}!");
                foreach (var jObject in BA_Effect.effectDataJO)
                {
                    var effectData = new EffectData();
                    effectData.FromJSON(jObject.ToString());
                    BA_Effect.effects.Add(effectData);
                    ModInit.modLog?.Trace?.Write($"EffectData statname: {effectData?.statisticData?.statName}");
                }
                OnGarrisonCollapseEffects.Add(BA_Effect);
            }

            AirliftEffects = new List<AirliftTargetEffect>();
            foreach (var airliftEffect in ModInit.modSettings.AirliftTargetEffects)
            {
                ModInit.modLog?.Trace?.Write($"[Initializing AirliftTargetEffects] Adding effects for {airliftEffect.ID}!");
                foreach (var jObject in airliftEffect.effectDataJO)
                {
                    var effectData = new EffectData();
                    effectData.FromJSON(jObject.ToString());
                    airliftEffect.effects.Add(effectData);
                    ModInit.modLog?.Trace?.Write($"EffectData statname: {effectData?.statisticData?.statName}");
                }
                AirliftEffects.Add(airliftEffect);
            }
        }

        public static void ResetAll()
        {
            ResupplyShutdownPhases = new Dictionary<string, int>();
            TeamsWithResupply = new List<string>();
            CurrentContractBASpawners = new List<CustomSpawner>();
            SwarmSuccessChance = 0f;
            DeSwarmSuccessChance = 0f;
            CurrentBattleArmorSquads = new Dictionary<string, int>();
            CurrentFactionSettingsList = new List<ConfigOptions.AI_FactionCommandAbilitySetting>();
            PendingStrafeWaves = new Dictionary<string, PendingStrafeWave>();
            BADamageTrackers = new Dictionary<string, BA_DamageTracker>();
            AirliftTrackers = new Dictionary<string, AirliftTracker>();
            CommandAbilities = new List<Ability>();
            DeferredInvokeSpawns = new List<KeyValuePair<string, Action>>();
            DeferredInvokeBattleArmor = new List<KeyValuePair<string, Action>>();
            DeferredDespawnersFromStrafe = new Dictionary<string, AbstractActor>();
            CommandUses = new List<CmdUseInfo>();
            DeploymentAssetsStats = new List<CmdUseStat>();
            //SavedBAScale = new Dictionary<string, Vector3>();
            CachedUnitCoordinates = new Dictionary<string, Vector3>();
            PositionLockMount = new Dictionary<string, string>();
            PositionLockSwarm = new Dictionary<string, string>();
            PositionLockGarrison = new Dictionary<string, BA_GarrisonInfo>();
            DeferredActorResource = "";
            PopupActorResource = "";
            StrafeWaves = 0; // this is TBD-> want to make beacons define # of waves.
            PilotOverride = null;
            DeferredSpawnerFromDelegate = false;
            DeferredBattleArmorSpawnerFromDelegate = false;
            OutOfRange = false;
            AiCmds = new Dictionary<string, AI_CmdInvocation>();
            StrategicActorTargetInvocationCmds = new Dictionary<string, StrategicActorTargetInvocation>();
            IsStrafeAOE = false;
        }

        public static void ResetDelegateInfos()
        {
            DeferredSpawnerFromDelegate = false;
            DeferredActorResource = "";
            PopupActorResource = "";
            PilotOverride = null;
        }

        public static void ResetDeferredSpawners()
        {
            DeferredInvokeSpawns = new List<KeyValuePair<string, Action>>();
        }
        public static void ResetDeferredBASpawners()
        {
            DeferredInvokeBattleArmor = new List<KeyValuePair<string, Action>>();
        }
    }
}
