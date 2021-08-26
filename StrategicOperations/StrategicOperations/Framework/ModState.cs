using System;
using System.Collections.Generic;
using BattleTech;
using UnityEngine;
using static StrategicOperations.Framework.Classes;

namespace StrategicOperations.Framework
{
    public static class ModState
    {
        public static Dictionary<string, List<ChassisLocations>> CachedDestroyedLocations = new Dictionary<string, List<ChassisLocations>>();
        public static Dictionary<string, List<Transform>> CachedActiveComponents = new Dictionary<string, List<Transform>>();


        public static Dictionary<string, Vector3> CachedUnitCoordinates = new Dictionary<string, Vector3>();
        public static Dictionary<string, string> PositionLockMount = new Dictionary<string, string>(); // key is mounted unit, value is carrier
        public static Dictionary<string, string> PositionLockSwarm = new Dictionary<string, string>(); // key is mounted unit, value is carrier

        public static List<Ability> CommandAbilities = new List<Ability>();

        public static List<KeyValuePair<string, Action>>
            deferredInvokeSpawns = new List<KeyValuePair<string, Action>>();

        public static string deferredActorResource = "";
        public static string popupActorResource = "";
        public static string PilotOverride= null;
        public static bool FromDelegate;
        public static bool OutOfRange;

        public static Dictionary<string, AI_CmdInvocation> AiCmds = new Dictionary<string, AI_CmdInvocation>();

        public static List<CmdUseInfo> CommandUses = new List<CmdUseInfo>();

        public static List<CmdUseStat> deploymentAssetsStats = new List<CmdUseStat>();

        public static void ResetAll()
        {
            CommandAbilities = new List<Ability>();
            deferredInvokeSpawns = new List<KeyValuePair<string, Action>>();
            CommandUses = new List<CmdUseInfo>();
            deploymentAssetsStats = new List<CmdUseStat>();
            CachedDestroyedLocations = new Dictionary<string, List<ChassisLocations>>();
            CachedActiveComponents = new Dictionary<string, List<Transform>>();
            CachedUnitCoordinates = new Dictionary<string, Vector3>();
            PositionLockMount = new Dictionary<string, string>();
            PositionLockSwarm = new Dictionary<string, string>();
            deferredActorResource = "";
            popupActorResource = "";
            PilotOverride = null;
            FromDelegate = false;
            OutOfRange = false;
            AiCmds = new Dictionary<string, AI_CmdInvocation>();

        }

        public static void ResetDelegateInfos()
        {
            FromDelegate = false;
            deferredActorResource = "";
            popupActorResource = "";
            PilotOverride = null;
        }

        public static void ResetDeferredSpawners()
        {
            deferredInvokeSpawns = new List<KeyValuePair<string, Action>>();
        }

        public static List<AI_CommandAbilitySetting> AI_CommandAbilitySettings = new List<AI_CommandAbilitySetting>();

    }
}
