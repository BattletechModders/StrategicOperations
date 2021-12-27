﻿using System;
using System.Collections.Generic;
using System.Linq;
using Abilifier;
using BattleTech;
using BattleTech.Data;
using CustomUnits;
using UnityEngine;

namespace StrategicOperations.Framework
{
    public class AI_Utils
    {

        public static int EvaluateStrafing(AbstractActor actor, out Ability ability, out Vector3 startpoint, out Vector3 endpoint)
        {
            startpoint = default(Vector3);
            endpoint = default(Vector3);
            if (!CanStrafe(actor, out ability)) return 0;
            var assetID = AssignRandomSpawnAsset(ability, actor.team.FactionValue.Name, out var waves);
            var dmg = Mathf.RoundToInt(CalcExpectedDamage(actor, assetID) * waves);

            var targets = TargetsForStrafe(actor, ability,out startpoint, out endpoint);

            return dmg * targets;
        }

        public static void GenerateAIStrategicAbilities(AbstractActor unit)
        {
            if (unit is Turret) return;
            if (unit.team.IsLocalPlayer) return;
            var dm = unit.Combat.DataManager;

            //check for BA equipment. if present, we're going to spawn BA and mount it to AI
            
            ModInit.modLog.LogMessage($"Checking if unit {unit.DisplayName} {unit.GUID} should spawn Battle Armor.");

            if (ModInit.modSettings.BattleArmorFactionAssociations.Any(x => x.FactionIDs.Contains(unit.team.FactionValue.Name)))
            {
                if (!ModState.CurrentBattleArmorSquads.ContainsKey(unit.team.FactionValue.Name))
                {
                    ModState.CurrentBattleArmorSquads.Add(unit.team.FactionValue.Name, 0);
                }
                var baConfig = ModInit.modSettings.BattleArmorFactionAssociations.FirstOrDefault(x => x.FactionIDs.Contains(unit.team.FactionValue.Name));
                if (baConfig == null)
                {
                    ModInit.modLog.LogError($"[GenerateAIStrategicAbilities] - something broken trying to process BA Faction Association. baConfig was null.");
                    return;
                }
                ModInit.modLog.LogTrace($"Found config for {unit.team.FactionValue.Name}.");

                var baLance = Utils.CreateOrFetchCMDLance(unit.team);
                var spawnChance = baConfig.SpawnChanceBase +
                                  (unit.Combat.ActiveContract.Override.finalDifficulty *
                                   baConfig.SpawnChanceDiffMod);
                var internalSpace = unit.getAvailableInternalBASpace();
                if (internalSpace > 0)
                {
                    ModInit.modLog.LogTrace($"Unit has {internalSpace} internal space.");
                    for (int i = 0; i < internalSpace; i++)
                    {
                        var chosenInt = baConfig.ProcessBattleArmorSpawnWeights(unit.team.FactionValue.Name, "InternalBattleArmorWeight");
                        if (!string.IsNullOrEmpty(chosenInt))
                        {
                            var baRollInt = ModInit.Random.NextDouble();
                            if (baRollInt <= spawnChance)
                            {
                                ModInit.modLog.LogMessage($"Roll {baRollInt} <= {spawnChance}, choosing BA from InternalBattleArmorWeight for slot {i} of {internalSpace}.");
                                if (chosenInt != "BA_EMPTY")
                                {
                                    if (ModState.CurrentBattleArmorSquads[unit.team.FactionValue.Name] < baConfig.MaxSquadsPerContract)
                                    {
                                        SpawnUtils.SpawnBattleArmorAtActor(unit, chosenInt, baLance);
                                        ModState.CurrentBattleArmorSquads[unit.team.FactionValue.Name] += 1;
                                        ModInit.modLog.LogMessage($"Spawning {chosenInt}, incrementing CurrentBattleArmorSquads to {ModState.CurrentBattleArmorSquads[unit.team.FactionValue.Name]}.");
                                    }
                                    else
                                    {
                                        ModInit.modLog.LogMessage($"{ModState.CurrentBattleArmorSquads[unit.team.FactionValue.Name]} is max {baConfig.MaxSquadsPerContract} per contract.");
                                    }
                                }
                                else
                                {
                                    ModInit.modLog.LogMessage($"Chose {chosenInt}.");
                                }
                            }
                            else
                            {
                                ModInit.modLog.LogMessage($"Roll {baRollInt} > {spawnChance}, not adding BA internally.");
                            }
                        }
                        else
                        {
                            ModInit.modLog.LogMessage($"No config for internal BA for faction {unit.team.FactionValue.Name}.");
                        }
                    }
                }
                else
                {
                    ModInit.modLog.LogTrace($"Unit dont has internal space.");
                }

                if (unit.getHasBattleArmorMounts())
                {
                    ModInit.modLog.LogTrace($"Unit has mounts.");

                    var chosenMount = baConfig.ProcessBattleArmorSpawnWeights(unit.team.FactionValue.Name, "MountedBattleArmorWeight");
                    if (!string.IsNullOrEmpty(chosenMount))
                    {
                        var baRollMount = ModInit.Random.NextDouble();
                        if (baRollMount <= spawnChance)
                        {
                            ModInit.modLog.LogMessage($"Roll {baRollMount} <= {spawnChance}, choosing BA from MountedBattleArmorWeight.");
                            if (chosenMount != "BA_EMPTY")
                            {
                                if (ModState.CurrentBattleArmorSquads[unit.team.FactionValue.Name] < baConfig.MaxSquadsPerContract)
                                {
                                    SpawnUtils.SpawnBattleArmorAtActor(unit, chosenMount, baLance);
                                    ModState.CurrentBattleArmorSquads[unit.team.FactionValue.Name] += 1;
                                    ModInit.modLog.LogMessage($"Spawning {chosenMount}, incrementing CurrentBattleArmorSquads to {ModState.CurrentBattleArmorSquads[unit.team.FactionValue.Name]}.");
                                }
                                else
                                {
                                    ModInit.modLog.LogMessage($"{ModState.CurrentBattleArmorSquads[unit.team.FactionValue.Name]} is max {baConfig.MaxSquadsPerContract} per contract.");
                                }
                            }
                            else
                            {
                                ModInit.modLog.LogMessage($"Chose {chosenMount}.");
                            }
                        }
                        else
                        {
                            ModInit.modLog.LogMessage($"Roll {baRollMount} > {spawnChance}, not adding BA to mounts.");
                        }
                    }
                    else
                    {
                        ModInit.modLog.LogMessage($"No config for mounted BA for faction {unit.team.FactionValue.Name}.");
                    }
                }
                else
                {
                    ModInit.modLog.LogTrace($"Unit dont has mounts.");
                    var chosenHandsy = baConfig.ProcessBattleArmorSpawnWeights(unit.team.FactionValue.Name, "HandsyBattleArmorWeight");
                    if (!string.IsNullOrEmpty(chosenHandsy))
                    {
                        var baRollHandsy = ModInit.Random.NextDouble();
                        if (baRollHandsy <= spawnChance)
                        {
                            ModInit.modLog.LogMessage($"Roll {baRollHandsy} <= {spawnChance}, choosing BA from HandsyBattleArmorWeight.");
                            if (chosenHandsy != "BA_EMPTY")
                            {
                                if (ModState.CurrentBattleArmorSquads[unit.team.FactionValue.Name] <
                                    baConfig.MaxSquadsPerContract)
                                {
                                    SpawnUtils.SpawnBattleArmorAtActor(unit, chosenHandsy, baLance);
                                    ModState.CurrentBattleArmorSquads[unit.team.FactionValue.Name] += 1;
                                    ModInit.modLog.LogMessage($"Spawning {chosenHandsy}, incrementing CurrentBattleArmorSquads to {ModState.CurrentBattleArmorSquads[unit.team.FactionValue.Name]}.");
                                }
                                else
                                {
                                    ModInit.modLog.LogMessage($"{ModState.CurrentBattleArmorSquads[unit.team.FactionValue.Name]} is max {baConfig.MaxSquadsPerContract} per contract.");
                                }
                            }
                            else
                            {
                                ModInit.modLog.LogMessage($"Chose {chosenHandsy}.");
                            }
                        }
                        else
                        {
                            ModInit.modLog.LogMessage($"Roll {baRollHandsy} > {spawnChance}, not adding handsy BA.");
                        }
                    }
                    else
                    {
                        ModInit.modLog.LogMessage($"No config for handsy BA for faction {unit.team.FactionValue.Name}.");
                    }
                }
            }

            //give AI mechs ability to swat or roll
            if (unit is Mech && !(unit is TrooperSquad) && !unit.IsCustomUnitVehicle())
            {
                if (!string.IsNullOrEmpty(ModInit.modSettings.BattleArmorDeSwarmSwat))
                {
                    if (unit.GetPilot().Abilities
                            .All(x => x.Def.Id != ModInit.modSettings.BattleArmorDeSwarmSwat) &&
                        unit.ComponentAbilities.All(y =>
                            y.Def.Id != ModInit.modSettings.BattleArmorDeSwarmSwat))
                    {
                        dm.AbilityDefs.TryGet(ModInit.modSettings.BattleArmorDeSwarmSwat, out var def);
                        var ability = new Ability(def);
                        ModInit.modLog.LogTrace(
                            $"Adding {ability.Def?.Description?.Id} to {unit.Description?.Name}.");
                        ability.Init(unit.Combat);
                        unit.GetPilot().Abilities.Add(ability);
                        unit.GetPilot().ActiveAbilities.Add(ability);
                    }
                }

                if (!string.IsNullOrEmpty(ModInit.modSettings.BattleArmorDeSwarmRoll))
                {
                    if (unit.GetPilot().Abilities
                            .All(x => x.Def.Id != ModInit.modSettings.BattleArmorDeSwarmRoll) &&
                        unit.ComponentAbilities.All(y =>
                            y.Def.Id != ModInit.modSettings.BattleArmorDeSwarmRoll))
                    {
                        dm.AbilityDefs.TryGet(ModInit.modSettings.BattleArmorDeSwarmRoll, out var def);
                        var ability = new Ability(def);
                        ModInit.modLog.LogTrace(
                            $"Adding {ability.Def?.Description?.Id} to {unit.Description?.Name}.");
                        ability.Init(unit.Combat);
                        unit.GetPilot().Abilities.Add(ability);
                        unit.GetPilot().ActiveAbilities.Add(ability);
                    }
                }
                
            }

            //do we want to generate AI abilities if they already have BA? unsure.
            if (unit.Combat.TurnDirector.CurrentRound > 1) return; // don't give abilities to reinforcements?
            if (unit.team.GUID != "be77cadd-e245-4240-a93e-b99cc98902a5") return; // TargetTeam is only team that gets cmdAbilities 
            if (!ModInit.modSettings.commandAbilities_AI.ContainsKey(unit.team.FactionValue.Name))
            {
                ModInit.modLog.LogMessage($"No settings for command abilities for {unit.team.FactionValue.Name}, skipping.");
                return;
            }

            ModState.CurrentFactionSettingsList = new List<Classes.AI_FactionCommandAbilitySetting>(
                    ModInit.modSettings.commandAbilities_AI[unit.team.FactionValue.Name].OrderBy(x => x.AddChance));
            ModInit.modLog.LogTrace($"Ordering setting dictionary.");

            if (unit.GetPilot().Abilities.All(x => x.Def.Resource != AbilityDef.ResourceConsumed.CommandAbility))
            {
                ModInit.modLog.LogTrace($"No command abilities on pilot.");
                if (unit.ComponentAbilities.All(x => x.Def.Resource != AbilityDef.ResourceConsumed.CommandAbility))
                {
                    ModInit.modLog.LogTrace($"No command abilities on unit from Components.");
                    foreach (var abilitySetting in ModState.CurrentFactionSettingsList)
                    {
                        var roll = ModInit.Random.NextDouble();
                        var chance = abilitySetting.AddChance +
                                     (abilitySetting.DiffMod * unit.Combat.ActiveContract.Override.finalDifficulty);
                        if (roll <= chance)
                        {
                            ModInit.modLog.LogTrace($"Rolled {roll}, < {chance}.");
                            if (!dm.AbilityDefs.TryGet(abilitySetting.AbilityDefID, out var def)) return;
                            var ability = new Ability(def);
                            ModInit.modLog.LogMessage(
                                $"Adding {ability.Def?.Description?.Id} to {unit.Description?.Name}.");
                            ability.Init(unit.Combat);
                            unit.ComponentAbilities.Add(ability);
                            return;
                        }
                    }
                }
            }
        }

