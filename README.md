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
	"customSpawnReticleAsset": "select_spawn_reticle",
	"customSpawnReticleColor": {
		"r": 255,
		"g": 16,
		"b": 240
	},
	"MountIndicatorAsset": "Target",
	"MountIndicatorColor": {
		"r": 0,
		"g": 187,
		"b": 255
	},
	"strafeWaves": 3,
	"commandAbilities_AI": [
		{
			"AbilityDefID": "AbilityDefCMD_Strafe_AI",
			"FactionIDs": [
				"ClanGhostBear",
				"ClanWolf"
			],
			"AddChance": 0.0,
			"DiffMod": 0.0,
			"MaxUsersAddedPerContract": 0,
			"AvailableBeacons": [
				{
					"UnitDefID": "vehicledef_MECHBUSTER_AERO",
					"Weight": 10,
					"StrafeWaves": 3
				}
			]
		}
	],
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
	"DeswarmConfigs": {
			"AbilityDefDeSwarmerRoll": {
				"BaseSuccessChance": 0.55,
				"MaxSuccessChance": 0.95,
				"PilotingSuccessFactor": 0.05,
				"TotalDamage": 150,
				"Clusters": 5,
				"InitPenalty": 2
			},
			"AbilityDefDeSwarmerSwat": {
				"BaseSuccessChance": 0.33,
				"MaxSuccessChance": 0.95,
				"PilotingSuccessFactor": 0.05,
				"TotalDamage": 75,
				"Clusters": 3,
				"InitPenalty": 1
			}
		},
		"DeswarmMovementConfig": {
			"AbilityDefID": "AbilityDefDeSwarmerMovement",
			"BaseSuccessChance": 0.35,
			"MaxSuccessChance": 0.95,
			"EvasivePipsFactor": 0.1,
			"JumpMovementModifier": 1.2,
			"UseDFADamage": true,
			"LocationDamageOverride": 0,
			"PilotingDamageReductionFactor": 0.1
		},
	"BPodComponentIDs": [
		"Gear_B_Pod"
		],
	"BPodsAutoActivate": true,
	"BATargetEffects": [
		{
			"ID": "BA_AccurateFire",
			"TargetEffectType": "SWARM",
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
		{
			"ID": "BA_MountedFireDebuff",
			"TargetEffectType": "MOUNT_INT",
			"Name": "Battle Armor - Mounted Debuff",
			"description": "Battle Armor has decreased accuracy when firing while mounted.",
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
						"Details": "Battle Armor has decreased accuracy while mounted.",
						"Icon": "allied-star"
					},
					"nature": "Buff",
					"statisticData": {
						"statName": "AccuracyModifier",
						"operation": "Set",
						"modValue": "8",
						"modType": "System.Single"
					}
				}
			]
		},
		{
			"ID": "BA_MountedFireDebuff",
			"TargetEffectType": "MOUNT_EXT",
			"Name": "Battle Armor - Mounted Debuff",
			"description": "Battle Armor has decreased accuracy when firing while mounted.",
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
						"Details": "Battle Armor has decreased accuracy while mounted.",
						"Icon": "allied-star"
					},
					"nature": "Buff",
					"statisticData": {
						"statName": "AccuracyModifier",
						"operation": "Set",
						"modValue": "8",
						"modType": "System.Single"
					}
				}
			]
		},
		{
				"ID": "BA_GarrisonDefense",
				"TargetEffectType": "GARRISON",
				"Name": "Battle Armor - Garrisoned Defensive Bonuses",
				"Description": "Garrisoned units have Defensive bosnuses.",
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
							"Id": "BA_GartrisonDamageResistance",
							"Name": "AOE Immune",
							"Details": "arrisoned units have 30% additional damage resist.",
							"Icon": "allied-star"
						},
						"nature": "Buff",
						"statisticData": {
							"statName": "DamageReductionMultiplierAll",
							"operation": "Float_Add",
							"modValue": "-0.3",
							"modType": "System.Single"
						}
					},
					{
						"durationData": {},
						"targetingData": {
							"effectTriggerType": "Passive",
							"effectTargetType": "Creator",
							"showInStatusPanel": true
						},
						"effectType": "StatisticEffect",
						"Description": {
							"Id": "BA_GartrisonDamageResistance",
							"Name": "AOE Immune",
							"Details": "arrisoned units have 30% additional damage resist.",
							"Icon": "allied-star"
						},
						"nature": "Buff",
						"statisticData": {
							"statName": "ToHitThisActor",
							"operation": "Float_Add",
							"modValue": "5.0",
							"modType": "System.Single"
						}
					}
				]
			}
	],
	"AirliftTargetEffects": [
			{
				"ID": "Airlift_Accuracy",
				"FriendlyAirlift": true,
				"Name": "Airlift Airlift_Accuracy",
				"Description": "Airlifted Units get extra accuracy because t-bone needed to test something.",
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
							"Id": "Airlift_Accuracy_Bonus",
							"Name": "Easy shots",
							"Details": "Airlift_Accuracy has increased accuracy while airlift.",
							"Icon": "allied-star"
						},
						"nature": "Buff",
						"statisticData": {
							"statName": "AccuracyModifier",
							"operation": "Set",
							"modValue": "-9001",
							"modType": "System.Single"
						}
					}
				]
			}
		],
		"OnGarrisonCollapseEffects": [
			{
				"ID": "GarrisonCollapseAccuracy",
				"TargetEffectType": "GARRISON",
				"Name": "Accuracy Penalty",
				"Description": "Accuracy Penalty when building collapse",
				"effectDataJO": [
					{
						"durationData": {
							"duration": 2,
							"stackLimit": 1
						},
						"targetingData": {
							"effectTriggerType": "Passive",
							"effectTargetType": "Creator",
							"showInStatusPanel": true
						},
						"effectType": "StatisticEffect",
						"Description": {
							"Id": "Garrison_Collapse_accuracy",
							"Name": "hard shots",
							"Details": "Garrison_Collapse_accuracy has worse accuracy after collpase.",
							"Icon": "allied-star"
						},
						"nature": "Buff",
						"statisticData": {
							"statName": "AccuracyModifier",
							"operation": "Float_Add",
							"modValue": "10",
							"modType": "System.Single"
						}
					}
				]
			}
		],
	"BattleArmorFactionAssociations": [
		{
			"FactionIDs": [
				"ClanGhostBear",
				"ClanWolf"
			],
			"SpawnChanceBase": 0.3,
			"SpawnChanceDiffMod": 0.05,
			"MaxSquadsPerContract": 4,
			"InternalBattleArmorWeight": {
				"mechdef_ba_is_standard": 5,
				"mechdef_ba_marauder": 2
			},
			"MountedBattleArmorWeight": {
				"mechdef_ba_is_standard": 5,
				"mechdef_ba_infiltratormkii": 2,
				"BA_EMPTY": 3
			},
			"HandsyBattleArmorWeight": {
				"mechdef_ba_marauder": 1,
				"BA_EMPTY": 2
			}
		}
	],
	"AttackOnSwarmSuccess": true,
	"AI_BattleArmorExcludedContractTypes": [
			"DuoDuel",
			"SoloDuel"
		],
	"AI_BattleArmorExcludedContractIDs": [],
	"BeaconExcludedContractTypes": [
			"DuoDuel",
			"SoloDuel"
		],
	"BeaconExcludedContractIDs": [],
	"UsingMechAffinityForSwarmBreach": true,
	"AirliftAbilityID": "AbilityDefAirliftActivate",
	"AirliftCapacityByTonnage": false,
	"CanDropOffAfterMoving": true
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

