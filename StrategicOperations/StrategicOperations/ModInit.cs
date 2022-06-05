using System;
using System.Collections.Generic;
using System.Reflection;
using BattleTech;
using CustomComponents;
using Harmony;
using IRBTModUtils.CustomInfluenceMap;
using IRBTModUtils.Logging;
using Localize;
using Newtonsoft.Json;
using StrategicOperations.Framework;
using static StrategicOperations.Framework.Classes;
using Random = System.Random;

namespace StrategicOperations
{
    public static class ModInit
    {
        internal static DeferringLogger modLog;
        private static string modDir;
        public static readonly Random Random = new Random();

        internal static Settings modSettings;
        public const string HarmonyPackage = "us.tbone.StrategicOperations";
        public static void Init(string directory, string settings)
        {
            
            modDir = directory;
            Exception settingsException = null;
            try
            {
                modSettings = JsonConvert.DeserializeObject<Settings>(settings);
            }
            catch (Exception ex)
            {
                settingsException = ex;
                modSettings = new Settings();
            }
            //HarmonyInstance.DEBUG = true;
            modLog = new DeferringLogger(modDir, "Strategery", modSettings.Debug, modSettings.enableTrace);
            if (settingsException != null)
            {
                ModInit.modLog?.Error?.Write($"EXCEPTION while reading settings file! Error was: {settingsException}");
            }
            
            ModInit.modLog?.Info?.Write($"Initializing StrategicOperations - Version {typeof(Settings).Assembly.GetName().Version}");
            var harmony = HarmonyInstance.Create(HarmonyPackage);
            //FileLog.Log(HarmonyPackage);
            Registry.RegisterSimpleCustomComponents(Assembly.GetExecutingAssembly());
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            ModState.Initialize();
            //dump settings
            ModInit.modLog?.Info?.Write($"Settings dump: {settings}");
        }

        public static void FinishedLoading(List<string> loadOrder)
        {
            ModInit.modLog?.Info?.Write($"Invoking FinishedLoading");
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
    class Settings
    {
        public bool DEVTEST_AIPOS = false;
        public bool Debug = false;
        public bool debugFlares = false;
        public bool enableTrace = true;
        public string flareResourceID = "vfxPrfPrtl_fireTerrain_smLoop";
        public string CUVehicleStat = "CUFakeVehicle";
        public bool showStrafeCamera = true;
        public bool strafeEndsActivation = true;
        public bool spawnTurretEndsActivation = true;
        public float strafeTargetsFriendliesChance = 1f;
        public float strafeNeutralBuildingsChance = 1f;
        public float strafeObjectiveBuildingsChance = 1f;
        public int deployProtection = 8;
        public float strafeSensorFactor = 4f;
        public float strafeVelocityDefault = 150f;
        public float strafeAltitudeMin = 75f;
        public float strafeAltitudeMax = 250f;
        public float strafePreDistanceMult = 6f;
        public int strafeWaves = 1; // strafes will spawn this many units and do
                                    // ive strafing runs.
                                    
        public float strafeAAFailThreshold = 1f; //for AI strafes, if fail % is higher than this, they wont try
        public float timeBetweenAttacks = 0.35f;
        public float strafeMinDistanceToEnd = 10f;
        public float commandUseCostsMulti = 1f;
        
        public List<string> deploymentBeaconEquipment = new List<string>(); //e.g. Item.HeatSinkDef.Gear_HeatSink_Generic_Standard
        
        public List<ConfigOptions.AI_FactionCommandAbilitySetting> commandAbilities_AI = new List<ConfigOptions.AI_FactionCommandAbilitySetting>();
        public ColorSetting customSpawnReticleColor = new ColorSetting();
        public string customSpawnReticleAsset = "";
        public string MountIndicatorAsset = "";
        public ColorSetting MountIndicatorColor = new ColorSetting();
        public int AI_InvokeStrafeThreshold = 1;
        public int AI_InvokeSpawnThreshold = 1;
        public List<AI_SpawnBehavior> AI_SpawnBehavior = new List<AI_SpawnBehavior>(); // values can be "AMBUSH", "BRAWLER" (is default if none selected), "REINFORCE"
        public string BattleArmorMountAndSwarmID = "";
        public List<BA_TargetEffect> BATargetEffects = new List<BA_TargetEffect>();
        public List<AirliftTargetEffect> AirliftTargetEffects = new List<AirliftTargetEffect>();
        public List<BA_TargetEffect> OnGarrisonCollapseEffects = new List<BA_TargetEffect>();
        public List<BA_FactionAssoc> BattleArmorFactionAssociations = new List<BA_FactionAssoc>();
        public string BattleArmorDeSwarmRoll = "";
        public string BattleArmorDeSwarmSwat = "";
        //public string BattleArmorDeSwarmMovement = "";
        public List<string> BPodComponentIDs = new List<string>(); //statistic for dmg will be BPod_DamageDealt
        public bool BPodsAutoActivate = true; //BPods always auto activate when swarmed for AI, this only controls for player
        public List<string> ArmActuatorCategoryIDs = new List<string>();

        //public bool UseActorStatsForDeswarmAbilities = false;
        public Dictionary<string, ConfigOptions.BA_DeswarmAbilityConfig> DeswarmConfigs =
            new Dictionary<string, ConfigOptions.BA_DeswarmAbilityConfig>();

        public ConfigOptions.BA_DeswarmMovementConfig DeswarmMovementConfig = new ConfigOptions.BA_DeswarmMovementConfig();

        public bool AttackOnSwarmSuccess = false;
        public List<string> AI_BattleArmorExcludedContractTypes = new List<string>();
        public List<string> AI_BattleArmorExcludedContractIDs = new List<string>();

        public List<string> BeaconExcludedContractTypes = new List<string>();
        public List<string> BeaconExcludedContractIDs = new List<string>();

        public bool UsingMechAffinityForSwarmBreach = false;

        public bool DisableGarrisons = false;
        public float GarrisonBuildingArmorFactor = 1f;

        public string AirliftAbilityID = "";
        public bool CanDropOffAfterMoving = false;
        public bool AirliftCapacityByTonnage = false;
        public List<string> AirliftImmuneTags = new List<string>();

        public ConfigOptions.ResupplyConfigOptions ResupplyConfig = new ConfigOptions.ResupplyConfigOptions();

        public bool EnforceIFFForAmmoTooltips = false;
        public bool ShowAmmoInVehicleTooltips = false;
        public bool EnableQuickReserve = false;
        public float SBI_HesitationMultiplier = 0f;
    }
}