        public static string AssignRandomSpawnAsset(Ability ability, string factionName, out int waves)
        {
            var sgs = UnityGameInstance.BattleTechGame.Simulation;
            var dm = UnityGameInstance.BattleTechGame.DataManager;
            var potentialAssetsForAI = new List<string> {ability.Def.ActorResource};
            var potentialAssetsForAIWaves = new List<int> {ModInit.modSettings.strafeWaves};
            var isAOE = new List<bool>{false};

            if (!string.IsNullOrEmpty(ability.Def.ActorResource))
            {
                string type;
                if (ability.Def.ActorResource.StartsWith("mechdef_"))
                {
                    type = "mechdef_";
                }

                else if (ability.Def.ActorResource.StartsWith("vehicledef_"))
                {
                    type = "vehicledef_";
                }
                else if (ability.Def.ActorResource.StartsWith("turretdef_"))
                {
                    type = "turretdef_";
                }
                else
                {
                    ModInit.modLog.LogTrace($"Something fucked in the ability {ability.Def.Description.Id}");
                    waves = 0;
                    return "";
                }
                var allowedUnitTags = ability.Def.StringParam2;

                var beaconsToCheck = new List<string>();

                if (ModInit.modSettings.AI_FactionBeacons.ContainsKey(factionName))
                {
                    beaconsToCheck = ModInit.modSettings.AI_FactionBeacons[factionName];
                }
                else
                {
                    ModInit.modLog.LogTrace($"No setting in AI_FactionBeacons for {factionName}, using only default {ability.Def.ActorResource}");
                    goto choose;
                }

                foreach (var stat in beaconsToCheck)
                {
                    string[] array = stat.Split(new char[]
                    {
                        '.'
                    });
                    if (string.CompareOrdinal(array[1], "MECHPART") != 0)
                    {
                        BattleTechResourceType battleTechResourceType =
                            (BattleTechResourceType) Enum.Parse(typeof(BattleTechResourceType), array[1]);
                        if (battleTechResourceType != BattleTechResourceType.MechDef &&
                            dm.Exists(battleTechResourceType, array[2]))
                        {
                            bool flag = array.Length > 3 &&
                                        string.Compare(array[3], "DAMAGED", StringComparison.Ordinal) == 0;
                            MechComponentDef componentDef = sgs.GetComponentDef(battleTechResourceType, array[2]);
                            MechComponentRef mechComponentRef = new MechComponentRef(componentDef.Description.Id,
                                sgs.GenerateSimGameUID(), componentDef.ComponentType, ChassisLocations.None, -1,
                                flag ? ComponentDamageLevel.NonFunctional : ComponentDamageLevel.Functional, false);
                            mechComponentRef.SetComponentDef(componentDef);

                            if (mechComponentRef.Def.ComponentTags.All(x => x != "CanSpawnTurret" && ability.Def.specialRules == AbilityDef.SpecialRules.SpawnTurret))
                                continue;
                            if (mechComponentRef.Def.ComponentTags.All(x => x != "CanStrafe" && ability.Def.specialRules == AbilityDef.SpecialRules.Strafe))
                                continue;
                            var id = mechComponentRef.Def.ComponentTags.FirstOrDefault(x =>
                                x.StartsWith("mechdef_") || x.StartsWith("vehicledef_") ||
                                x.StartsWith("turretdef_"));


                            if (!id.StartsWith(type))
                            {
                                ModInit.modLog.LogTrace($"{id} != {type}, ignoring.");
                                continue;
                            }

                            if (!string.IsNullOrEmpty(allowedUnitTags) &&
                                mechComponentRef.Def.ComponentTags.All(x => x != allowedUnitTags))
                            {
                                continue;
                            }
                            var waveString = mechComponentRef.Def.ComponentTags.FirstOrDefault(x => x.StartsWith("StrafeWaves_"));
                            int.TryParse(waveString?.Substring(11), out waves);
                            potentialAssetsForAI.Add(id);
                            potentialAssetsForAIWaves.Add(waves);
                            isAOE.Add(mechComponentRef.IsAOEStrafe(ability.Def.specialRules == AbilityDef.SpecialRules.Strafe));
                            ModInit.modLog.LogTrace($"Added {id} to potential AI assets.");
                        }
                    }
                }

                choose:
                var idx = potentialAssetsForAI.GetRandomIndex();
                var chosen = potentialAssetsForAI[idx];
                waves = potentialAssetsForAIWaves[idx];
                ModState.IsStrafeAOE = isAOE[idx];
                ModInit.modLog.LogTrace($"Chose {chosen} for this activation.");

                LoadRequest loadRequest = dm.CreateLoadRequest();
                if (chosen.StartsWith("mechdef_"))
                {
                    ModInit.modLog.LogMessage($"Added loadrequest for MechDef: {chosen}");
                    loadRequest.AddBlindLoadRequest(BattleTechResourceType.MechDef, chosen);
                }
                else if (chosen.StartsWith("vehicledef_"))
                {
                    ModInit.modLog.LogMessage($"Added loadrequest for VehicleDef: {chosen}");
                    loadRequest.AddBlindLoadRequest(BattleTechResourceType.VehicleDef, chosen);
                }
                else if (chosen.StartsWith("turretdef_"))
                {
                    ModInit.modLog.LogMessage($"Added loadrequest for TurretDef: {chosen}");
                    loadRequest.AddBlindLoadRequest(BattleTechResourceType.TurretDef, chosen);
                }
                loadRequest.ProcessRequests(1000u);

                return chosen;
            }

            waves = 0;
            return "";
        }

