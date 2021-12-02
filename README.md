# StrategicOperations

**Depends On Abilifier v1.1.0.2 or higher!** 

This mod enables, fixes, and expands the unused vanilla Command Abilities. There are two main "types" of abilities, Strafes and Spawns.

settings in the mod.json:

```
	"enableLogging": true,
	"enableTrace": false,
	"flareResourceID": "vfxPrfPrtl_fireTerrain_smLoop",
	"showStrafeCamera": false,
	"strafeEndsActivation": true,
	"spawnTurretEndsActivation": true,
	"deployProtection": 8,
	"strafeTargetsFriendliesChance": 1.0,
	"strafeNeutralBuildingsChance": 0.2,
	"strafeSensorFactor": 4.0,
	"strafeVelocityDefault": 150.0,
	"strafeAltitudeMin": 75.0,
	"strafeAltitudeMax": 250.0,
	"strafePreDistanceMult": 15.0,
	"strafeMinDistanceToEnd": 10.0,
	"timeBetweenAttacks": 0.35,
	"commandUseCostsMulti": 1.0,
	"deploymentBeaconEquipment": [
		"Item.UpgradeDef.Gear_TurretBeacon_Cicada",
		"Item.UpgradeDef.Gear_TurretBeacon_Cicada2",
		"Item.UpgradeDef.Gear_TurretBeacon_kanazuchi",
		"Item.UpgradeDef.Gear_TurretBeacon_hvac",
		"Item.UpgradeDef.Gear_TurretBeacon_lgr",
		"Item.UpgradeDef.Gear_TurretBeacon_Schiltron",
		"Item.UpgradeDef.Gear_TurretBeacon_Schiltron2"
	],
	"customSpawnReticleColor": {
		"r": 255,
		"g": 16,
		"b": 240
	},
	"customSpawnReticleAsset": "select_spawn_reticle",
	"strafeWaves": 3,
	"AI_FactionBeacons": {
		"ClanGhostBear": [
			"Item.UpgradeDef.Gear_Contract_Tank_Burke_WoB"
		]
	},
	"commandAbilities_AI": {
		"ClanGhostBear": [
			{
				"AbilityID": "AbilityDefCMD_Strafe_AI",
				"AddChance": 0.2,
				"DiffMod": 0.0
			}
		]
	},
	"AI_SpawnBehavior": [
		{
			"Tag": "ProtoMech",
			"Behavior": "AMBUSH",
			"MinRange": "5"
		}
	],
	"BattleArmorMountAndSwarmID": "AbilityDefBattleArmorMount",
	"BattleArmorDeSwarmRoll": "AbilityDefDeSwarmerRoll",
	"BattleArmorDeSwarmSwat": "AbilityDefDeSwarmerSwat",
	"ArmActuatorCategoryIDs": [
		"ArmShoulder",
		"ArmUpperActuator",
		"ArmLowerActuator",
		"OmniLowerActuator",
		"ArmHandActuator",
		"OmniHandActuator"
	],
	"BATargetEffect": {
		"ID": "BA_AccurateFire",
		"Name": "Battle Armor - SwarmingAccuracy",
		"description": "Battle Armor has greatly increased accuracy against the unit it is swarming.",
		"effectDataJO": [
			{
				"durationData": {},
				"targetingData": {
					"effectTriggerType": "Passive",
					"effectTargetType": "Creator",
					"showInStatusPanel": true
				},
				"effectType": "StatisticEffect",
				"Description": {
					"Id": "BA_EasyTargetingPassive",
					"Name": "Easy shots",
					"Details": "Battle Armor has increased accuracy while swarming.",
					"Icon": "allied-star"
				},
				"nature": "Buff",
				"statisticData": {
					"statName": "AccuracyModifier",
					"operation": "Set",
					"modValue": "-9001",
					"modType": "System.Single"
				}
			},
			{
				"durationData": {
					"duration": -1,
					"stackLimit": -1
				},
				"targetingData": {
					"effectTriggerType": "Passive",
					"effectTargetType": "Creator"
				},
				"effectType": "StatisticEffect",
				"Description": {
					"Id": "BA_CALLED_SHOT",
					"Name": "BA Called Shot",
					"Details": "Called Shots twice as reliable when swarming",
					"Icon": "uixSvgIcon_ability_mastertactician"
				},
				"statisticData": {
					"statName": "CalledShotBonusMultiplier",
					"operation": "Float_Multiply",
					"modValue": "9001.0",
					"modType": "System.Single"
				}
			},
			{
				"durationData": {
					"duration": -1,
					"stackLimit": -1
				},
				"targetingData": {
					"effectTriggerType": "Passive",
					"effectTargetType": "Creator",
					"showInTargetPreview": false,
					"showInStatusPanel": false
				},
				"effectType": "StatisticEffect",
				"Description": {
					"Id": "BA_FocusFireCluster",
					"Name": "BA ClusterFuck",
					"Details": "Better clustering for BA while swarming.",
					"Icon": "UixSvgIcon_specialEquip_System"
				},
				"statisticData": {
					"statName": "ClusteringModifier",
					"operation": "Float_Add",
					"modValue": "9001",
					"modType": "System.Single",
					"targetCollection": "Weapon"
				},
				"nature": "Buff"
			}
		]
	},
	"AI_BattleArmorSpawnChance": 1.0,
	"AI_BattleArmorSpawnDiffMod": 0.05,
	"BattleArmorFactionAssociations": {
		"ClanGhostBear": [
			"mechdef_ba_is_standard"
		]
	},
	"AttackOnSwarmSuccess": true
```

