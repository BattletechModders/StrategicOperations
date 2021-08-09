using System;
using System.Collections.Generic;
using BattleTech;
using MissionControl.Logic;
using UnityEngine;
using static StrategicOperations.Framework.Classes;

namespace StrategicOperations.Framework
{
    public static class ModState
    {
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

        public static void ResetSpawnInfo()
        {
        }
    }
}
