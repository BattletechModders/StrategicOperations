using CustomComponents;
using IRBTModUtils.CustomInfluenceMap;
using ModTek.Public;
using Newtonsoft.Json;
using StrategicOperations.Framework;
using System.IO;
using System.Reflection;
using static StrategicOperations.Framework.Classes;
using Random = System.Random;

namespace StrategicOperations
{
    public static class Mod
    {
        public const string HarmonyPackage = "us.tbone.StrategicOperations";
        internal static NullableLogger Log = NullableLogger.GetLogger("StrategicOperations", NullableLogger.TraceLogLevel);

        internal static ModSettings Settings;
        public static readonly Random Random = new Random();

        public static void Init(string modDirectory, string settingsJSON)
        {

            try
            {
                string settingsFile = Path.Combine(modDirectory, "settings.json");
                using StreamReader reader = new(settingsFile);
                string settingsText = reader.ReadToEnd();
                Mod.Settings = JsonConvert.DeserializeObject<ModSettings>(settingsText);
            }
            catch (Exception e)
            {
                Mod.Settings = new ModSettings();
                Mod.Log.Error?.Log($"EXCEPTION while reading settings file! Error was: {e}");
            }

            Mod.Log.Info?.Log($"Initializing StrategicOperations - Version {typeof(ModSettings).Assembly.GetName().Version}");
            Registry.RegisterSimpleCustomComponents(Assembly.GetExecutingAssembly());
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), HarmonyPackage);
            ModState.Initialize();

            Mod.Settings.LogConfig();
        }

        public static void FinishedLoading(List<string> loadOrder)
        {
            Mod.Log.Info?.Log($"Invoking FinishedLoading");
            var customPositionFactors = new List<CustomInfluenceMapPositionFactor>()
            {
                new StrategicInfluenceMapFactors.CustomPositionFactors.PreferAvoidStandingInAirstrikeAreaPosition(),
                new StrategicInfluenceMapFactors.CustomPositionFactors.PreferCloserToResupply(),
                new StrategicInfluenceMapFactors.CustomPositionFactors.PreferNearerToSwarmTargets()
            };
            CustomFactors.Register("StrategicOperations_PositionFactors", customPositionFactors);
            var customHostileFactors = new List<CustomInfluenceMapHostileFactor>()
            {
                new StrategicInfluenceMapFactors.CustomHostileFactors.PreferAvoidStandingInAirstrikeAreaWithHostile(),
                new StrategicInfluenceMapFactors.CustomHostileFactors.PreferCloserToResupplyWithHostile(),
                new StrategicInfluenceMapFactors.CustomHostileFactors.PreferNearerToSwarmTargetsWithHostile()
            };
            CustomFactors.Register("StrategicOperations_HostileFactors", customHostileFactors);
        }
    }

}