`enableLogging` - bool, enable logging

`enableTrace` - bool, enable verbose logging

`flareResourceID` - string, name of vFX resource used for flares denoting strafe/spawn positions

`showStrafeCamera` - bool, if true, camera will show 1st person view of strafing unit as it flies in (but view returns to normal before it starts shooting).

`strafeEndsActivation` - bool, do strafes automatically end the turn of the unit using them?

`spawnTurretEndsActivation` - bool, does spawn unit automatically end the turn of the unit using them?

`deployProtection` - int, number of evasion pips added to deployed units at spawn (IRTweaks/MC spawn protection dont work on these)

`strafeTargetsFriendliesChance` - float, probability that strafing targets friendly units

`strafeNeutralBuildingsChance` - float, probability that strafing targets non-objective buildings (friendly buildings are covered by `strafeTargetsFriendliesChance`)

`strafeSensorFactor` - float, multiplier of strafing units base sensor range for revealing sensor blips of hostiles as it flies over them.

`strafeVelocityDefault` - float, default velocity of strafing unit <i>while strafing</i>. The faster the unit moves, the fewer targets it will be able to hit during a strafe. If MaxSpeed is > 0 in the strafing unit, then that speed will override this value.

`strafeAltitudeMin` and `strafeAltitudeMax` - float. The altitide of the strafing unit is the maximum weapon range of the strafing unit divided by 4, but is clamped between these two values.

`strafePreDistanceMult` - float, controls the distance at which the strafing unit is instantiated from the point of strafing start; influences the length of the "fly-in" sequence.

`strafeMinDistanceToEnd` - float, distance from the strafing unit to the endpoint of the strafe at which the strafe is considered to be "complete" and no more targets will be attacked.

`timeBetweenAttacks` - float. minimum amount of time that must elapse before strafing unit can instantiate another attack. 0.35 is HBS' default, can probably go as low as 0.1 without strange things happening. really only a minor tweak if you think the strafing units arent attacking "enough", but velocity and elevation are likely more important.

`commandUseCostsMulti` - float, multiplier governing costs of using command abilities. if >0, the cost of the unit being used (as defined in the unit def) is multiplied by this value to obtain a per-use cost of using the ability. should probably only be used if beacons are not set to be consumed, or at least set to some low value. **this cost will stack with any manually defined costs in the AbilityDef using Abilifier**

`deploymentBeaconEquipment` - List<string>, list of component IDs that are considered "deployment beacons" to give options for the specific unit that gets deployed/strafes during combat.

`customSpawnReticleColor` - new type, defines custom color of reticle used for spawns. fields r, g, b, are RGB values, 0-255.
	
`customSpawnReticleAsset` string. name of custom .DDS asset that will be used for Spawn reticle (needs to be one that is added to manifest via modtek)
	
`strafeWaves` - int, default number of units (same unit copied multiple times) that will perform a strafe. e.g., if set to 3 and strafe calls a Lightning Aerospace fighter, 3 Lightnings will strafe the target area in succession. They tend to target exactly the same units (unless of course one of the targeted units gets destroyed by one of the previous strafing units). Overriden by mechcomponent tags in beacons where tag is "StrafeWaves_X" where X is the number of waves. E.g. a beacon with tag `StrafeWaves_5` would strafe with 5 units.