        public static bool CanStrafe(AbstractActor actor, out Ability ability)
        {
            var _ability =
                actor.ComponentAbilities.FirstOrDefault(x => x.Def.specialRules == AbilityDef.SpecialRules.Strafe);
            if (_ability != null)
            {
                if (_ability.IsAvailable)
                {
                    ability = _ability;
                    return true;
                }
            }
            ability = default(Ability);
            return false;
        }

        public static float CalcExpectedDamage(AbstractActor actor, string attackerResource)
        {
            if (attackerResource.StartsWith("mechdef_"))
            {
                actor.Combat.DataManager.MechDefs.TryGet(attackerResource, out MechDef attacker);
                attacker.Refresh();
                var potentialRegDamage = 0f;
                var potentialHeatDamage = 0f;
                var potentialStabDamage = 0f;
                foreach (var weapon in attacker.Inventory.Where(x=>x.ComponentDefType == ComponentType.Weapon))
                {
                    if (!(weapon.Def is WeaponDef weaponDef)) continue;
                    potentialRegDamage += weaponDef.Damage;
                    potentialHeatDamage += weaponDef.HeatDamage;
                    potentialStabDamage += weaponDef.Instability;
                }

                var finalDamage = potentialRegDamage + potentialHeatDamage + potentialStabDamage;
                return finalDamage;
            }
            else if (attackerResource.StartsWith("vehicledef_"))
            {
                actor.Combat.DataManager.VehicleDefs.TryGet(attackerResource, out VehicleDef attacker);
                attacker.Refresh();
                var potentialRegDamage = 0f;
                var potentialHeatDamage = 0f;
                var potentialStabDamage = 0f;
                foreach (var weapon in attacker.Inventory.Where(x=>x.ComponentDefType == ComponentType.Weapon))
                {
                    if (!(weapon.Def is WeaponDef weaponDef)) continue;
                    potentialRegDamage += weaponDef.Damage;
                    potentialHeatDamage += weaponDef.HeatDamage;
                    potentialStabDamage += weaponDef.Instability;
                }

                var finalDamage = potentialRegDamage + potentialHeatDamage + potentialStabDamage;
                return finalDamage;
            }
            else if (attackerResource.StartsWith("turretdef_"))
            {
                actor.Combat.DataManager.TurretDefs.TryGet(attackerResource, out TurretDef attacker);
                attacker.Refresh();
                var potentialRegDamage = 0f;
                var potentialHeatDamage = 0f;
                var potentialStabDamage = 0f;
                foreach (var weapon in attacker.Inventory.Where(x=>x.ComponentDefType == ComponentType.Weapon))
                {
                    if (!(weapon.Def is WeaponDef weaponDef)) continue;
                    potentialRegDamage += weaponDef.Damage;
                    potentialHeatDamage += weaponDef.HeatDamage;
                    potentialStabDamage += weaponDef.Instability;
                }

                var finalDamage = potentialRegDamage + potentialHeatDamage + potentialStabDamage;
                return finalDamage;
            }
            return 0f;
        }

