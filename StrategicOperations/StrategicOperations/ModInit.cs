using System;
using System.Collections.Generic;
using System.Reflection;
using Harmony;
using Newtonsoft.Json;
using StrategicOperations.Framework;

namespace StrategicOperations
{
    public static class ModInit
    {
        internal static Logger modLog;
        private static string modDir;


        internal static Settings modSettings;
        public const string HarmonyPackage = "us.tbone.StrategicOperations";
        public static void Init(string directory, string settings)
        {
            modDir = directory;
            modLog = new Logger(modDir, "Strategery", true);
            try
            {
                modSettings = JsonConvert.DeserializeObject<Settings>(settings);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                modSettings = new Settings();
            }
            //HarmonyInstance.DEBUG = true;
            ModInit.modLog.LogMessage($"Initializing StrategicOperations - Version {typeof(Settings).Assembly.GetName().Version}");

            var harmony = HarmonyInstance.Create(HarmonyPackage);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
    class Settings
    {
        public bool enableLogging = true;
        public bool showStrafeCamera = true;
        public bool strafeTargetsFriendlies = true;
        public bool strafeEndsActivation = true;
        public bool spawnTurretEndsActivation = true;
        public int deployProtection = 8;
        public float strafeSensorFactor = 4f;
        public float strafeVelocityDefault = 150f;
        public float strafeAltitudeMin = 75f;
        public float strafeAltitudeMax = 250f;
        public float strafePreDistanceMult = 6f;
        public float timeBetweenAttacks = 0.35f;
        public float strafeMinDistanceToEnd = 10f;
        public float commandUseCostsMulti = 1f;
        public List<string> deploymentBeaconEquipment = new List<string>(); //e.g. Item.HeatSinkDef.Gear_HeatSink_Generic_Standard
        public List<string> commandAbilities_AI = new List<string>(); //e.g. Item.HeatSinkDef.Gear_HeatSink_Generic_Standard
    }
}