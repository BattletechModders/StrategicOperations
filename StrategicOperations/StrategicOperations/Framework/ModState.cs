using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;

namespace StrategicOperations.Framework
{
    public static class ModState
    {
        public static List<Ability> CommandAbilities = new List<Ability>();

        public static List<KeyValuePair<string, Action>>
            deferredInvokeSpawns = new List<KeyValuePair<string, Action>>();

        public static string deferredActorResource = "";
        public static string popupActorResource = "";
        public static bool FromDelegate;
        public static bool popupShown;

        public static List<Utils.CmdUseInfo> CommandUses = new List<Utils.CmdUseInfo>();

        public static Dictionary<string, int> deploymentAssetsDict = new Dictionary<string, int>();

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
        }

        public static void ResetDeferredSpawners()
        {
            deferredInvokeSpawns = new List<KeyValuePair<string, Action>>();
        }
    }
}
