using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using BattleTech.Data;
using CustomComponents;
using CustomUnits;
using UnityEngine;

namespace StrategicOperations.Framework
{
    public static class AI_Utils
    {
        public static string AssignRandomSpawnAsset(Ability ability, string factionName, out int waves)
        {
            var dm = UnityGameInstance.BattleTechGame.DataManager;

            if (!string.IsNullOrEmpty(ability.Def.ActorResource))
            {
                if (ModState.CachedFactionCommandBeacons.ContainsKey(ability.Def.Id))
                {
                    if (ModState.CachedFactionCommandBeacons[ability.Def.Id].ContainsKey(factionName))
                    {
                        var beaconsToCheck =
                            ModState.CachedFactionCommandBeacons[ability.Def.Id]
                                [factionName];
                        var chosen = beaconsToCheck.GetRandomElement();
                        waves = chosen.StrafeWaves;
                        Mod.Log.Trace?.Log($"Chose {chosen} for this activation.");

                        LoadRequest loadRequest = dm.CreateLoadRequest();
                        if (chosen.UnitDefID.StartsWith("mechdef_"))
                        {
                            Mod.Log.Trace?.Log($"Added loadrequest for MechDef: {chosen.UnitDefID}");
                            loadRequest.AddBlindLoadRequest(BattleTechResourceType.MechDef, chosen.UnitDefID);
                        }
                        else if (chosen.UnitDefID.StartsWith("vehicledef_"))
                        {
                            Mod.Log.Trace?.Log($"Added loadrequest for VehicleDef: {chosen.UnitDefID}");
                            loadRequest.AddBlindLoadRequest(BattleTechResourceType.VehicleDef, chosen.UnitDefID);
                        }
                        else if (chosen.UnitDefID.StartsWith("turretdef_"))
                        {
                            Mod.Log.Trace?.Log($"Added loadrequest for TurretDef: {chosen.UnitDefID}");
                            loadRequest.AddBlindLoadRequest(BattleTechResourceType.TurretDef, chosen.UnitDefID);
                        }
                        loadRequest.ProcessRequests(1000U);

                        return chosen.UnitDefID;
                    }

                    Mod.Log.Trace?.Log($"No setting in AI_FactionBeacons for {ability.Def.Id} and {factionName}, using only default {ability.Def.ActorResource}");
                    waves = Mod.Settings.strafeWaves;
                    return ability.Def.ActorResource;
                }

                Mod.Log.Trace?.Log($"No setting in AI_FactionBeacons for {ability.Def.Id} and {factionName}, using only default {ability.Def.ActorResource}");
                waves = Mod.Settings.strafeWaves;
                return ability.Def.ActorResource;
            }
            waves = 0;
            return "";
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
                    foreach (var behavior in Mod.Settings.AI_SpawnBehavior)
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
                    foreach (var behavior in Mod.Settings.AI_SpawnBehavior)
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
                    foreach (var behavior in Mod.Settings.AI_SpawnBehavior)
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
                Mod.Log.Trace?.Log(
                    $"found {enemyActors.Count} to eval");
                enemyActors.RemoveAll(x => x.WasDespawned || x.IsDead || x.IsFlaggedForDeath || x.WasEjected);
                Mod.Log.Trace?.Log(
                    $"found {enemyActors.Count} after eval");
                var avgCenter = new Vector3();
                var theCenter = new Vector3();
                var orientation = new Vector3();
                var finalOrientation = new Vector3();

                if (spawnBehavior.Behavior == "AMBUSH")
                {
                    var count = 0;
                    var targetEnemy = actor.GetClosestDetectedEnemy(actor.CurrentPosition);
                    if (targetEnemy == null) return 0;
                    Mod.Log.Trace?.Log(
                        $"Target enemy {targetEnemy.DisplayName}");
                    theCenter = targetEnemy.CurrentPosition;
                    count = 1;

                    if (Vector3.Distance(actor.CurrentPosition, theCenter) > maxRange)
                    {
                        theCenter = Utils.LerpByDistance(actor.CurrentPosition, theCenter, maxRange);
                        Mod.Log.Trace?.Log(
                            $"Chosen point is {Vector3.Distance(actor.CurrentPosition, theCenter)} from source after LerpByDist");
                    }
                    else
                    {
                        Mod.Log.Trace?.Log(
                            $"Chosen point is {Vector3.Distance(actor.CurrentPosition, theCenter)} from source, should be < {maxRange}");
                    }

                    theCenter = theCenter.FetchRandomAdjacentHexFromVector(actor, targetEnemy, spawnBehavior.MinRange, maxRange);//SpawnUtils.FindValidSpawn(targetEnemy, actor, spawnBehavior.MinRange, maxRange);

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
                        Mod.Log.Trace?.Log(
                            $"friendlyActors count = {count}");
                    }

                    if (count == 0)
                    {
                        Mod.Log.Trace?.Log(
                            $"FINAL friendlyActors count = {count}");
                        theCenter = actor.CurrentPosition;
                        finalOrientation = orientation;
                        goto skip;
                    }

                    avgCenter = center / count;

                    if (Vector3.Distance(actor.CurrentPosition, avgCenter) > maxRange)
                    {
                        theCenter = Utils.LerpByDistance(actor.CurrentPosition, avgCenter, maxRange);
                        Mod.Log.Trace?.Log(
                            $"Chosen point is {Vector3.Distance(actor.CurrentPosition, theCenter)} from source after LerpByDist");
                    }
                    else
                    {
                        theCenter = avgCenter;
                        Mod.Log.Trace?.Log(
                            $"Chosen point is {Vector3.Distance(actor.CurrentPosition, theCenter)} from source, should be < {maxRange}");
                    }

                    var targetFriendly = Utils.GetClosestDetectedFriendly(theCenter, actor);
                    var closestEnemy = actor.GetClosestDetectedEnemy(theCenter);

                    //theCenter = SpawnUtils.FindValidSpawn(targetFriendly, actor, spawnBehavior.MinRange, maxRange);
                    theCenter = theCenter.FetchRandomAdjacentHexFromVector(actor, targetFriendly, spawnBehavior.MinRange, maxRange);//SpawnUtils.FindValidSpawn(targetEnemy, actor, spawnBehavior.MinRange, maxRange);

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
                        Mod.Log.Trace?.Log(
                            $"enemyActors count = {count}");
                    }