`strafeVelocityDefault` - float, default velocity of strafing unit <i>while strafing</i>. <s>The faster the unit moves, the fewer targets it will be able to hit during a strafe.</s> If MaxSpeed is > 0 in the strafing unit, then that speed will override this value.

`strafeAltitudeMin` and `strafeAltitudeMax` - float. The altitide of the strafing unit is the maximum weapon range of the strafing unit divided by 4, but is clamped between these two values.

`strafePreDistanceMult` - float, controls the distance at which the strafing unit is instantiated from the point of strafing start; influences the length of the "fly-in" sequence.

`strafeMinDistanceToEnd` - float, distance from the strafing unit to the endpoint of the strafe at which the strafe is considered to be "complete" and no more targets will be attacked.

`timeBetweenAttacks` - float. minimum amount of time that must elapse before strafing unit can instantiate another attack. 0.35 is HBS' default, can probably go as low as 0.1 without strange things happening. really only a minor tweak if you think the strafing units arent attacking "enough", but velocity and elevation are likely more important.

`commandUseCostsMulti` - float, multiplier governing costs of using command abilities. if >0, the cost of the unit being used (as defined in the unit def) is multiplied by this value to obtain a per-use cost of using the ability. should probably only be used if beacons are not set to be consumed, or at least set to some low value. **this cost will stack with any manually defined costs in the AbilityDef using Abilifier**