        public static int TargetsForStrafe(AbstractActor actor, Ability ability, out Vector3 startPos, out Vector3 endPos) //switch to Icombatant
        {
            var maxCount = 0;
            var savedEndVector = new Vector3();
            var savedStartVector = new Vector3();

            if (ability != null)
            {
                var maxRange = ability.Def.IntParam2 - ability.Def.FloatParam2;

                var circ = maxRange * 2 * Math.PI;
                var steps = Mathf.RoundToInt((float)circ / (ability.Def.FloatParam1 * 2));

                var enemyCombatants = new List<ICombatant>(actor.Combat.GetAllImporantCombatants().Where(x=>x.team.IsEnemy(actor.team)));

                enemyCombatants.RemoveAll(x=> x.GUID == actor.GUID || x.IsDead);

                for (int i = 0; i < enemyCombatants.Count; i++)
                {
                    if (enemyCombatants[i] is AbstractActor combatantAsActor)
                    {
                        if (combatantAsActor.WasDespawned || combatantAsActor.WasEjected)
                        {
                            enemyCombatants.RemoveAt(i);
                        }
                    }
                }

                for (int k = 0; k < enemyCombatants.Count; k++)
                {
//                    AbstractActor enemyActor = actor.BehaviorTree.enemyUnits[k] as AbstractActor;
                    if (enemyCombatants[k] == null) continue;
                    Vector3 possibleStart;
                    if (Mathf.RoundToInt(Vector3.Distance(actor.CurrentPosition, enemyCombatants[k].CurrentPosition)) < maxRange)
                    {
                        possibleStart = enemyCombatants[k].CurrentPosition;
                    }
                    else
                    {
                        continue;
                    }

                    var vectors = Utils.MakeCircle(possibleStart, steps, ability.Def.FloatParam2);
                    var currentSavedEndVector = new Vector3();
                    var currentSavedStartVector = new Vector3();
                    var currentMaxCount = 0;
                    
                    foreach (var vector in vectors)
                    {
                        var targetCount = 0;
                        var rectangles = Utils.MakeRectangle(possibleStart, vector, ability.Def.FloatParam1);
                        foreach (var rectangle in rectangles)
                        {
                            for (int l = 0; l < enemyCombatants.Count; l++)
                            {
                                if (!(enemyCombatants[l] is AbstractActor newTarget)) continue;
                                if (rectangle.Contains(newTarget.CurrentPosition))
                                {
                                    targetCount += 1;
                                }
                            }
                        }

                        if (targetCount >= currentMaxCount)
                        {
                            currentMaxCount = targetCount;
                            currentSavedEndVector = vector;
                            currentSavedStartVector = possibleStart;

                        }
                    }

                    if (currentMaxCount >= maxCount)
                    {
                        maxCount = currentMaxCount;
                        savedEndVector = currentSavedEndVector;
                        savedStartVector = currentSavedStartVector;
                    }
                }
                // should probably try to evaluate how many allied units it could hit and offset?

                startPos = savedStartVector;
                endPos = savedEndVector;
               // ModState.selectedAIVectors.Add(savedStartVector);
               // ModState.selectedAIVectors.Add(savedEndVector);
                return maxCount;
            }

            startPos = default(Vector3);
            endPos = default(Vector3);
            return 0;
        }