                    if (count == 0)
                    {
                        Mod.Log.Trace?.Log(
                            $"FINAL enemyActors count = {count}");
                        theCenter = actor.CurrentPosition;
                        finalOrientation = orientation;
                        goto skip;
                    }

                    avgCenter = center / count;

                    if (Vector3.Distance(actor.CurrentPosition, avgCenter) > maxRange)
                    {
                        theCenter = Utils.LerpByDistance(actor.CurrentPosition, avgCenter, maxRange);
                        Mod.Log.Trace?.Log(
                            $"Chosen point is {Vector3.Distance(actor.CurrentPosition, theCenter)} from source after LerpByDist");
                    }
                    else
                    {
                        theCenter = avgCenter;
                        Mod.Log.Trace?.Log(
                            $"Chosen point is {Vector3.Distance(actor.CurrentPosition, theCenter)} from source, should be < {maxRange}");
                    }

                    var closestEnemy = actor.GetClosestDetectedEnemy(theCenter);

                    //theCenter = SpawnUtils.FindValidSpawn(closestEnemy, actor, spawnBehavior.MinRange, maxRange);
                    theCenter = theCenter.FetchRandomAdjacentHexFromVector(actor, closestEnemy, spawnBehavior.MinRange, maxRange);//SpawnUtils.FindValidSpawn(targetEnemy, actor, spawnBehavior.MinRange, maxRange);
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

        public static int EvaluateStrafing(AbstractActor actor, out Ability ability, out Vector3 startpoint, out Vector3 endpoint, out AbstractActor targetUnit)
        {
            startpoint = default(Vector3);
            endpoint = default(Vector3);
            targetUnit = null;
            if (!CanStrafe(actor, out ability)) return 0;
            var assetID = AssignRandomSpawnAsset(ability, actor.team.FactionValue.Name, out var waves);

            if (Mod.Settings.strafeUseAlternativeImplementation)
            {
                ModState.StrafeAttacker = assetID;
                ModState.StrafeSelectedWaves = waves;
            }

            var dmg = Mathf.RoundToInt(CalcExpectedDamage(actor, assetID) * waves);

            var targets = TargetsForStrafe(actor, ability,out startpoint, out endpoint, out targetUnit);

            return dmg * targets;
        }