`deploymentBeaconEquipment` - List<string>, list of component IDs that are considered "deployment beacons" to give options for the specific unit that gets deployed/strafes during combat.

`customSpawnReticleColor` - new type, defines custom color of reticle used for spawns. fields r, g, b, are RGB values, 0-255.
	
`customSpawnReticleAsset` string. name of custom .DDS asset that will be used for Spawn reticle (needs to be one that is added to manifest via modtek)

`MountIndicatorAsset` - string. name of custom .DDS asset that will be used for Mount indicator when using Battle Armor (needs to be one that is added to manifest via modtek)
	
`MountIndicatorColor` - as `customSpawnReticleColor`, defines custom color of reticle used for Mount indicator. fields r, g, b, are RGB values, 0-255.
	
`strafeWaves` - int, default number of units (same unit copied multiple times) that will perform a strafe. e.g., if set to 3 and strafe calls a Lightning Aerospace fighter, 3 Lightnings will strafe the target area in succession. They tend to target exactly the same units (unless of course one of the targeted units gets destroyed by one of the previous strafing units). Overriden by mechcomponent tags in beacons where tag is "StrafeWaves_X" where X is the number of waves. E.g. a beacon with tag `StrafeWaves_5` would strafe with 5 units.

<s>"AI_FactionBeacons"`: essentially functions the same as `deploymentBeaconEquipment`, but restricts factions listed to the corresponding equipment. if a faction is not listed in here, it will use only the "default" unit listed in a given command ability.</s> **deprecated in 2.0.2.8**

`commandAbilities_AI` - **Format Change in v2.0.2.8** - Changed to similar format as `BattleArmorFactionAssociations`. Can be used to give AI strafe and spawn (Beacon) abilities. Obviously the StrafeWaves field only applies to strafes, and will do nothing for beacons. MaxUsersAddedPerContract limit is based on only *this* ability, and is separate *per faction*. I.e, in a 3-way contract with you, ClanGhostBear and ClanWolf, both Ghost Bear and Wolf could get 3 units each with AbilityDefCMD_Strafe_AI.

```

"commandAbilities_AI": [
	{
		"AbilityDefID": "AbilityDefCMD_Strafe_AI",
		"FactionIDs": [
			"ClanGhostBear",
			"ClanWolf"
		],
		"AddChance": 0.5,
		"DiffMod": 0.05,
		"MaxUsersAddedPerContract": 3,
		"AvailableBeacons": [
			{
				"UnitDefID": "vehicledef_MECHBUSTER_AERO",
				"Weight": 10,
				"StrafeWaves": 3
			}
		]
	}
],

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
	
`BattleArmorDeSwarmMovement` - support for new ability giving units a chance to deswarm BA from movement. See below for details.

