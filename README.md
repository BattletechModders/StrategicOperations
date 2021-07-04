# StrategicOperations

This mod enables (and fixes), and expands the unused vanilla Command Abilities. There are two main "types" of abilities, Strafes and Spawns.

## Strafes

Strafes are mostly what they sound like: an aerospace (or other) unit strafing the battlefield. There are a few quirks about strafes. 1st, valid targets for a strafe are calculated within an AOE around a line drawn between 2 points. Simple, right?

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

`ActivationETA` - int, number of turns before the actual incoming strafe will occur.

`FloatParam1` - float, SHIT WHAT IS THIS