        public static void GenerateAIStrategicAbilities(AbstractActor unit)
        {
            if (unit is Turret) return;
            if (unit.team.IsLocalPlayer) return;
            var dm = unit.Combat.DataManager;

            //check for BA equipment. if present, we're going to spawn BA and mount it to AI
            Mod.Log.Info?.Log($"Checking if unit {unit.DisplayName} {unit.GUID} should spawn Battle Armor.");

            if (!Mod.Settings.AI_BattleArmorExcludedContractIDs.Contains(unit.Combat.ActiveContract.Override
                    .ID) && !Mod.Settings.AI_BattleArmorExcludedContractTypes.Contains(unit.Combat.ActiveContract
                    .ContractTypeValue.Name))
            {
                if (!unit.GetIsUnMountable())
                {

                    if (Mod.Settings.BattleArmorFactionAssociations.Any(x =>
                            x.FactionIDs.Contains(unit.team.FactionValue.Name)))
                    {
                        if (!ModState.CurrentBattleArmorSquads.ContainsKey(unit.team.FactionValue.Name))
                        {
                            ModState.CurrentBattleArmorSquads.Add(unit.team.FactionValue.Name, 0);
                        }

                        var baConfig =
                            Mod.Settings.BattleArmorFactionAssociations.FirstOrDefault(x =>
                                x.FactionIDs.Contains(unit.team.FactionValue.Name));
                        if (baConfig == null)
                        {
                            Mod.Log.Error?.Log(
                                $"[GenerateAIStrategicAbilities] - something broken trying to process BA Faction Association. baConfig was null.");
                            return;
                        }

                        Mod.Log.Trace?.Log($"Found config for {unit.team.FactionValue.Name}.");

                        var baLance = Utils.CreateOrFetchCMDLance(unit.team);
                        var spawnChance = baConfig.SpawnChanceBase +
                                          (unit.Combat.ActiveContract.Override.finalDifficulty *
                                           baConfig.SpawnChanceDiffMod);
                        var internalSpace = unit.GetAvailableInternalBASpace();
                        if (internalSpace > 0)
                        {
                            Mod.Log.Trace?.Log($"Unit has {internalSpace} internal space.");
                            for (int i = 0; i < internalSpace; i++)
                            {
                                var chosenInt = baConfig.ProcessBattleArmorSpawnWeights(dm, unit.team.FactionValue.Name,
                                    "InternalBattleArmorWeight");
                                if (!string.IsNullOrEmpty(chosenInt))
                                {
                                    var baRollInt = Mod.Random.NextDouble();
                                    if (baRollInt <= spawnChance)
                                    {
                                        Mod.Log.Info?.Log(
                                            $"Roll {baRollInt} <= {spawnChance}, choosing BA from InternalBattleArmorWeight for slot {i} of {internalSpace}.");
                                        if (chosenInt != "BA_EMPTY")
                                        {
                                            if (ModState.CurrentBattleArmorSquads[unit.team.FactionValue.Name] <
                                                baConfig.MaxSquadsPerContract)
                                            {
                                                var spawner = new Classes.CustomSpawner(unit.Combat, unit, chosenInt, baLance);
                                                spawner.SpawnBattleArmorAtActor();
                                                ModState.CurrentBattleArmorSquads[unit.team.FactionValue.Name] += 1;
                                                Mod.Log.Info?.Log(
                                                    $"Spawning {chosenInt}, incrementing CurrentBattleArmorSquads to {ModState.CurrentBattleArmorSquads[unit.team.FactionValue.Name]}.");
                                            }
                                            else
                                            {
                                                Mod.Log.Info?.Log(
                                                    $"{ModState.CurrentBattleArmorSquads[unit.team.FactionValue.Name]} is max {baConfig.MaxSquadsPerContract} per contract.");
                                            }
                                        }
                                        else
                                        {
                                            Mod.Log.Info?.Log($"Chose {chosenInt}.");
                                        }
                                    }
                                    else
                                    {
                                        Mod.Log.Info?.Log(
                                            $"Roll {baRollInt} > {spawnChance}, not adding BA internally.");
                                    }
                                }
                                else
                                {
                                    Mod.Log.Info?.Log(
                                        $"No config for internal BA for faction {unit.team.FactionValue.Name}.");
                                }
                            }
                        }
                        else
                        {
                            Mod.Log.Trace?.Log($"Unit dont has internal space.");
                        }

                        if (unit.GetHasBattleArmorMounts())
                        {
                            Mod.Log.Trace?.Log($"Unit has mounts.");

                            var chosenMount = baConfig.ProcessBattleArmorSpawnWeights(dm, unit.team.FactionValue.Name,
                                "MountedBattleArmorWeight");
                            if (!string.IsNullOrEmpty(chosenMount))
                            {
                                var baRollMount = Mod.Random.NextDouble();
                                if (baRollMount <= spawnChance)
                                {
                                    Mod.Log.Info?.Log(
                                        $"Roll {baRollMount} <= {spawnChance}, choosing BA from MountedBattleArmorWeight.");
                                    if (chosenMount != "BA_EMPTY")
                                    {
                                        if (ModState.CurrentBattleArmorSquads[unit.team.FactionValue.Name] <
                                            baConfig.MaxSquadsPerContract)
                                        {
                                            var spawner = new Classes.CustomSpawner(unit.Combat, unit, chosenMount, baLance);
                                            spawner.SpawnBattleArmorAtActor();
                                            ModState.CurrentBattleArmorSquads[unit.team.FactionValue.Name] += 1;
                                            Mod.Log.Info?.Log(
                                                $"Spawning {chosenMount}, incrementing CurrentBattleArmorSquads to {ModState.CurrentBattleArmorSquads[unit.team.FactionValue.Name]}.");
                                        }
                                        else
                                        {
                                            Mod.Log.Info?.Log(
                                                $"{ModState.CurrentBattleArmorSquads[unit.team.FactionValue.Name]} is max {baConfig.MaxSquadsPerContract} per contract.");
                                        }
                                    }
                                    else
                                    {
                                        Mod.Log.Info?.Log($"Chose {chosenMount}.");
                                    }
                                }
                                else
                                {
                                    Mod.Log.Info?.Log(
                                        $"Roll {baRollMount} > {spawnChance}, not adding BA to mounts.");
                                }
                            }
                            else
                            {
                                Mod.Log.Info?.Log(
                                    $"No config for mounted BA for faction {unit.team.FactionValue.Name}.");
                            }
                        }
                        else if (!(unit is TrooperSquad))
                        {
                            Mod.Log.Trace?.Log($"Unit dont has mounts.");
                            var chosenHandsy = baConfig.ProcessBattleArmorSpawnWeights(dm, unit.team.FactionValue.Name,
                                "HandsyBattleArmorWeight");
                            if (!string.IsNullOrEmpty(chosenHandsy))
                            {
                                var baRollHandsy = Mod.Random.NextDouble();
                                if (baRollHandsy <= spawnChance)
                                {
                                    Mod.Log.Info?.Log(
                                        $"Roll {baRollHandsy} <= {spawnChance}, choosing BA from HandsyBattleArmorWeight.");
                                    if (chosenHandsy != "BA_EMPTY")
                                    {
                                        if (ModState.CurrentBattleArmorSquads[unit.team.FactionValue.Name] <
                                            baConfig.MaxSquadsPerContract)
                                        {
                                            var spawner = new Classes.CustomSpawner(unit.Combat, unit, chosenHandsy, baLance);
                                            spawner.SpawnBattleArmorAtActor();
                                            ModState.CurrentBattleArmorSquads[unit.team.FactionValue.Name] += 1;
                                            Mod.Log.Info?.Log(
                                                $"Spawning {chosenHandsy}, incrementing CurrentBattleArmorSquads to {ModState.CurrentBattleArmorSquads[unit.team.FactionValue.Name]}.");
                                        }
                                        else
                                        {
                                            Mod.Log.Info?.Log(
                                                $"{ModState.CurrentBattleArmorSquads[unit.team.FactionValue.Name]} is max {baConfig.MaxSquadsPerContract} per contract.");
                                        }
                                    }
                                    else
                                    {
                                        Mod.Log.Info?.Log($"Chose {chosenHandsy}.");
                                    }
                                }
                                else
                                {
                                    Mod.Log.Info?.Log(
                                        $"Roll {baRollHandsy} > {spawnChance}, not adding handsy BA.");
                                }
                            }
                            else
                            {
                                Mod.Log.Info?.Log(
                                    $"No config for handsy BA for faction {unit.team.FactionValue.Name}.");
                            }
                        }
                    }
                }
            }
            else
            {
                Mod.Log.Info?.Log($"Contract ID {unit.Combat.ActiveContract.Override.ID} or Type {unit.Combat.ActiveContract.ContractTypeValue.Name} found in AI Battle Armor spawn exclusion list.");
            }

            //give AI mechs ability to swat or roll
            if (unit is Mech && !(unit is TrooperSquad) && !unit.IsCustomUnitVehicle())
            {
                if (!string.IsNullOrEmpty(Mod.Settings.BattleArmorDeSwarmSwat))
                {
                    if (unit.GetPilot().Abilities
                            .All(x => x.Def.Id != Mod.Settings.BattleArmorDeSwarmSwat) &&
                        unit.ComponentAbilities.All(y =>
                            y.Def.Id != Mod.Settings.BattleArmorDeSwarmSwat))
                    {
                        dm.AbilityDefs.TryGet(Mod.Settings.BattleArmorDeSwarmSwat, out var def);
                        var ability = new Ability(def);
                        Mod.Log.Trace?.Log(
                            $"Adding {ability.Def?.Description?.Id} to {unit.Description?.Name}.");
                        ability.Init(unit.Combat);
                        unit.GetPilot().Abilities.Add(ability);
                        unit.GetPilot().ActiveAbilities.Add(ability);
                    }
                }

                if (!string.IsNullOrEmpty(Mod.Settings.BattleArmorDeSwarmRoll))
                {
                    if (unit.GetPilot().Abilities
                            .All(x => x.Def.Id != Mod.Settings.BattleArmorDeSwarmRoll) &&
                        unit.ComponentAbilities.All(y =>
                            y.Def.Id != Mod.Settings.BattleArmorDeSwarmRoll))
                    {
                        dm.AbilityDefs.TryGet(Mod.Settings.BattleArmorDeSwarmRoll, out var def);
                        var ability = new Ability(def);
                        Mod.Log.Trace?.Log(
                            $"Adding {ability.Def?.Description?.Id} to {unit.Description?.Name}.");
                        ability.Init(unit.Combat);
                        unit.GetPilot().Abilities.Add(ability);
                        unit.GetPilot().ActiveAbilities.Add(ability);
                    }
                }
            }

            if (!(unit is TrooperSquad) && !string.IsNullOrEmpty(Mod.Settings.DeswarmMovementConfig.AbilityDefID))
            {
                if (unit.GetPilot().Abilities
                        .All(x => x.Def.Id != Mod.Settings.DeswarmMovementConfig.AbilityDefID) &&
                    unit.ComponentAbilities.All(y =>
                        y.Def.Id != Mod.Settings.DeswarmMovementConfig.AbilityDefID))
                {
                    unit.Combat.DataManager.AbilityDefs.TryGet(Mod.Settings.DeswarmMovementConfig.AbilityDefID,
                        out var def);
                    var ability = new Ability(def);
                    Mod.Log.Trace?.Log(
                        $"Adding {ability.Def?.Description?.Id} to {unit.Description?.Name}.");
                    ability.Init(unit.Combat);
                    unit.GetPilot().Abilities.Add(ability);
                    unit.GetPilot().ActiveAbilities.Add(ability);
                }
            }
            

            //do we want to generate AI abilities if they already have BA? unsure.
            //if (ModInit.modSettings.BeaconExcludedContractIDs.Contains(unit.Combat.ActiveContract.Override.ID) || ModInit.modSettings.BeaconExcludedContractTypes.Contains(unit.Combat.ActiveContract.ContractTypeValue.Name))
            //{
            //    ModInit.modLog?.Info?.Write($"Contract ID {unit.Combat.ActiveContract.Override.ID} or Type {unit.Combat.ActiveContract.ContractTypeValue.Name} found in command ability exclusion list.");
            //    return;
            //}

            if (unit.Combat.TurnDirector.CurrentRound > 1) return; // don't give abilities to reinforcements?
            if (unit.team.GUID != "be77cadd-e245-4240-a93e-b99cc98902a5") return; // TargetTeam is only team that gets cmdAbilities
                                                                                  // 
            if (!Mod.Settings.commandAbilities_AI.Any(x=>x.FactionIDs.Contains(unit.team.FactionValue.Name)))
            {
                Mod.Log.Info?.Log($"No settings for command abilities for {unit.team.FactionValue.Name}, skipping.");
                return;
            }

            ModState.CurrentFactionSettingsList = new List<Classes.ConfigOptions.AI_FactionCommandAbilitySetting>(new List<Classes.ConfigOptions.AI_FactionCommandAbilitySetting>(
                Mod.Settings.commandAbilities_AI.Where(x=>x.FactionIDs.Contains(unit.team.FactionValue.Name))).OrderBy(y=>y.AddChance));
            Mod.Log.Debug?.Log($"Ordering setting dictionary.");

            ModState.CurrentFactionSettingsList.RemoveAll(x => x.ContractBlacklist.Contains(unit.Combat.ActiveContract
                .Override
                .ID) || x.ContractBlacklist.Contains(unit.Combat.ActiveContract
                .ContractTypeValue.Name));

            if (unit.GetPilot().Abilities.All(x => x.Def.Resource != AbilityDef.ResourceConsumed.CommandAbility))
            {
                Mod.Log.Debug?.Log($"No command abilities on pilot.");
                if (unit.ComponentAbilities.All(x => x.Def.Resource != AbilityDef.ResourceConsumed.CommandAbility))
                {
                    Mod.Log.Debug?.Log($"No command abilities on unit from Components.");
                    foreach (var abilitySetting in ModState.CurrentFactionSettingsList)
                    {
                        if (!ModState.CurrentCommandUnits.ContainsKey(abilitySetting.AbilityDefID))
                        {
                            ModState.CurrentCommandUnits.Add(abilitySetting.AbilityDefID, new Dictionary<string, int>());
                            ModState.CurrentCommandUnits[abilitySetting.AbilityDefID].Add(unit.team.FactionValue.Name, 0);
                        }
                        else
                        {
                            if (!ModState.CurrentCommandUnits[abilitySetting.AbilityDefID].ContainsKey(unit.team.FactionValue.Name))
                            {
                                ModState.CurrentCommandUnits[abilitySetting.AbilityDefID].Add(unit.team.FactionValue.Name, 0);
                            }
                        }

                        if (!ModState.CachedFactionCommandBeacons.ContainsKey(abilitySetting.AbilityDefID))
                        {
                            ModState.CachedFactionCommandBeacons.Add(abilitySetting.AbilityDefID, new Dictionary<string, List<Classes.AI_BeaconProxyInfo>>());
                            ModState.CachedFactionCommandBeacons[abilitySetting.AbilityDefID].Add(unit.team.FactionValue.Name, new List<Classes.AI_BeaconProxyInfo>());
                        }
                        else
                        {
                            if (!ModState.CachedFactionCommandBeacons[abilitySetting.AbilityDefID].ContainsKey(unit.team.FactionValue.Name))
                            {
                                ModState.CachedFactionCommandBeacons[abilitySetting.AbilityDefID].Add(unit.team.FactionValue.Name, new List<Classes.AI_BeaconProxyInfo>());
                            }
                        }

                        abilitySetting.ProcessAIBeaconWeights(dm, unit.team.FactionValue.Name, abilitySetting.AbilityDefID);

                        if (ModState.CurrentCommandUnits[abilitySetting.AbilityDefID][unit.team.FactionValue.Name] >=
                            abilitySetting.MaxUsersAddedPerContract) return;
                        var roll = Mod.Random.NextDouble();
                        var chance = abilitySetting.AddChance +
                                     (abilitySetting.DiffMod * unit.Combat.ActiveContract.Override.finalDifficulty);
                        if (roll <= chance)
                        {
                            Mod.Log.Trace?.Log($"Rolled {roll}, < {chance}.");
                            if (!dm.AbilityDefs.TryGet(abilitySetting.AbilityDefID, out var def))
                            {
                                LoadRequest loadRequest = dm.CreateLoadRequest();
                                loadRequest.AddBlindLoadRequest(BattleTechResourceType.AbilityDef, abilitySetting.AbilityDefID);
                                loadRequest.ProcessRequests(1000U);
                                if (!dm.AbilityDefs.TryGet(abilitySetting.AbilityDefID, out def))
                                {
                                    Mod.Log.Error?.Log($"ERROR couldnt find {abilitySetting.AbilityDefID} in DataManager after loadrequest.");
                                    return;
                                }
                            }
                            var ability = new Ability(def);
                            Mod.Log.Info?.Log(
                                $"Adding {ability.Def?.Description?.Id} to {unit.Description?.Name}.");
                            ModState.CurrentCommandUnits[abilitySetting.AbilityDefID][unit.team.FactionValue.Name] ++;
                            
                            var abilityComponent = unit.allComponents.FirstOrDefault(z => Mod.Settings.crewOrCockpitCustomID.Any((string x) => z.componentDef.GetComponents<Category>().Any((Category c) => c.CategoryID == x)));

                            if (abilityComponent == null)
                            {
                                Mod.Log.Info?.Log($"component was null; no CriticalComponents?");
                            }

                            if (abilityComponent?.parent == null)
                            {
                                Mod.Log.Info?.Log($"component parent was null; no parent actor???");
                            }
                            ability.Init(unit.Combat, abilityComponent);
                            unit.ComponentAbilities.Add(ability);
                            return;
                        }
                    }
                }
            }
        }

