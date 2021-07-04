# StrategicOperations

This mod enables (and fixes), and expands the unused vanilla Command Abilities. There are two main "types" of abilities, Strafes and Spawns.

## Spawns

Spawns are basically what they sound like: spawning reinforcement units at the selected location. These units will be AI-controlled (allied). Exactly <i>what</i> unit gets deployed depends on a few things.

1) The "type" of the unit noted in `ActorResource` defines the type of unit that can be deployed by that ability. If it starts with "mechdef_", then mechs can be deployed. If it starts with "vehicledef_", then vehicles can be deployed. If it starts with "turretdef_" then turrets can be deployed.
2) Holding shift while activating the ability will bring up a popup with any available units to select for deployment (in addition to the default assigned in `ActorResource`). The player can obtain these "alternative" units by acquiring "deployment beacon" items. Deployment beacons can be of any upgrade type, but must have two things in their ComponentTags: they must have the tag "CanSpawnTurret", and they must have a tag with the ID of the unit they will enable for deployment, e.g. "mechdef_cicada_CDA-4G". The number of "probes" in the player inventory is the number of that unit that can be deployed in a given mission; i.e if you have 2 probes for mechdef_cicada_CDA-4G, you could deploy up to two of those Cicadas, and subsequent deployments would be limited to whatever unit is defined in the ability ActorResource.

Spawn ability json structure:
```
{
	"Description" : {
		"Id" : "AbilityDefCMD_UrbDrop",
		"Name" : "UrbDrop",
		"Details" : "DEPLOY MOBILE TURRET",
		"Icon" : "uixSvgIcon_genericDiamond"
	},
	"ActivationTime" : "CommandAbility",
	"Resource" : "CommandAbility",
	"ActivationCooldown" : -1,
	"NumberOfUses" : 1,
	"specialRules" : "SpawnTurret",
	"Targeting" : "CommandSpawnPosition",
	"ActorResource" : "mechdef_urbanmech_UM-R50",
	"StringParam1" : "vfxPrfPrtl_fireTerrain_smLoop",
	"IntParam2" : 250
}
```
The configurable parameters of the above:

`Description` - as any other Ability Description

`ActivationCooldown` - int, turn cooldown to use the ability again

`NumberOfUses` - int, number of times the ability can be used in a given contract (set to -1 or remove for unlimited uses)

`ActivationETA` - int, number of turns before the unit will be deployed.

`ActorResource` - string, the default "def" ID of the unit to be deployed.

`IntParam2` - int, the maximum distance from the initiating actor at which a unit can be deployed.


## Strafes

Strafes are also mostly what they sound like: an aerospace (or other) unit strafing the battlefield. Valid targets for a strafe are calculated within an AOE around a line drawn between two points (the "strafing run"), and can be either units or Objective buildings. Friendly units within the AOE can and will be hit by the strafing run!

The json structure of a strafe ability follows:

```
	"Description" : {
		"Id" : "AbilityDefCMD_Strafe",
		"Name" : "STRAFE",
		"Details" : "CALLS IN A STRAFING RUN BY YOUR AEROSPACE SUPPORT UNIT.",
		"Icon" : "uixSvgIcon_genericDiamond"
	},
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
	"IntParam1" : 5,
	"IntParam2" : 500
```

The configurable parameters of the above:

`Description` - as any other Ability Description

`ActivationCooldown` - int, turn cooldown to use the ability again

`NumberOfUses` - int, number of times the ability can be used in a given contract (set to -1 or remove for unlimited uses)

`ActivationETA` - int, number of turns before the actual incoming strafe will occur.

`FloatParam1` - float, the AOE radius in which a unit can be a valid target along the strafing run.

`FloatParam2` - float, the maximum length of the strafing run.

`ActorResource` - string, the default "def" ID of the unit doing the strafing.

`IntParam1` - int, number of "flares" that pop up to show the strafing effect area.

`IntParam2` - int, the maximum distance from the initiating actor at which a strafing run can be initialized OR ended! so you can't start a run at `IntParam2` and still go `FloatParam2` past it.