        public static bool CanSpawn(AbstractActor actor, out Ability ability)
        {
            var _ability =
                actor.ComponentAbilities.FirstOrDefault(x => x.Def.specialRules == AbilityDef.SpecialRules.SpawnTurret);
            if (_ability != null)
            {
                if (_ability.IsAvailable)
                {
                    ability = _ability;
                    return true;
                }
            }
            ability = default(Ability);
            return false;
        }


        //this is still spawning way out of ranhe for some reason
        public static int EvaluateSpawn(AbstractActor actor, out Ability ability, out Vector3 spawnpoint, out Vector3 rotationVector)
        {
            spawnpoint = new Vector3();
            rotationVector = new Vector3();
            if (!CanSpawn(actor, out ability)) return 0;

            if (ability != null)
            {
                var assetID = AssignRandomSpawnAsset(ability, actor.team.FactionValue.Name, out var waves);

                var asset = actor.Combat.DataManager.FetchUnitFromDataManager(assetID);

                Classes.AI_SpawnBehavior spawnBehavior = new Classes.AI_SpawnBehavior();
                if (asset is MechDef mech)
                {
                    foreach (var behavior in ModInit.modSettings.AI_SpawnBehavior)
                    {
                        if (mech.MechTags.Contains(behavior.Tag))
                        {
                            spawnBehavior = behavior;
                            goto behaviorEvalFinished;
                        }
                    }
                }
                else if (asset is VehicleDef vehicle)
                {
                    foreach (var behavior in ModInit.modSettings.AI_SpawnBehavior)
                    {
                        if (vehicle.VehicleTags.Contains(behavior.Tag))
                        {
                            spawnBehavior = behavior;
                            goto behaviorEvalFinished;
                        }
                    }
                }
                else if (asset is TurretDef turret)
                {
                    foreach (var behavior in ModInit.modSettings.AI_SpawnBehavior)
                    {
                        if (turret.TurretTags.Contains(behavior.Tag))
                        {
                            spawnBehavior = behavior;
                            goto behaviorEvalFinished;
                        }
                    }
                }

                behaviorEvalFinished:
                var maxRange = ability.Def.IntParam2;
                //var enemyActors = new List<AbstractActor>(actor.Combat.AllEnemies);
                var enemyActors = actor.team.VisibilityCache.GetAllDetectedEnemies(actor);
                ModInit.modLog.LogTrace(
                    $"found {enemyActors.Count} to eval");
                enemyActors.RemoveAll(x => x.WasDespawned || x.IsDead || x.IsFlaggedForDeath || x.WasEjected);
                ModInit.modLog.LogTrace(
                    $"found {enemyActors.Count} after eval");
                var avgCenter = new Vector3();
                var theCenter = new Vector3();
                var orientation = new Vector3();
                var finalOrientation = new Vector3();

                if (spawnBehavior.Behavior == "AMBUSH")
                {
                    var count = 0;
                    var targetEnemy = actor.GetClosestDetectedEnemy(actor.CurrentPosition);

                    if (targetEnemy != null)
                    {
                        ModInit.modLog.LogTrace(
                            $"Target enemy {targetEnemy.DisplayName}");
                        theCenter = targetEnemy.CurrentPosition;
                        count = 1;
                    }

                    if (Vector3.Distance(actor.CurrentPosition, theCenter) > maxRange)
                    {
                        theCenter = Utils.LerpByDistance(actor.CurrentPosition, theCenter, maxRange);
                        ModInit.modLog.LogTrace(
                            $"Chosen point is {Vector3.Distance(actor.CurrentPosition, theCenter)} from source after LerpByDist");
                    }
                    else
                    {
                        ModInit.modLog.LogTrace(
                            $"Chosen point is {Vector3.Distance(actor.CurrentPosition, theCenter)} from source, should be < {maxRange}");
                    }

                    theCenter = SpawnUtils.FindValidSpawn(targetEnemy, actor, spawnBehavior.MinRange, maxRange);

                    finalOrientation = targetEnemy.CurrentPosition - theCenter;

                    theCenter.y = actor.Combat.MapMetaData.GetLerpedHeightAt(theCenter);
                    spawnpoint = theCenter;
                    rotationVector = finalOrientation;
                    return count;
                }

                if (spawnBehavior.Behavior == "REINFORCE")
                {
                    var friendlyActors = actor.team.VisibilityCache.GetAllFriendlies(actor);
                    var center = new Vector3(0, 0, 0);
                    var count = 0;
                    foreach (var friendly in friendlyActors)
                    {
                        center += friendly.CurrentPosition;
                        count++;
                        ModInit.modLog.LogTrace(
                            $"friendlyActors count = {count}");
                    }

                    if (count == 0)
                    {
                        ModInit.modLog.LogTrace(
                            $"FINAL friendlyActors count = {count}");
                        theCenter = actor.CurrentPosition;
                        finalOrientation = orientation;
                        goto skip;
                    }

                    avgCenter = center / count;

                    if (Vector3.Distance(actor.CurrentPosition, avgCenter) > maxRange)
                    {
                        theCenter = Utils.LerpByDistance(actor.CurrentPosition, avgCenter, maxRange);
                        ModInit.modLog.LogTrace(
                            $"Chosen point is {Vector3.Distance(actor.CurrentPosition, theCenter)} from source after LerpByDist");
                    }
                    else
                    {
                        theCenter = avgCenter;
                        ModInit.modLog.LogTrace(
                            $"Chosen point is {Vector3.Distance(actor.CurrentPosition, theCenter)} from source, should be < {maxRange}");
                    }

                    var targetFriendly = Utils.GetClosestDetectedFriendly(theCenter, actor);
                    var closestEnemy = actor.GetClosestDetectedEnemy(theCenter);

                    theCenter = SpawnUtils.FindValidSpawn(targetFriendly, actor, spawnBehavior.MinRange, maxRange);


                    finalOrientation = closestEnemy.CurrentPosition - theCenter;

                    skip:
                    theCenter.y = actor.Combat.MapMetaData.GetLerpedHeightAt(theCenter);
                    spawnpoint = theCenter;
                    rotationVector = finalOrientation;
                    return count;
                }

                if (spawnBehavior.Behavior == "DEFAULT" || spawnBehavior.Behavior == "BRAWLER")
                {
                    var center = new Vector3();
                    var count = 0;
                    foreach (var enemy in enemyActors)
                    {
                        center += enemy.CurrentPosition;
                        count++;
                        ModInit.modLog.LogTrace(
                            $"enemyActors count = {count}");
                    }

                    if (count == 0)
                    {
                        ModInit.modLog.LogTrace(
                            $"FINAL enemyActors count = {count}");
                        theCenter = actor.CurrentPosition;
                        finalOrientation = orientation;
                        goto skip;
                    }

                    avgCenter = center / count;

                    if (Vector3.Distance(actor.CurrentPosition, avgCenter) > maxRange)
                    {
                        theCenter = Utils.LerpByDistance(actor.CurrentPosition, avgCenter, maxRange);
                        ModInit.modLog.LogTrace(
                            $"Chosen point is {Vector3.Distance(actor.CurrentPosition, theCenter)} from source after LerpByDist");
                    }
                    else
                    {
                        theCenter = avgCenter;
                        ModInit.modLog.LogTrace(
                            $"Chosen point is {Vector3.Distance(actor.CurrentPosition, theCenter)} from source, should be < {maxRange}");
                    }

                    var closestEnemy = actor.GetClosestDetectedEnemy(theCenter);

                    theCenter = SpawnUtils.FindValidSpawn(closestEnemy, actor, spawnBehavior.MinRange, maxRange);

                    finalOrientation = closestEnemy.CurrentPosition = theCenter;

                    skip:
                    theCenter.y = actor.Combat.MapMetaData.GetLerpedHeightAt(theCenter);
                    spawnpoint = theCenter;
                    rotationVector = finalOrientation;
                    return count;
                }
            }
            return 0;
        }
    }
}