`ArmActuatorCategoryIDs` - list of strings, Custom Category IDs identifying actuators that will be considered in `BattleArmorDeSwarmSwat` calculations. E.g, if ArmActuator is listed here and an actuator has the following, it would be counted:
```
{
	"Custom": {
		"Category" : [
			{"CategoryID": "ArmActuator"},
			{"CategoryID": "NonQuad"}
		],
```
**new/changed in 3.0.0.0**
`DeswarmConfigs` : dictionary - configurations for Deswarm abilities used by mechs; variables previously set in the AbilityDef are now controlled here:
	- dictionary "key" is the abilityDef ID (i.e AbilityDefDeSwarmerRoll)
	- BaseSuccessChance and MaxSuccessChance control the min and maximum success chance
	- Piloting success factor is modifier for piloting skill added to success chance (swat ability also still uses # of intact actuators as before)
	- TotalDamage total amount of damage dealt to suit on successful roll
	- Clusters - TotalDamage is divided into this man clusters, and dealt randomly to the squad
	- InitPenalty - int penalty

`DeswarmMovementConfig` : config for movement-based deswarming (erratic maneuvers)
	- AbilityDefID - ability def ID of the ability
	- BaseSuccessChance and MaxSuccessChance as above
	- EvasivePipsFactor success factor added based on evasive pips generated by the move
	- JumpMovementModifier added if move is a jump
	- UseDFADamage bool, if true will use chassis dfa damage to deal damage on success (for jump moves)
	- LocationDamageOverride- damage to be dealt if UseDFADamage = false
	- PilotingDamageReductionFactor = damage will be reduced by (this value x piloting)%. So setting to 0.1 and a unit with Piloting 6 would only take 40% ("60% less") damage.


`BPodComponentIDs` - List of string componenet IDs for CAE gear that, when activated, will function as B-Pods, detonating and damaging only Battle Armor.

`BPodsAutoActivate` - bool, if true BPod Components will auto-activate when the unit carrying them gets swarmed. If false, only BPods mounted on AI-controlled units will auto-activate (players must manually activate BPods using CAE activation).

**new/changed in 3.0.0.0**
`BATargetEffects` - **Renamed BATargetEffects and changed to list in 2.0.3.1**  Effects which will be applied to Battle Armor while swarming or mounting, depending on value of `TargetEffectType` field. Can be GARRISON, SWARM, MOUNT_INT, MOUNT_EXT, or BOTH. Intended use is to improve accuracy and clustering of swarming BA so they always (usually, mostly) hit the same location on the unit they're swarming, and give mounted BA a debuff to accuracy when firing from mount. Of course you can add whatever else you want here. MOUNT_INT will take effect only when mounted internally, while MOUNT_EXT will take effect only when mounted externally. SWAM will only take effect when swarming, and BOTH will always take effect. GARRISON will take effect only when units are occupying a building.

**new/changed in 3.0.0.0**
`AirliftTargetEffects` - effects that will take effect when a unit is airlifted. If FriendlyAirlift = true, will only take effect when being airlifted by firendly unit. If FriendlyAirlift = false, will only take effect when being airlifted by hostile unit.

**new/changed in 3.0.0.0**
`OnGarrisonCollapseEffects` - effects that will effect squad <i>when a garrisoned building collapses</i>. intended for debuffs, etc.
	
<s>`AI_BattleArmorSpawnChance` - float, base probability that AI units that <i>can</i> mount Battle Armor, either mounted externally or internally, will get Battle Armor at contract start. Note that any added Battle Armor is independent of any "support lance" or "extra lance" settings in Mission Control or other mods. Added to AI_BattleArmorSpawnDiffMod for total chance.</s> DEPRECATED, IMPLEMENTED IN BattleArmorFactionAssociations

<s>AI_BattleArmorSpawnDiffMod - float, contract difficulty is multiplied by this value and added to AI_BattleArmorSpawnChance to determine probability of AI units spawning BA.</s> DEPRECATED, IMPLEMENTED IN BattleArmorFactionAssociations

`BattleArmorFactionAssociations` - Faction information for AI battle armor spawning. <b>This has been revamped in v2.0.1.8</b>.
	
FactionIDs is a list of faction IDs for which this particular config will be applied. If a faction is not present in any configs, it will not spawn Battle Armor. If a faction is present in multiple configs, only the first config in the list will be used for that faction.
	
Using the following settings, ClanGhostBear and ClanWolf have baseline 30% chance to spawn Battle Armor, + 5% per difficulty level, with rolls against that occurring separately for internal mounting space (i.e. APCs), external mounts (i.e. Omnimechs), and for conventional (non-omni) mechs pulling from `HandsyBattleArmorWeight`. For units with internal mounting space, each internal slot is rolled separately. For all "mounting types", an entry `BA_EMPTY` can be used to further tweak the spawn %; if BA_EMTPY is chosen, no BA will spawn for that "mounting type." For example using the below settings a unit without internal storage or BA mounts would have only `0.33 x base%+difficulty%` calculated chance to actually spawn `mechdef_ba_marauder`, while a unit with BA mounts would have `0.7 x base%+difficulty%` calculated chance to spawn battle armor. Lastly, `MaxSquadsPerContract` defines an upper limit for the # of BA squads a given faction can spawn during a contract.

```
"BattleArmorFactionAssociations": [
			{
				"FactionIDs": [
					"ClanGhostBear",
					"ClanWolf"
				],
				"SpawnChanceBase": 0.3,
				"SpawnChanceDiffMod": 0.05,
				"MaxSquadsPerContract": 4,
				"InternalBattleArmorWeight": {
					"mechdef_ba_is_standard": 5,
					"mechdef_ba_marauder": 2
				},
				"MountedBattleArmorWeight": {
					"mechdef_ba_is_standard": 5,
					"mechdef_ba_infiltratormkii": 2,
					"BA_EMPTY": 3
				},
				"HandsyBattleArmorWeight": {
					"mechdef_ba_marauder": 1,
					"BA_EMPTY": 2
				}
			}
		],
```

`AttackOnSwarmSuccess` - bool, if true BA will initiate an attack sequence on a successful swarming attempt (rather than needing to wait until the subsequence activation)

`AI_BattleArmorExcludedContractTypes` - List of ContractTypes where AI is not allowed to spawn mounted Battle Armor

`AI_BattleArmorExcludedContractIDs` - List of contract IDs where AI is not allowed to spawn mounted Battle Armor
	
`BeaconExcludedContractTypes` - List of ContractTypes where deployment or strafing beacons are not allowed to be used

`BeaconExcludedContractIDs` - List of contract IDs where deployment or strafing beacons are not allowed to be used

**new/changed in 3.0.0.0**
`UsingMechAffinityForSwarmBreach` - use MechAffinity implementation to give BA swarms breaching shot (if using MechAffinity, need to have BATargetEffect on swarm that sets the appropriate stat to true).

`AirliftAbilityID` - abilityDef ID for airlift ability

`AirliftCapacityByTonnage` - if true, airlift capacity will be determined by tonnage rather than absolute # of units

`CanDropOffAfterMoving` - if true, airlift units can move and _then_ drop their airlifted units at final location. if false, airlifted units must drop units before moving that round.

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

By default, the above ability will only allow the Battle Armor to <i>mount</i> friendly units. In order to be able to <i>swarm</i> hostile units, the Battle Armor must also have a stat effect from equipment that sets a stat `CanSwarm` to True, e.g. the following:
	
```
{
			"durationData": {
				"duration": -1,
				"stackLimit": -1
			},
			"targetingData": {
				"effectTriggerType": "Passive",
				"effectTargetType": "Creator",
				"showInTargetPreview": true,
				"showInStatusPanel": true
			},
			"effectType": "StatisticEffect",
			"Description": {
				"Id": "CanSwarmStat",
				"Name": "This Battle Armor can make swarm attacks.",
				"Details": "This Battle Armor can make swarm attacks."
			},
			"statisticData": {
				"statName": "CanSwarm",
				"operation": "Set",
				"modValue": "true",
				"modType": "System.Boolean"
			}
		},	
```
	
In order to have Battle Armor mounted <i>to</i> it, a unit must have either stat effect that sets bool `HasBattleArmorMounts` to true OR must have the integer stat `InternalBattleArmorSquadCap` set to the # of Battle Armor squads that can be carried internally (for APCs and such). For AI units, those are the two stats that further dictate whether BA can be spawned.
	- Battle Armor with AbstractActor statistic bool `BattleArmorInternalMountsOnly` can only mount internally (i.e. inside vehicles) and cannot use conventional Battle Armor omni mounts.

On the player-facing side, an additional bool stat, `IsBattleArmorHandsy` can be added to <i>Battle Armor</i> that would allow BA to mount friendly units <i>regardless of</i> `HasBattleArmorMounts`. 
This was added to allow Battle Armor such as the Marauder BA that canonically have Magnetic Clamps to allow them to ride on <i>any</i> friendly unit.


- Units with AbstractActor statistic `IsUnmountableBattleArmor` are <i>never</i> mountable, even by BA with `IsBattleArmorHandsy`.
- Units with AbstractActor statistic `IsUnswarmableBattleArmor` are <i>never</i> swarmable (i.e, LAMs in LAM mode, or VTOLs)

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

Selecting the Mount/Swarm ability when the unit is already mounted/swarming will auto-select the unit you are mounted to; now just click Confirm to dismount/stop swarming. Selecting the Mount/Swarm ability when *not* mounted/swarming will display indicators showing valid targets for mounting and swarming. Swarm targets will all display the standard "melee target" indicator. Mount targets will get an icon defined in the `MountIndicatorAsset` setting, using the color defined in `MountIndicatorColor`
	
Attempts to mount a friendly unit are always successful. Attempts to swarm an enemy unit make a simplified melee roll to determine success. If unsuccessful, the BA will be deposited in the same hex as the unit they attempted to swarm. On both a swarm success and failure, a floatie will be generated indicating success or failure. Attempts to mount or swarm always end the BA activation, and must be conducted <i>before</i> the BA attempts to move.

BA that is either swarming or mounted is noted in the "carrier" unit's armor paperdoll, as well as the current armor/structure of the BA mounted to a specific location. The individual suits of the BA squad are always distributed "evenly" across a mech in the following order: CT, CT-R, RT, RT-R, LT, LT-R for mounts (only a single squad can be mounted), and CT, CT-R, RT, RT-R, LT, LT-R, LA, RA, LL, RL, HD for swarms. Multiple squads can swarm a mech simultaneously, and will double-up on locations as needed. For vehicles the order is Front, Rear, Left, Right, Turret for both swarms and mounts. Incoming attacks targeting the "carrier" have a 33% chance of impacting the BA suit mounted to that location instead of the "carrier", with excess damaging transfering through the BA to the carrier.

If the mech chassis location where BA is mounted is destroyed, any BA mounted to that location has a 33% chance of also being destroyed except when the swarming BA is responsible for destroying that location. If the entire unit to which BA is mounted is destroyed, any BA mounted to that unit has a 33% chance of also being destroyed except when the swarming BA is responsible for destroying the unit. 

Once BA is swarming an enemy, they cannot do any other actions on their activation. The only options are to either activate the mount/swarm ability again (and thus stop swarming the enemy), or to end their activation ("Done" button). If you choose "Done", the BA will fire all active weapons at the unit they are swarming automatically.

The AI will also attempt to use Swarm against you. If an AI unit has BA (dictated by `AI_BattleArmorSpawnChance`, `BattleArmorFactionAssociations` and the unit has either HasBattleArmorMounts or InternalBattleArmorSquadCap > 0), some very ugly AI behavior patches should <i>try</i> to get the AI to move closer to your units. Once within a certain range, the AI BA will dismount from its carrier and attempt to swarm you if you're within range. If not, it'll just attack like a normal unit.

#### Firing Ports

Carrier units with actor bool statistic `HasFiringPorts` set to true will allow mounted BA to fire at enemies within range <i>while they are mounted</i>. Units so mounted will have the same LOF level as the carrier unit.

#### Garrisons - new in v3.0.0.0

BA can now occupy buildings. Each building can only hold a single squad of BA because reasons. Because getting the AI to shoot at the damn _building_ is a pain in the ass, I've chosen to allow the AI to target the BA squads directly; however the BA squads <i>also</i> get the building HP added to their own armor (divided amongst the squad). If the building itself is destroyed with the BA still occupying it, they take their chassis DFA self-damage to each location, and receive any effects defined in `OnGarrisonCollapseEffects`. If players choose to voluntarily exit the garrisoned building, they will exfil to a random adjacent hex. I am not planning on giving the AI the ability to garrison buildings at this time; this is strictly a player gimmick.

#### Countering Mount/Swarm

**B-Pods Added in v2.0.2.0**

Any CAE equipment whose ID is found in `BPodComponentIDs` will function as a B-Pod; when activated it will automatically hit all Battle Armor within the radius defined by the "Range" field within the "Explosion" section of the "ActivatableComponent" portion of the equipment, i.e
```
"ActivatableComponent": {
			"Explosion": {
				"Range": 150,
				"Damage": 100,
```
All battle armor squads within 150m would then take 100 damage, divided randomly amongst the suits in the squad, and any enemy battle armor squads within that radius that are currently swarming will be forcibly dismounted.
	
In addition, to B-Pods, all mechs can be given one or two abilities in order to attempt to dislodge swarming BA, using the following settings. The AI will also attempt to dislodge player BA when swarming.

**note changes in settings rundown earlier in this document; behavior is now controlled by values in DeswarmConfigs**
`BattleArmorDeSwarmRoll` - string, ability ID of pilot ability that allows mech to "roll" (forced self-knockdown) in order to dislodge swarming battle armor. On a success, there is a 30% chance to deal cluster damage to the Battle Armor squad in the process, as defined in the DeswarmConfig settings. Ability is automatically granted to Mechs at contract start (e.g. does not need to be added manually to pilot).

`BattleArmorDeSwarmSwat` - string, ability ID of pilot ability that allows mech to "swat" swarming battle armor (remove using arms). In addition to base chance and piloting mopifier, swat chance gets -5% for each "missing" arm actuator. An arm actuator is considered "missing" if it is destroyed, or was never mounted in the first place. Shoulder, Upper Arm, Lower Arm, and Hand for both left and right arms; thus a mech missing both arms would suffer a 40% penalty (8 x 5%). Ability is automatically granted to Mechs at contract start (e.g. does not need to be added manually to pilot).
The swat ability can deal damage directly to the swarming battle armor if a 2nd successful toll is made, iOn activating a swat, a roll against `BaseChance + (Piloting skill x 5%) - 5% for each "missing" arm actuator` is made determine a successful swat. <b>Then</b> a 2nd roll against `BaseChance + (Piloting skill x 5%) - 5% for each "missing" arm actuator` is made to determine if the specified damage is dealt.

The two abilities must have at least the following (StratOps release contains these), although additional effects can certainly be added if developers wish to give Buffs/Debuffs when the ability is activated. **new in v3.0.0.0, no effectData are necessary in these abilities**

For rolls
	
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
	"EffectData": []
}
```

and for swats

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
	"EffectData": []
}
```

