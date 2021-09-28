# StrategicOperations

**Depends On Abilifier v1.1.0.2 or higher!** 

This mod enables, fixes, and expands the unused vanilla Command Abilities. There are two main "types" of abilities, Strafes and Spawns.

settings in the mod.json:

```
"enableLogging": true,
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
"customSpawnReticleAsset": "select_spawn_reticle"
```

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
	
`strafeWaves` - int, number of units (same unit copied multiple times) that will perform a strafe. e.g., if set to 3 and strafe calls a Lightning Aerospace fighter, 3 Lightnings will strafe the target area in succession. They tend to target exactly the same units (unless of course one of the targeted units gets destroyed by one of the previous strafing units)

`commandAbilities_AI` - list of command abilities the AI can get, as well as the probability and difficulty modifier to that probability that a given AI unit will be given the corresponding ability. e.g for the following setting, any given AI unit will have a 5% + 1% per-difficulty chance of being given `AbilityDefCMD_Strafe` at contract start.
```

{
	"AbilityID": "AbilityDefCMD_Strafe",
	"AddChance": 0.05,
	"DiffMod": 0.01
}
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

`AI_BattleArmorSpawnChance` - float, probability that AI units that <i>can</i> mount Battle Armor, either mounted externally or internally, will get Battle Armor at contract start. Note that this is independent of any "support lance" or "extra lance" settings in Mission Control or other mods.

`BattleArmorFactionAssociations` - Faction information for AI battle armor spawning. If a faction does not have an entry here, it will not spawn Battle Armor. E.g, using the following setting the only faction that would spawn Battle Armor would be ClanGhostBear, and the only battle armor it would spawn is `mechdef_ba_is_standard`.

```
"BattleArmorFactionAssociations": {
			"ClanGhostBear": [
				"mechdef_ba_is_standard"
			]
		}
```

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
	"StringParam1" : "vfxPrfPrtl_fireTerrain_smLoop",
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
	"StringParam1" : "vfxPrfPrtl_fireTerrain_smLoop",
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