        public static bool IsPositionWithinAnAirstrike(AbstractActor unit, Vector3 position)
        {
            foreach (var pendingStrike in ModState.PendingStrafeWaves.Values)
            {
                foreach (var rectangle in pendingStrike.FootPrintRects)
                {
                    Mod.Log.Trace?.Log($"[IsPositionWithinAnAirstrike] position {position} is inside an incoming airstrike. ohnos.");
                    if (rectangle.Contains(position)) return true;
                }
            }
            return false;
        }

        public static void ProcessAIBeaconWeights(this Classes.ConfigOptions.AI_FactionCommandAbilitySetting BeaconWeights, DataManager dm,
            string factionID, string abilityName)
        {
            foreach (var beaconType in BeaconWeights.AvailableBeacons)
            {
                if (beaconType.UnitDefID.StartsWith("mechdef_"))
                {
                    if (dm.Exists(BattleTechResourceType.MechDef, beaconType.UnitDefID) || beaconType.UnitDefID == "BEACON_EMPTY")
                    {
                        Mod.Log.Trace?.Log(
                            $"[ProcessAIBeaconWeights - MechDef] Processing spawn weights for {beaconType.UnitDefID} and weight {beaconType.Weight}");
                        for (int i = 0; i < beaconType.Weight; i++)
                        {
                            ModState.CachedFactionCommandBeacons[BeaconWeights.AbilityDefID][factionID].Add(beaconType);
                            Mod.Log.Trace?.Log(
                                $"[ProcessAIBeaconWeights - MechDef] spawn list has {ModState.CachedFactionCommandBeacons[BeaconWeights.AbilityDefID][factionID].Count} entries");
                        }
                    }
                }
                if (beaconType.UnitDefID.StartsWith("vehicledef_"))
                {
                    if (dm.Exists(BattleTechResourceType.VehicleDef, beaconType.UnitDefID) || beaconType.UnitDefID == "BEACON_EMPTY")
                    {
                        Mod.Log.Trace?.Log(
                            $"[ProcessAIBeaconWeights - VehicleDef] Processing spawn weights for {beaconType.UnitDefID} and weight {beaconType.Weight}");
                        for (int i = 0; i < beaconType.Weight; i++)
                        {
                            ModState.CachedFactionCommandBeacons[BeaconWeights.AbilityDefID][factionID].Add(beaconType);
                            Mod.Log.Trace?.Log(
                                $"[ProcessAIBeaconWeights - VehicleDef] spawn list has {ModState.CachedFactionCommandBeacons[BeaconWeights.AbilityDefID][factionID].Count} entries");
                        }
                    }
                }
                if (beaconType.UnitDefID.StartsWith("turretdef_"))
                {
                    if (dm.Exists(BattleTechResourceType.TurretDef, beaconType.UnitDefID) || beaconType.UnitDefID == "BEACON_EMPTY")
                    {
                        Mod.Log.Trace?.Log(
                            $"[ProcessAIBeaconWeights - TurretDef] Processing spawn weights for {beaconType.UnitDefID} and weight {beaconType.Weight}");
                        for (int i = 0; i < beaconType.Weight; i++)
                        {
                            ModState.CachedFactionCommandBeacons[BeaconWeights.AbilityDefID][factionID].Add(beaconType);
                            Mod.Log.Trace?.Log(
                                $"[ProcessAIBeaconWeights - TurretDef] spawn list has {ModState.CachedFactionCommandBeacons[BeaconWeights.AbilityDefID][factionID].Count} entries");
                        }
                    }
                }
            }
        }

