using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static StrategicOperations.Framework.Classes;

namespace StrategicOperations
{
    internal class ModSettings
    {
        public List<string> AI_BattleArmorExcludedContractIDs = new List<string>();
        public List<string> AI_BattleArmorExcludedContractTypes = new List<string>();
        public int AI_InvokeSpawnThreshold = 1;
        public int AI_InvokeStrafeThreshold = 1;
        public List<AI_SpawnBehavior> AI_SpawnBehavior = new List<AI_SpawnBehavior>(); // values can be "AMBUSH", "BRAWLER" (is default if none selected), "REINFORCE"

        public string AirliftAbilityID = "";
        public bool AirliftCapacityByTonnage = false;
        public List<string> AirliftImmuneTags = new List<string>();
        public List<AirliftTargetEffect> AirliftTargetEffects = new List<AirliftTargetEffect>();
        public bool AllowIRBTUHandleVisibility = false;
        public List<string> ArmActuatorCategoryIDs = new List<string>();

        public bool AttackOnSwarmSuccess = false;
        //BD wants controllable gated on item: companystat and use ROI?
        //RT wants specific ability or reinforcement thingy controllable: component tag on the beacon item. per abilitry will suck bc abilities suck.

        public List<ColorSetting> BAMountPairColors = new List<ColorSetting>();
        public string BAMountReminderText = "Shift-click unit in drop slot to set carrier";
        public bool UseOriginalBAMountInterface = false;
        public HashSet<string> forbidCarrierContractTypes = new HashSet<string>() { "SoloDuel", "DuoDuel" };
        public List<BA_TargetEffect> BATargetEffects = new List<BA_TargetEffect>();
        public string BattleArmorDeSwarmRoll = "";
        public string BattleArmorDeSwarmSwat = "";
        public bool InternalBAAffectsOverallDropTonnage = true;
        public bool ExternalBAAffectsOverallDropTonnage = true;
        public bool InternalBAAffectsSlotDropTonnage = false;
        public bool ExternalBAAffectsSlotDropTonnage = true;
        public List<BA_FactionAssoc> BattleArmorFactionAssociations = new List<BA_FactionAssoc>();
        public string BattleArmorMountAndSwarmID = "";
        public bool BattleArmorHandsyOverridesInternalOnly = false;

        public Dictionary<string, ConfigOptions.BeaconExclusionConfig> BeaconExclusionConfig =
            new Dictionary<string, ConfigOptions.BeaconExclusionConfig>();

        //public string BattleArmorDeSwarmMovement = "";
        public List<string> BPodComponentIDs = new List<string>(); //statistic for dmg will be BPod_DamageDealt
        public bool BPodsAutoActivate = true; //BPods always auto activate when swarmed for AI, this only controls for player
        public bool CanDropOffAfterMoving = false;

        public List<ConfigOptions.AI_FactionCommandAbilitySetting> commandAbilities_AI = new List<ConfigOptions.AI_FactionCommandAbilitySetting>();
        public float commandUseCostsMulti = 1f; // deprecate?

        public List<string> crewOrCockpitCustomID = new List<string>
        {
            "CrewCompartment",
            "Cockpit"
        };

        public string customSpawnReticleAsset = "";
        public ColorSetting customSpawnReticleColor = new ColorSetting();
        public string CUVehicleStat = "CUFakeVehicle";
        public bool Debug = false;
        public bool debugFlares = false;

        public List<string> deploymentBeaconEquipment = new List<string>(); //e.g. Item.HeatSinkDef.Gear_HeatSink_Generic_Standard
        public int deployProtection = 8;

        //public bool UseActorStatsForDeswarmAbilities = false;
        public Dictionary<string, ConfigOptions.BA_DeswarmAbilityConfig> DeswarmConfigs =
            new Dictionary<string, ConfigOptions.BA_DeswarmAbilityConfig>();

        public ConfigOptions.BA_DeswarmMovementConfig DeswarmMovementConfig = new ConfigOptions.BA_DeswarmMovementConfig();
        public bool DEVTEST_AIPOS = false;

        //StratOps_player_control_enable -> component tag for player control always
        //StratOps_player_control_disable-> component tag for player control NEVER

        public string DisableAISwarmTag = "AI_DISABLE_SWARM";

        public bool DisableGarrisons = false;
        public bool EnableQuickReserve = false;
        public bool enableTrace = true;

        public bool EnforceIFFForAmmoTooltips = false;
        public KeyCode EquipmentButtonsHotkey = KeyCode.M; // with shift to activate/cycle through any existing buttons
        public string flareResourceID = "vfxPrfPrtl_fireTerrain_smLoop";
        public float GarrisonBuildingArmorFactor = 1f;
        public float lostBeaconUnitCostMult = 0f;
        public string MountIndicatorAsset = "";
        public ColorSetting MountIndicatorColor = new ColorSetting();
        public List<BA_TargetEffect> OnGarrisonCollapseEffects = new List<BA_TargetEffect>();

        public List<string> PlayerControlSpawnAbilities = new List<string>(); //abilities here will always allow player control

        public List<string> PlayerControlSpawnAbilitiesBlacklist = new List<string>(); // abilities here will never allow player control

        public bool PlayerControlSpawns = false; //complete override. players always control all spawns.

        public ConfigOptions.ResupplyConfigOptions ResupplyConfig = new ConfigOptions.ResupplyConfigOptions();
        public float SBI_HesitationMultiplier = 0f;
        public bool ShowAmmoInVehicleTooltips = false;
        public bool showStrafeCamera = true;

        public bool spawnTurretEndsActivation = true;
        // ive strafing runs.

        public float strafeAAFailThreshold = 1f; //for AI strafes, if fail % is higher than this, they wont try
        public float strafeAltitudeMax = 250f;
        public float strafeAltitudeMin = 75f;
        public bool strafeEndsActivation = true;
        public float strafeMinDistanceToEnd = 10f;
        public float strafeNeutralBuildingsChance = 1f;
        public float strafeObjectiveBuildingsChance = 1f;
        public float strafePreDistanceMult = 6f;
        public float strafeSensorFactor = 4f;
        public float strafeTargetsFriendliesChance = 1f;
        public float strafeVelocityDefault = 150f;
        public int strafeWaves = 1; // strafes will spawn this many units and do
        public bool strafeUseAlternativeImplementation = false; // if true, use the AA cover method based on cover unit distance and strafe unit strength
        public float strafeAAMaxCoverDistance = 1000f; // Only used by alternative implementation
        public float strafeFallbackStrengthValue = 5f; // Only used by alternative implementation
        public Dictionary<string, float> strafeAttackerStrength = new Dictionary<string, float>(); // Only used by alternative implementation

        public float timeBetweenAttacks = 0.35f;

        //public List<string> BeaconExcludedContractTypes = new List<string>();
        //public List<string> BeaconExcludedContractIDs = new List<string>();
        public bool UsingMechAffinityForSwarmBreach = false;
        public bool ReworkedCarrierEvasion = true;
        public bool MeleeOnSwarmAttacks = true;

        public string SimBattleArmorMountError = "The selected squad cannot be transported by this unit.";
    }
}