"AI_FactionBeacons"`: essentially functions the same as `deploymentBeaconEquipment`, but restricts factions listed to the corresponding equipment. if a faction is not listed in here, it will use only the "default" unit listed in a given command ability.

`commandAbilities_AI` - **Format Change in v2.0.0.3** - dictionary of command abilities and probabilities the AI can get per-faction (dictionary "key" is FactionValue.Name, e.g. "ClanGhostBear" or "Liao"), as well as the probability and difficulty modifier to that probability that a given AI unit will be given the corresponding ability. e.g for the following setting, Clan Ghost Bear units will have a 2% + 0.5% per-difficulty chance of being given `AbilityDefCMD_Strafe_AI` at contract start. Currently only the Target Team will recieve command abilities (their allies will not). If a faction is <i>not</i> listed in this setting, they will not be given any command abilities.

```

"ClanGhostBear": [
			{
				"AbilityID": "AbilityDefCMD_Strafe_AI",
				"AddChance": 0.02,
				"DiffMod": 0.05
			}
		]

```
	
`AI_SpawnBehavior` - list of "spawn behavior" for AI to use if they recieve a "spawn" type ability. Largely affects spawn positioning. "Tag" corresponds to a MechDef tag on the unit being spawned that will use the Behavior and MinRange defined in the setting. If the unit has multiple tags matching a behavior setting, it will simply use the behavior matching the first tag with a behavior defined. Options for behavior are: "AMBUSH" which will attempt to spawn the unit as close as possible to the nearest enemy, "BRAWLER" which will attempt to spawn the unit at an approximate centroid of all detected enemies, and "REINFORCE" which will attempt to spawn at the approximate centroid of all friendlies. MinRange simply sets a minimum spawn distance from any actor. Example setting:

```
{
	"Tag": "ProtoMech",
	"Behavior": "AMBUSH",
	"MinRange": "5"
}
```

`BattleArmorMountAndSwarmID` - string, ability ID of <b>component</b> ability that will allow Battle Armor to mount/swarm units (same ability is used for both). Ability will need to be added to BA specific component (something all BA, but only BA will have, such as cockpit. Should be one of the "hidden" single components so there are not duplicates).

`BattleArmorDeSwarmRoll` - string, ability ID of pilot ability that allows mech to "roll" (forced self-knockdown) in order to dislodge swarming battle armor. Chance of success is 50% + (Piloting skill x 5%), capped at 95%. On a success, there is a 30% chance to smush the Battle Armor in the process. Ability is automatically granted to Mechs at contract start (e.g. does not need to be added manually to pilot).

`BattleArmorDeSwarmSwat` - string, ability ID of pilot ability that allows mech to "swat" swarming battle armor (remove using arms). Chance of success is 30% + (Piloting skill x 5%) - 5% for each "missing" arm actuator. An arm actuator is considered "missing" if it is destroyed, or was never mounted in the first place. Shoulder, Upper Arm, Lower Arm, and Hand for both left and right arms; thus a mech missing both arms would suffer a 40% penalty (8 x 5%). Ability is automatically granted to Mechs at contract start (e.g. does not need to be added manually to pilot).

`ArmActuatorCategoryIDs` - list of strings, Custom Category IDs identifying actuators that will be considered in `BattleArmorDeSwarmSwat` calculations. E.g, if ArmActuator is listed here and an actuator has the following, it would be counted:
```
{
	"Custom": {
		"Category" : [
			{"CategoryID": "ArmActuator"},
			{"CategoryID": "NonQuad"}
		],
```

`BATargetEffect` - Effect which will be applied to Battle Armor while swarming. Intended use is to improve accuracy and clustering of swarming BA so they always (usually, mostly) hit the same location on the unit they're swarming. Of course you can add whatever else you want here.

`AI_BattleArmorSpawnChance` - float, base probability that AI units that <i>can</i> mount Battle Armor, either mounted externally or internally, will get Battle Armor at contract start. Note that any added Battle Armor is independent of any "support lance" or "extra lance" settings in Mission Control or other mods. Added to AI_BattleArmorSpawnDiffMod for total chance.

`AI_BattleArmorSpawnDiffMod` - float, contract difficulty is multiplied by this value and added to AI_BattleArmorSpawnChance to determine probability of AI units spawning BA.

`BattleArmorFactionAssociations` - Faction information for AI battle armor spawning. If a faction does not have an entry here, it will not spawn Battle Armor. E.g, using the following setting the only faction that would spawn Battle Armor would be ClanGhostBear, and the only battle armor it would spawn is `mechdef_ba_is_standard`.

```
"BattleArmorFactionAssociations": {
			"ClanGhostBear": [
				"mechdef_ba_is_standard"
			]
		}
```

`AttackOnSwarmSuccess` - bool, if true BA will initiate an attack sequence on a successful swarming attempt (rather than needing to wait until the subsequence activation)
	
## Spawns

Spawns are basically what they sound like: spawning reinforcement units at the selected location. These units will be AI-controlled (allied). Exactly <i>what</i> unit gets deployed depends on a few things.

1) The "type" of the unit noted in `ActorResource` defines the type of unit that can be deployed by that ability. If it starts with "mechdef_", then mechs can be deployed. If it starts with "vehicledef_", then vehicles can be deployed. If it starts with "turretdef_" then turrets can be deployed.
	
2) Holding shift while activating the ability will bring up a popup with any available units to select for deployment (in addition to the default assigned in `ActorResource`). The player can obtain these "alternative" units by acquiring "deployment beacon" items. Deployment beacons can be of any upgrade type, but must have two things in their ComponentTags: they must have the tag "CanSpawnTurret", and they must have a tag with the ID of the unit they will enable for deployment, e.g. "mechdef_cicada_CDA-4G". The number of "probes" in the player inventory is the number of that unit that can be deployed in a given mission (within any restrictions also defined in the AbilityDef); i.e if you have 2 probes for mechdef_cicada_CDA-4G, you could deploy up to two of those Cicadas, and subsequent deployments would be limited to whatever unit is defined in the ability ActorResource.
	
3) If the "beacon" item contains the string "ConsumeOnUse" in its component tags, the beacon item will be actually consumed on use; the player inventory in simgame will be decremented.

4) If the "beacon" item contains a component tag that starts with "StratOpsPilot_", the remainder of that tag should be the PilotDef ID of the pilot that will pilot the unit (overrides anything set by CMDPilotOverride below)

Spawn ability json structure:
```
{
	"Description" : {
		"Id" : "AbilityDefCMD_UrbDrop",
		"Name" : "UrbDrop",
		"Details" : "DEPLOY MOBILE TURRET",
		"Icon" : "uixSvgIcon_genericDiamond"
	},
	"CMDPilotOverride" : "pilot_sim_dekker",
	"ActivationTime" : "CommandAbility",
	"Resource" : "CommandAbility",
	"ActivationCooldown" : -1,
	"NumberOfUses" : 1,
	"specialRules" : "SpawnTurret",
	"Targeting" : "CommandSpawnPosition",
	"ActorResource" : "mechdef_urbanmech_UM-R50",
	"StringParam1" : "Deployment Unavailable",
	"StringParam2" : "unit_urb",
	"IntParam2" : 250
}
```
The configurable parameters of the above:
	
`CMDPilotOverride` - string. if present and not "", lists ID of pilotDef for pilot that will use this unit.

`Description` - as any other Ability Description

`ActivationCooldown` - int, turn cooldown to use the ability again

`NumberOfUses` - int, number of times the ability can be used in a given contract (set to -1 or remove for unlimited uses)

`ActivationETA` - int, number of turns before the unit will be deployed.

`ActorResource` - string, the default "def" ID of the unit to be deployed.

`IntParam2` - int, the maximum distance from the initiating actor at which a unit can be deployed.

`StringParam2` - string, tag that "beacons" componentDefs are required to have in order to be used by this ability.

## Strafes

Strafes are also mostly what they sound like: a flying unit strafing the battlefield. Valid targets for a strafe are calculated within an AOE around a line drawn between two points (the "strafing run"), and can be either units or Objective buildings. Friendly units within the AOE can and will be hit by the strafing run!

A unique feature of strafes is that the strafing unit will reveal the locations (minimal sensor blip) of hostile units it detects as it flies in to do the actual strafing. These sensor blips are removed/return to hidden at the start of the following round.
	
Similarly to Spawns, the actual unit doing the strafing depends on the following:
1) Only vehicles can be used for strafing.
	
2) Holding shift while activating the ability will bring up a popup with any available units to select for strafing (in addition to the default assigned in `ActorResource`). The player can obtain these "alternative" units by acquiring "deployment beacon" items. Deployment beacons for strafing can be of any upgrade type, but must have two things in their ComponentTags: they must have the tag "CanStrafe", and they must have a tag with the ID of the unit they will enable for strafing, e.g. "vehicledef_ALACORN_IIC". The number of "probes" in the player inventory is the number of times that unit can strafe in a given mission (within any restrictions also defined in the AbilityDef); i.e if you have 2 probes for vehicledef_ALACORN_IIC, you could strafe using that Alacorn twice, and subsequent strafes would be limited to whatever unit is defined in the ability ActorResource.
	
3) If the "beacon" item contains the string "ConsumeOnUse" in its component tags, the beacon item will be actually consumed on use; the player inventory in simgame will be decremented.

4) If the "beacon" item contains a component tag that starts with "StratOpsPilot_", the remainder of that tag should be the PilotDef ID of the pilot that will pilot the unit (overrides anything set by CMDPilotOverride below)

5) If the "beacon" item contains a component tag that starts with "StrafeWaves_", the remainder of that tag should be the number (integer) of "waves" or copies of the unit that will do the strafing. E.g. StrafeWaves_3 would strafe with 3 of whatever the unit is.

The json structure of a strafe ability follows:

```
	"Description" : {
		"Id" : "AbilityDefCMD_Strafe",
		"Name" : "STRAFE",
		"Details" : "CALLS IN A STRAFING RUN BY YOUR AEROSPACE SUPPORT UNIT.",
		"Icon" : "uixSvgIcon_genericDiamond"
	},
	"CMDPilotOverride" : "pilot_sim_dekker",
	"ActivationTime" : "CommandAbility",
	"Resource" : "CommandAbility",
	"ActivationCooldown" : 1,
	"NumberOfUses" : 1,
	"ActivationETA" : 1,
	"specialRules" : "Strafe",
	"Targeting" : "CommandTargetTwoPoints",
	"FloatParam1" : 100.0,
	"FloatParam2" : 250.0,
	"ActorResource" : "vehicledef_SCHREK",
	"StringParam1" : "Strafe Unavailable",
	"StringParam2" : "unit_urb",
	"IntParam1" : 5,
	"IntParam2" : 500
```

The configurable parameters of the above:

`Description` - as any other Ability Description

`CMDPilotOverride` - string. if present and not "", lists ID of pilotDef for pilot that will use this unit.
	
`ActivationCooldown` - int, turn cooldown to use the ability again

`NumberOfUses` - int, number of times the ability can be used in a given contract (set to -1 or remove for unlimited uses)

`ActivationETA` - int, number of turns before the actual incoming strafe will occur.

`FloatParam1` - float, the AOE radius in which a unit can be a valid target along the strafing run.

`FloatParam2` - float, the maximum length of the strafing run.

`ActorResource` - string, the default "def" ID of the unit doing the strafing.

`IntParam1` - int, number of "flares" that pop up to show the strafing effect area.

`IntParam2` - int, the maximum distance from the initiating actor at which a strafing run can be initialized OR ended! so you can't start a run at `IntParam2` and still go `FloatParam2` past it.
	
`StringParam2` - string, tag that "beacons" componentDefs are required to have in order to be used by this ability.

## AI Command Ability Usage

Starting in 2.0.0.0, the AI can be given command abilities (spawn and strafe) just like the player. To reiterate, the following setting controls when/if the AI will be given a command ability. Any given AI unit can only receive a single command ability (i.e, a spawn or a strafe, but not both).

`commandAbilities_AI` - **Format Change in v2.0.0.3** - dictionary of command abilities and probabilities the AI can get per-faction (dictionary "key" is FactionValue.Name, e.g. "ClanGhostBear" or "Liao"), as well as the probability and difficulty modifier to that probability that a given AI unit will be given the corresponding ability. e.g for the following setting, Clan Ghost Bear units will have a 2% + 0.5% per-difficulty chance of being given `AbilityDefCMD_Strafe_AI` at contract start. Currently only the Target Team will recieve command abilities (their allies will not). If a faction is <i>not</i> listed in this setting, they will not be given any command abilities.

```

"ClanGhostBear": [
			{
				"AbilityID": "AbilityDefCMD_Strafe_AI",
				"AddChance": 0.02,
				"DiffMod": 0.05
			}
		]

```

Generally speaking, if the AI <i>can</i> use a command ability, it <i>will</i> use a command ability. Because of this, I strongly suggest creating separate command abilities for AI use that a much higher cooldown and/or fewer uses than the command abilities available to the player. Unless you want to get spammed by strafes every round.
	

### AI Spawns

To reiterate, the following setting controls spawn behavior: 

`AI_SpawnBehavior` - list of "spawn behavior" for AI to use if they recieve a "spawn" type ability. Largely affects spawn positioning. "Tag" corresponds to a MechDef tag on the unit being spawned that will use the Behavior and MinRange defined in the setting. If the unit has multiple tags matching a behavior setting, it will simply use the behavior matching the first tag with a behavior defined. Options for behavior are: "AMBUSH" which will attempt to spawn the unit as close as possible to the nearest enemy, "BRAWLER" which will attempt to spawn the unit at an approximate centroid of all detected enemies, and "REINFORCE" which will attempt to spawn at the approximate centroid of all friendlies. MinRange simply sets a minimum spawn distance from any actor. Example setting:

```
{
	"Tag": "ProtoMech",
	"Behavior": "AMBUSH",
	"MinRange": "5"
}
```

### AI Strafes

Strafes for the AI function largely in the same way as they do for the player. The AI will attempt to use strafes in a way that has the most enemy targets in the affected area, but will ignore friendly units in their calculations (given two options, one of which would hit 3 of your units and 3 of theirs versus another that would hit 2 of your units and none of theirs, they'll choose the 3 + 3). That said, the calculation isn't very smart, and the "start point" of a strafing run will always be the nearest target unit <i>even if there is another orientation that would hit more enemies</i>. I didn't want to iterate through every single possible start position and orientation because it became computationally expensive. Because of the logic involved this also means that if multiple enemies have a strafe available they'll tend to stack multiple strafes in the exact same orientation (assuming no units moved/nothing else changed between enemy A and enemy B activating).

## Battle Armor Is Useful Now!

Buckle up buttercup, there's a lot happening here.

### Mount/Swarm

Battle Armor can now mount/be carried by friendly units, as well as initiate true swarming attacks against enemies. Likewise, BattleMechs can attempt to dislodge swarming Battle Armor. 

#### Enabling Mount/Swarm

In order to mount/be carried, the battle armor must have a component ability such as the following:

```
{
	"Description": {
		"Id": "AbilityDefBattleArmorMount",
		"Name": "Mount Up",
		"Details": "ACTION: Battle armor will mount/dismount selected unit",
		"Icon": "uixSvgIcon_skullAtlas"
	},
	"DisplayParams": "ShowInMWTRay",
	"ActivationTime": "ConsumedByMovement",
	"ActivationCooldown": -1,
	"Targeting": "ActorTarget",
	"ResolveCost": 0,
	"TargetFriendlyUnit": "BOTH",
	"EffectData": [
		{
			"durationData": {
				"duration": -1,
				"stackLimit": -1
			},
			"targetingData": {
				"effectTriggerType": "OnActivation",
				"effectTargetType": "SingleTarget",
				"showInStatusPanel": false
			},
			"effectType": "StatisticEffect",
			"Description": {
				"Id": "StatusEffect-MountUnit",
				"Name": "Battle Armor Mount",
				"Details": "mount",
				"Icon": "uixSvgIcon_ability_precisionstrike"
			},
			"nature": "Buff",
			"statisticData": {
				"statName": "BattleArmorMount",
				"operation": "Set",
				"modValue": "true",
				"modType": "System.Boolean"
			}
		}
	]
}
```

The important parts are `"Targeting": "ActorTarget",`, `"TargetFriendlyUnit": "BOTH",`, targetingData be `OnActivation` and `SingleTarget`, and the effect itself must set `BattleArmorMount` to True.

In order to have Battle Armor mounted <i>to</i> it, a unit must have either stat effect that sets bool `HasBattleArmorMounts` to true OR must have the integer stat `InternalBattleArmorSquadCap` set to the # of Battle Armor squads that can be carried internally (for APCs and such). For AI units, those are the two stats that further dictate whether BA can be spawned.

On the player-facing side, an additional bool stat, `IsBattleArmorHandsy` can be added to <i>Battle Armor</i> that would allow BA to mount friendly units <i>regardless of</i> `HasBattleArmorMounts`. 
This was added to allow Battle Armor such as the Marauder BA that canonically have Magnetic Clamps to allow them to ride on <i>any</i> friendly unit.

For example, this may be added to the `statusEffects` section of the omnimech gyro:

```
{
			"durationData": {
				"duration": -1
			},
			"targetingData": {
				"effectTriggerType": "Passive",
				"effectTargetType": "Creator",
				"showInTargetPreview": false,
				"showInStatusPanel": false
			},
			"effectType": "StatisticEffect",
			"Description": {
				"Id": "StatusEffect-getHasBattleArmorMounts",
				"Name": "getHasBattleArmorMounts",
				"Details": "getHasBattleArmorMounts",
				"Icon": "uixSvgIcon_ability_precisionstrike"
			},
			"nature": "Buff",
			"statisticData": {
				"statName": "HasBattleArmorMounts",
				"operation": "Set",
				"modValue": "true",
				"modType": "System.Boolean"
			}
		}
```

#### Using Mount/Swarm

In order to mount or swarm, activate the appropriate component ability, then select either a friendly (to mount) or enemy (to swarm) within range. By default the range within which you can mount/swarm is equal to the longest of walk/sprint/jump distance. Likewise, in order to dismount or stop swarming, activate the same component ability and then select the unit the BA is mounted to/currently swarming. Dismounting must be done at the start of the BA activation, and once dismounted the BA can move/attack as normal.

Attempts to mount a friendly unit are always successful. Attempts to swarm an enemy unit make a simplified melee roll to determine success. If unsuccessful, the BA will be deposited in the same hex as the unit they attempted to swarm. On both a swarm success and failure, a floatie will be generated indicating success or failure. Attempts to mount or swarm always end the BA activation, and must be conducted <i>before</i> the BA attempts to move.

BA that is either swarming or mounted is noted in the "carrier" unit's armor paperdoll, as well as the current armor/structure of the BA mounted to a specific location. The individual suits of the BA squad are always distributed "evenly" across a mech in the following order: CT, CT-R, RT, RT-R, LT, LT-R for mounts (only a single squad can be mounted), and CT, CT-R, RT, RT-R, LT, LT-R, LA, RA, LL, RL, HD for swarms. Multiple squads can swarm a mech simultaneously, and will double-up on locations as needed. For vehicles the order is Front, Rear, Left, Right, Turret for both swarms and mounts. Incoming attacks targeting the "carrier" have a 33% chance of impacting the BA suit mounted to that location instead of the "carrier", with excess damaging transfering through the BA to the carrier.

If the mech chassis location where BA is mounted is destroyed, any BA mounted to that location has a 33% chance of also being destroyed except when the swarming BA is responsible for destroying that location. If the entire unit to which BA is mounted is destroyed, any BA mounted to that unit has a 33% chance of also being destroyed except when the swarming BA is responsible for destroying the unit. 

Once BA is swarming an enemy, they cannot do any other actions on their activation. The only options are to either activate the mount/swarm ability again (and thus stop swarming the enemy), or to end their activation ("Done" button). If you choose "Done", the BA will fire all active weapons at the unit they are swarming automatically.

The AI will also attempt to use Swarm against you. If an AI unit has BA (dictated by `AI_BattleArmorSpawnChance`, `BattleArmorFactionAssociations` and the unit has either HasBattleArmorMounts or InternalBattleArmorSquadCap > 0), some very ugly AI behavior patches should <i>try</i> to get the AI to move closer to your units. Once within a certain range, the AI BA will dismount from its carrier and attempt to swarm you if you're within range. If not, it'll just attack like a normal unit.

#### Countering Mount/Swarm

All mechs can be given one or two abilities in order to attempt to dislodge swarming BA, using the following settings. The AI will also attempt to dislodge player BA when swarming.

`BattleArmorDeSwarmRoll` - string, ability ID of pilot ability that allows mech to "roll" (forced self-knockdown) in order to dislodge swarming battle armor. <b>New in 2.0.1.2, the base success % is now exposed in the ability def</b>, for a final success % of BaseChance + (Piloting skill x 5%), capped at 95%. On a success, there is a 30% chance to smush the Battle Armor squad in the process. Ability is automatically granted to Mechs at contract start (e.g. does not need to be added manually to pilot). The ability can also set a statistic `BattleArmorDeSwarmerRollInitPenalty` to define an initiative penalty the BA will recieve on a successful deswarm.

`BattleArmorDeSwarmSwat` - string, ability ID of pilot ability that allows mech to "swat" swarming battle armor (remove using arms). <b>New in 2.0.1.2, the base success % is now exposed in the ability def</b>, for a final success % of BaseChance + (Piloting skill x 5%) - 5% for each "missing" arm actuator. An arm actuator is considered "missing" if it is destroyed, or was never mounted in the first place. Shoulder, Upper Arm, Lower Arm, and Hand for both left and right arms; thus a mech missing both arms would suffer a 40% penalty (8 x 5%). Ability is automatically granted to Mechs at contract start (e.g. does not need to be added manually to pilot).
The ability can also set a statistic `BattleArmorDeSwarmerRollInitPenalty` to define an initiative penalty the BA will recieve on a successful deswarm.
<b>Also new in 2.0.1.2</b>, the swat ability can deal damage directly to the swarming battle armor if a 2nd successful toll is made, if the statistic `BattleArmorDeSwarmerSwatDamage` is defined in the ability def (as below). If not defined, no damage will be done. To clarify, on activating a swat, a roll against `BaseChance + (Piloting skill x 5%) - 5% for each "missing" arm actuator` is made determine a successful swat. <b>Then</b> a 2nd roll against `BaseChance + (Piloting skill x 5%) - 5% for each "missing" arm actuator` is made to determine if the specified damage is dealt.

The two abilities must have at least the following (StratOps release contains these), although additional effects can certainly be added if developers wish to give Buffs/Debuffs when the ability is activated.

For rolls (the 2nd effectdata defining BattleArmorDeSwarmerSwatInitPenalty is optional):
	
```
{
	"Description": {
		"Id": "AbilityDefDeSwarmerRoll",
		"Name": "Roll",
		"Details": "ACTION: Unit will roll (self-knockdown) to remove swarming Battle Armor",
		"Icon": "rolling-energy"
	},
	"ActivationTime": "ConsumedByFiring",
	"Resource": "ConsumesActivation",
	"ActivationCooldown": -1,
	"Targeting": "ActorSelf",
	"ResolveCost": 0,
	"EffectData": [
		{
			"durationData": {
				"duration": 1,
				"stackLimit": 1
			},
			"targetingData": {
				"effectTriggerType": "OnActivation",
				"effectTargetType": "Creator",
				"showInStatusPanel": false
			},
			"effectType": "StatisticEffect",
			"Description": {
				"Id": "StatusEffect-DeSwarmRoll",
				"Name": "Battle Armor DeSwarmer Roll",
				"Details": "mount",
				"Icon": "uixSvgIcon_ability_precisionstrike"
			},
			"nature": "Buff",
			"statisticData": {
				"statName": "BattleArmorDeSwarmerRoll",
				"operation": "Set",
				"modValue": "0.5555",
				"modType": "System.Single"
			}
		},
		{
			"durationData": {
				"duration": 1,
				"stackLimit": 1
			},
			"targetingData": {
				"effectTriggerType": "OnActivation",
				"effectTargetType": "Creator",
				"showInStatusPanel": false
			},
			"effectType": "StatisticEffect",
			"Description": {
				"Id": "StatusEffect-DeSwarmRollInit",
				"Name": "Battle Armor DeSwarmer Roll",
				"Details": "mount",
				"Icon": "uixSvgIcon_ability_precisionstrike"
			},
			"nature": "Buff",
			"statisticData": {
				"statName": "BattleArmorDeSwarmerRollInitPenalty",
				"operation": "Set",
				"modValue": "2",
				"modType": "System.Int32"
			}
		}
	]
}
```

and for swats (the 2nd effectdata defining BattleArmorDeSwarmerSwatInitPenalty and 3rd effectdata defining BattleArmorDeSwarmerSwatDamage are optional)

```
{
	"Description": {
		"Id": "AbilityDefDeSwarmerSwat",
		"Name": "Swat",
		"Details": "ACTION: Unit will attempt to remove swarming Battle Armor with hands/limbs",
		"Icon": "hand"
	},
	"ActivationTime": "ConsumedByFiring",
	"Resource": "ConsumesActivation",
	"ActivationCooldown": -1,
	"Targeting": "ActorSelf",
	"ResolveCost": 0,
	"EffectData": [
		{
			"durationData": {
				"duration": 1,
				"stackLimit": 1
			},
			"targetingData": {
				"effectTriggerType": "OnActivation",
				"effectTargetType": "Creator",
				"showInStatusPanel": false
			},
			"effectType": "StatisticEffect",
			"Description": {
				"Id": "StatusEffect-DeSwarmSwat",
				"Name": "Battle Armor DeSwarmer Swat",
				"Details": "mount",
				"Icon": "uixSvgIcon_ability_precisionstrike"
			},
			"nature": "Buff",
			"statisticData": {
				"statName": "BattleArmorDeSwarmerSwat",
				"operation": "Set",
				"modValue": "0.3333",
				"modType": "System.Single"
			}
		},
		{
			"durationData": {
				"duration": 1,
				"stackLimit": 1
			},
			"targetingData": {
				"effectTriggerType": "OnActivation",
				"effectTargetType": "Creator",
				"showInStatusPanel": false
			},
			"effectType": "StatisticEffect",
			"Description": {
				"Id": "StatusEffect-DeSwarmRollInit",
				"Name": "Battle Armor DeSwarmer Roll",
				"Details": "mount",
				"Icon": "uixSvgIcon_ability_precisionstrike"
			},
			"nature": "Buff",
			"statisticData": {
				"statName": "BattleArmorDeSwarmerSwatInitPenalty",
				"operation": "Set",
				"modValue": "1",
				"modType": "System.Int32"
			}
		},
		{
			"durationData": {
				"duration": 1,
				"stackLimit": 1
			},
			"targetingData": {
				"effectTriggerType": "OnActivation",
				"effectTargetType": "Creator",
				"showInStatusPanel": false
			},
			"effectType": "StatisticEffect",
			"Description": {
				"Id": "StatusEffect-DeSwarmSwatDamage",
				"Name": "Battle Armor DeSwarmer Swat Damage",
				"Details": "mount",
				"Icon": "uixSvgIcon_ability_precisionstrike"
			},
			"nature": "Buff",
			"statisticData": {
				"statName": "BattleArmorDeSwarmerSwatDamage",
				"operation": "Set",
				"modValue": "100.0",
				"modType": "System.Single"
			}
		}
	]
}
```