        public static int TargetsForStrafe(AbstractActor actor, Ability ability, out Vector3 startPos, out Vector3 endPos, out AbstractActor targetUnit) //switch to Icombatant
        {
            var maxCount = 0;
            var savedEndVector = new Vector3();
            var savedStartVector = new Vector3();
            AbstractActor savedStartActor = null;

            if (ability != null)
            {
                var maxRange = ability.Def.IntParam2 - ability.Def.FloatParam2;

                var circ = maxRange * 2 * Math.PI;
                var steps = Mathf.RoundToInt((float)circ / (ability.Def.FloatParam1 * 2));

                //var enemyCombatants = new List<ICombatant>(actor.Combat.GetAllImporantCombatants().Where(x=>x.team.IsEnemy(actor.team)));
                var enemyUnits = actor.GetVisibleEnemyUnitsEnemiesOnly();//GetVisibleEnemyUnits dont work; includes neutreal
                enemyUnits.RemoveAll(x=> x.GUID == actor.GUID || !x.IsOperational);

                for (int i = enemyUnits.Count - 1; i >= 0; i--)
                {
                    if (enemyUnits[i].WasDespawned || enemyUnits[i].WasEjected || enemyUnits[i].IsDead || enemyUnits[i].GUID == actor.GUID)
                    {
                        enemyUnits.RemoveAt(i);
                    }
                }

                for (int k = 0; k < enemyUnits.Count; k++)
                {
//                    AbstractActor enemyActor = actor.BehaviorTree.enemyUnits[k] as AbstractActor;
                    if (enemyUnits[k] == null) continue;
                    Vector3 possibleStart;
                    var startActor = default(AbstractActor);
                    if (Mathf.RoundToInt(Vector3.Distance(actor.CurrentPosition, enemyUnits[k].CurrentPosition)) < maxRange)
                    {
                        possibleStart = enemyUnits[k].CurrentPosition;
                        startActor = enemyUnits[k];
                    }
                    else
                    {
                        Mod.Log.Trace?.Log($"[TargetsForStrafe] Unit #{k} {enemyUnits[k].DisplayName} > {maxRange} from starting unit, can't use as starting position.");
                        continue;
                    }

                    var vectors = Utils.MakeCircle(possibleStart, steps, ability.Def.FloatParam2);
                    var currentSavedEndVector = new Vector3();
                    var currentSavedStartVector = new Vector3();
                    AbstractActor currentStartActor = null;
                    var currentMaxCount = 0;
                    Mod.Log.Trace?.Log($"[TargetsForStrafe] Evaluating strafe start position at combatant #{k} {enemyUnits[k].DisplayName} pos {possibleStart}.");
                    for (var index = 0; index < vectors.Length; index++)
                    {
                        var vector = vectors[index];
                        var targetCount = 0;
                        var rectangles = Utils.MakeRectangle(possibleStart, vector, ability.Def.FloatParam1);
                        var rectTargets = new List<string>();
                        for (var i = 0; i < rectangles.Count; i++)
                        {
                            var rectangle = rectangles[i];
                            for (int l = 0; l < enemyUnits.Count; l++)
                            {
                                if (!(enemyUnits[l] is AbstractActor newTarget)) continue;
                                if (rectangle.Contains(newTarget.CurrentPosition) && !rectTargets.Contains(newTarget.GUID))
                                {
                                    rectTargets.Add(newTarget.GUID);
                                    targetCount += 1;
                                    Mod.Log.Trace?.Log($"[TargetsForStrafe] Unit #{k}, VectorStart {possibleStart} VectorEnd {vector}: {targetCount} targets.");
                                }
                            }
                        }

                        if (targetCount >= currentMaxCount)
                        {
                            currentMaxCount = targetCount;
                            currentSavedEndVector = vector;
                            currentSavedStartVector = possibleStart;
                            currentStartActor = startActor;
                            Mod.Log.Trace?.Log(
                                $"TargetsForStrafe] Unit #{k}, possibleStart {currentSavedStartVector} currentSavedEndVector {currentSavedEndVector}: Current highest target count in vector {index} is {currentMaxCount} from start {currentSavedStartVector} and end {currentSavedEndVector}.");
                        }
                    }

                    if (currentMaxCount >= maxCount)
                    {
                        maxCount = currentMaxCount;
                        savedEndVector = currentSavedEndVector;
                        savedStartVector = currentSavedStartVector;
                        savedStartActor = currentStartActor;
                        Mod.Log.Trace?.Log($"[TargetsForStrafe] Unit #{k}:  Current highest target count is {maxCount} from start {savedStartVector} and end {savedEndVector}.");
                    }
                }
                // should probably try to evaluate how many allied units it could hit and offset?

                startPos = savedStartVector;
                endPos = savedEndVector;
                targetUnit = savedStartActor;
                // ModState.selectedAIVectors.Add(savedStartVector);
                // ModState.selectedAIVectors.Add(savedEndVector);
                Mod.Log.Trace?.Log($"[TargetsForStrafe] Final highest target count is {maxCount} from start {startPos} and end {endPos}.");
                return maxCount;
            }

            startPos = default(Vector3);
            endPos = default(Vector3);
            targetUnit = null;
            return 0;
        }
    }
}