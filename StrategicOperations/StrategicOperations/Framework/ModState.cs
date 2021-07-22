using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using UnityEngine;

namespace StrategicOperations.Framework
{
    public static class ModState
    {
        public static List<Ability> CommandAbilities = new List<Ability>();

        public static List<KeyValuePair<string, Action>>
            deferredInvokeSpawns = new List<KeyValuePair<string, Action>>();

        public static List<Vector3> selectedAIVectors;
        public static string deferredActorResource = "";
        public static string popupActorResource = "";
        public static string PilotOverride= null;
        public static bool FromDelegate;
        public static bool OutOfRange;

        public static List<Utils.CmdUseInfo> CommandUses = new List<Utils.CmdUseInfo>();

        public static List<Utils.CmdUseStat> deploymentAssetsStats = new List<Utils.CmdUseStat>();

        public static void ResetAll()
        {
            CommandAbilities = new List<Ability>();
            deferredInvokeSpawns = new List<KeyValuePair<string, Action>>();
            CommandUses = new List<Utils.CmdUseInfo>();
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
    }
}
