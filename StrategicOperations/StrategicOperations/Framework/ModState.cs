using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Abilifier;
using BattleTech;
using UnityEngine;
using static StrategicOperations.Framework.Classes;

namespace StrategicOperations.Framework
{
    public static class ModState
    {
        public static Dictionary<string, PendingStrafeWave> PendingStrafeWaves =
            new Dictionary<string, PendingStrafeWave>();
        public static List<ArmorLocation> MechArmorMountOrder = new List<ArmorLocation>();
        public static List<ArmorLocation> MechArmorSwarmOrder = new List<ArmorLocation>();

        public static List<VehicleChassisLocations> VehicleMountOrder = new List<VehicleChassisLocations>();

        public static Dictionary<string, BA_DamageTracker> 
            BADamageTrackers = new Dictionary<string, BA_DamageTracker>(); // key is GUID of BA squad

        public static Dictionary<string, Vector3> SavedBAScale = new Dictionary<string, Vector3>();

        public static Dictionary<string, Vector3> CachedUnitCoordinates = new Dictionary<string, Vector3>();
        public static Dictionary<string, string> PositionLockMount = new Dictionary<string, string>(); // key is mounted unit, value is carrier
        public static Dictionary<string, string> PositionLockSwarm = new Dictionary<string, string>(); // key is mounted unit, value is carrier

        public static List<Ability> CommandAbilities = new List<Ability>();

        public static List<KeyValuePair<string, Action>>
            deferredInvokeSpawns = new List<KeyValuePair<string, Action>>();

        public static List<KeyValuePair<string, Action>>
            deferredInvokeBattleArmor = new List<KeyValuePair<string, Action>>();

        public static string deferredActorResource = "";
        public static string popupActorResource = "";
        public static int strafeWaves;
        public static string PilotOverride= null;
        public static bool DeferredSpawnerFromDelegate;
        public static bool DeferredBattleArmorSpawnerFromDelegate;
        public static bool OutOfRange;

        public static Dictionary<string, AI_DealWithBAInvocation> AiDealWithBattleArmorCmds = new Dictionary<string, AI_DealWithBAInvocation>();

        public static Dictionary<string, AI_CmdInvocation> AiCmds = new Dictionary<string, AI_CmdInvocation>();

        public static Dictionary<string, BA_MountOrSwarmInvocation> AiBattleArmorAbilityCmds = new Dictionary<string, BA_MountOrSwarmInvocation>();

        public static List<CmdUseInfo> CommandUses = new List<CmdUseInfo>();

        public static List<CmdUseStat> deploymentAssetsStats = new List<CmdUseStat>();

        public static BA_TargetEffect BAUnhittableEffect = new BA_TargetEffect();

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


            BAUnhittableEffect = ModInit.modSettings.BATargetEffect;
            foreach (var jObject in ModInit.modSettings.BATargetEffect.effectDataJO)
            {
                var effectData = new EffectData();
                effectData.FromJSON(jObject.ToString());
                BAUnhittableEffect.effects.Add(effectData);
            }
        }

        public static void ResetAll()
        {
            currentFactionSettingsList = new List<AI_FactionCommandAbilitySetting>();
            PendingStrafeWaves = new Dictionary<string, PendingStrafeWave>();
            BADamageTrackers = new Dictionary<string, BA_DamageTracker>(); 
            CommandAbilities = new List<Ability>();
            deferredInvokeSpawns = new List<KeyValuePair<string, Action>>();
            deferredInvokeBattleArmor = new List<KeyValuePair<string, Action>>();
            CommandUses = new List<CmdUseInfo>();
            deploymentAssetsStats = new List<CmdUseStat>();
            SavedBAScale = new Dictionary<string, Vector3>();
            CachedUnitCoordinates = new Dictionary<string, Vector3>();
            PositionLockMount = new Dictionary<string, string>();
            PositionLockSwarm = new Dictionary<string, string>();
            deferredActorResource = "";
            popupActorResource = "";
            strafeWaves = 0; // this is TBD-> want to make beacons define # of waves.
            PilotOverride = null;
            DeferredSpawnerFromDelegate = false;
            DeferredBattleArmorSpawnerFromDelegate = false;
            OutOfRange = false;
            AiCmds = new Dictionary<string, AI_CmdInvocation>();
            AiBattleArmorAbilityCmds = new Dictionary<string, BA_MountOrSwarmInvocation>();
        }

        public static void ResetDelegateInfos()
        {
            DeferredSpawnerFromDelegate = false;
            deferredActorResource = "";
            popupActorResource = "";
            PilotOverride = null;
        }

        public static void ResetDeferredSpawners()
        {
            deferredInvokeSpawns = new List<KeyValuePair<string, Action>>();
        }
        public static void ResetDeferredBASpawners()
        {
            deferredInvokeBattleArmor = new List<KeyValuePair<string, Action>>();
        }

        public static List<AI_FactionCommandAbilitySetting> currentFactionSettingsList = new List<AI_FactionCommandAbilitySetting>();
    }
}
