using CustomComponents;
using IRBTModUtils.CustomInfluenceMap;
using ModTek.Public;
using Newtonsoft.Json;
using StrategicOperations.Framework;
using System.Reflection;
using static StrategicOperations.Framework.Classes;
using Random = System.Random;

namespace StrategicOperations
{
    public static class Mod
    {
        public const string HarmonyPackage = "us.tbone.StrategicOperations";
        private static string modDir;
        internal static NullableLogger Log = NullableLogger.GetLogger("StrategicOperations", NullableLogger.TraceLogLevel);

        internal static ModSettings modSettings;
        public static readonly Random Random = new Random();

        public static void Init(string directory, string settings)
        {

            modDir = directory;
            Exception settingsException = null;
            try
            {
                modSettings = JsonConvert.DeserializeObject<ModSettings>(settings);
            }
            catch (Exception ex)
            {
                settingsException = ex;
                modSettings = new ModSettings();
            }

            if (settingsException != null)
            {
                Mod.Log.Error?.Log($"EXCEPTION while reading settings file! Error was: {settingsException}");
            }

            Mod.Log.Info?.Log($"Initializing StrategicOperations - Version {typeof(ModSettings).Assembly.GetName().Version}");
            Registry.RegisterSimpleCustomComponents(Assembly.GetExecutingAssembly());
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), HarmonyPackage);
            ModState.Initialize();

            //dump settings
            Mod.Log.Info?.Log($"Settings dump: {settings}");
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