**New in 2.0.3.1** **updated in v3.0.0.0
	
Deswarm by movement behavior is controlled by the `DeswarmMovementConfig` setting, rather than statistics in the abilityDef.
	
`AbilityDefID` - string, ability def ID for new ability giving units a chance to deswarm BA from movement. Usable by mechs AND vehicles.

`BaseSuccessChance` - Minimum (or starting) % to successfully deswarm on movement.

`MaxSuccessChance` - Maximum % to successfully deswarm on movement.

`EvasivePipsFactor` - Value set here is multiplied by # of evasive pips gained from movement and added to MinChance

`JumpMovementModifier` - Value here is multiplier on chance from above if the movement was a jump (do a barrel roll!)

`UseDFADamage` bool, if true will use chassis dfa damage to deal damage on success (for jump moves)

`LocationDamageOverride` - damage to be dealt if UseDFADamage = false

`PilotingDamageReductionFactor`  damage will be reduced by (this value x piloting)%. So setting to 0.1 and a unit with Piloting 6 would only take 40% ("60% less") damage.

After unit movement is complete, roll processes to determine if de-swarm occurs. IF so, the swarming BA is deposited randomly along the movement path. If move was a jump, the swarming BA will also take damage as defined above to each suit in the squad.
	
```
{
	"Description": {
		"Id": "AbilityDefDeSwarmerMovement",
		"Name": "Erratic Maneuvering",
		"Details": "ACTION: Do fancy footworks and barrel rolls to get rid of little grabby bois. But it's hard to shoot straight.",
		"Icon": "uixSvgIcon_skullAtlas"
	},
	"ActivationTime": "ConsumedByFiring",
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
				"showInStatusPanel": true
			},
			"effectType": "StatisticEffect",
			"Description": {
				"Id": "StatusEffect-BattlemasterAccuracy",
				"Name": "Overcharged Targeting",
				"Details": "-6 Accuracy",
				"Icon": "uixSvgIcon_skullAtlas"
			},
			"nature": "Buff",
			"statisticData": {
				"statName": "AccuracyModifier",
				"operation": "Float_Add",
				"modValue": "6",
				"modType": "System.Single",
				"additionalRules": "NotSet",
				"targetCollection": "Weapon",
				"targetWeaponCategory": "NotSet",
				"targetWeaponType": "NotSet",
				"targetAmmoCategory": "NotSet",
				"targetWeaponSubType": "NotSet"
			}
		}
	],
	"Priority": 0
}
```
