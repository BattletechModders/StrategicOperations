using System;
using System.Collections.Generic;
using System.Reflection;
using Harmony;
using Localize;
using Newtonsoft.Json;
using StrategicOperations.Framework;
using static StrategicOperations.Framework.Classes;
using Logger = StrategicOperations.Framework.Logger;
using Random = System.Random;

namespace StrategicOperations
{
    public static class ModInit
    {
        internal static Logger modLog;
        private static string modDir;
        public static readonly Random Random = new Random(123);


        internal static Settings modSettings;
        public const string HarmonyPackage = "us.tbone.StrategicOperations";
        public static void Init(string directory, string settings)
        {
            modDir = directory;
            modLog = new Logger(modDir, "Strategery");
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
            ModState.Initialize();
            //dump settings
            ModInit.modLog.LogTrace($"Settings dump: {settings}");
        }
    }
    class Settings
    {
        public bool DEVTEST_AIPOS = false;
        public bool DEVTEST_Logging = false;
        public bool debugFlares = false;
        public bool enableLogging = true;
        public bool enableTrace = true;
        public string flareResourceID = "vfxPrfPrtl_fireTerrain_smLoop";
        public bool showStrafeCamera = true;
        public bool strafeEndsActivation = true;
        public bool spawnTurretEndsActivation = true;
        public float strafeTargetsFriendliesChance = 1f;
        public float strafeNeutralBuildingsChance = 1f;
        public int deployProtection = 8;
        public float strafeSensorFactor = 4f;
        public float strafeVelocityDefault = 150f;
        public float strafeAltitudeMin = 75f;
        public float strafeAltitudeMax = 250f;
        public float strafePreDistanceMult = 6f;
        public int strafeWaves = 1; // strafes will spawn this many units and do successive strafing runs.
        public float timeBetweenAttacks = 0.35f;
        public float strafeMinDistanceToEnd = 10f;
        public float commandUseCostsMulti = 1f;
        
        public List<string> deploymentBeaconEquipment = new List<string>(); //e.g. Item.HeatSinkDef.Gear_HeatSink_Generic_Standard
        
        public Dictionary<string, List<string>> AI_FactionBeacons = new Dictionary<string, List<string>>(); // if factionvaluename is found here, will use only the associated list of units. else will use only default

        public Dictionary<string, List<AI_FactionCommandAbilitySetting>> commandAbilities_AI = new Dictionary<string, List<AI_FactionCommandAbilitySetting>>(); //key is factionValue.Name, paired with list of AI_FactionCommandAbilitySetting contains AbilityDefID, AddChance and DiffMod.
        public ColorSetting customSpawnReticleColor = new ColorSetting();
        public string customSpawnReticleAsset = "";
        public int AI_InvokeStrafeThreshold = 1;
        public int AI_InvokeSpawnThreshold = 1;
        public List<AI_SpawnBehavior> AI_SpawnBehavior = new List<AI_SpawnBehavior>(); // values can be "AMBUSH", "BRAWLER" (is default if none selected), "REINFORCE"
        public string BattleArmorMountAndSwarmID = "";
        public BA_TargetEffect BATargetEffect = new BA_TargetEffect();
        public float AI_BattleArmorSpawnChance = 0f;
        public float AI_BattleArmorSpawnDiffMod = 0f;
        public Dictionary<string, List<string>> BattleArmorFactionAssociations = new Dictionary<string, List<string>>();
        public string BattleArmorDeSwarmRoll = "";
        public string BattleArmorDeSwarmSwat = "";
        public List<string> ArmActuatorCategoryIDs = new List<string>();
        public bool AttackOnSwarmSuccess = false;
    }
}