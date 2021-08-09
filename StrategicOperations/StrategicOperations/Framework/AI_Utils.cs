using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using Abilifier;
using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using UnityEngine;
using static StrategicOperations.Framework.Classes;
using Random = UnityEngine.Random;

namespace StrategicOperations.Framework
{
    public class AI_Utils
    {
        public static int EvaluateStrafing(AbstractActor actor, out Ability ability, out Vector3 startpoint, out Vector3 endpoint)
        {
            startpoint = default(Vector3);
            endpoint = default(Vector3);
            if (!CanStrafe(actor, out ability)) return 0;
            var dmg = Mathf.RoundToInt(CalcExpectedDamage(actor, ability.Def.ActorResource));

            var targets = TargetsForStrafe(actor, ability,out startpoint, out endpoint);

            return dmg * targets;
        }

        public static void GenerateAIStrategicAbilities(AbstractActor unit)
        {
            if (unit is Turret) return;
            var dm = unit.Combat.DataManager;
            var cmdAbilities = new List<Ability>();
            foreach (var ability in ModInit.modSettings.commandAbilities_AI)
            {
                dm.AbilityDefs.TryGet(ability, out var def);
                cmdAbilities.Add(new Ability(def));
            }

            if (unit.GetPilot().Abilities.All(x => x.Def.Resource != AbilityDef.ResourceConsumed.CommandAbility))
            {
                ModInit.modLog.LogTrace($"No command abilities on pilot.");
                if (unit.ComponentAbilities.All(x => x.Def.Resource != AbilityDef.ResourceConsumed.CommandAbility))
                {
                    ModInit.modLog.LogTrace($"No command abilities on unit from Components.");
                    var roll = ModInit.Random.NextDouble();
                    var chance = ModInit.modSettings.AI_CommandAbilityAddChance + (ModInit.modSettings.AI_CommandAbilityDifficultyMod * unit.Combat.ActiveContract.Override.finalDifficulty);
                    if (roll <= chance)
                    {
                        ModInit.modLog.LogTrace($"Rolled {roll}, < {chance}.");
                        var ability = Utils.GetRandomFromList(cmdAbilities);
                        ModInit.modLog.LogTrace($"Adding {ability.Def?.Description?.Id} to {unit.Description?.Name}.");
                        ability.Init(unit.Combat);
                        unit.ComponentAbilities.Add(ability);
                    }
                }
            }// did i add things o the wrong team? probably.
            
        }

        public static string AssignRandomSpawnAsset(Ability ability)
        {
            var sgs = UnityGameInstance.BattleTechGame.Simulation;
            var dm = UnityGameInstance.BattleTechGame.DataManager;
            var potentialAssetsForAI = new List<string>();

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
                    return "";
                }
                var allowedUnitTags = ability.Def.StringParam2;

                foreach (var stat in ModInit.modSettings.deploymentBeaconEquipment)
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
                            potentialAssetsForAI.Add(id);
                            ModInit.modLog.LogTrace($"Added {id} to potential AI assets.");
                        }
                    }
                }

                var chosen = Utils.GetRandomFromList(potentialAssetsForAI);
                ModInit.modLog.LogTrace($"Chose {chosen} for this activation.");
                return chosen;
            }
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
                var assetID = AssignRandomSpawnAsset(ability);

                var asset = Utils.FetchUnitFromDM(actor.Combat.DataManager, assetID);

                var behaviorType = "";
                if (asset is MechDef mech)
                {
                    foreach (var key in ModInit.modSettings.AI_SpawnBehaviorTags.Keys)
                    {
                        if (mech.MechTags.Contains(key))
                        {
                            behaviorType = ModInit.modSettings.AI_SpawnBehaviorTags[key];
                            goto behaviorEvalFinished;
                        }
                    }
                }
                else if (asset is VehicleDef vehicle)
                {
                    foreach (var key in ModInit.modSettings.AI_SpawnBehaviorTags.Keys)
                    {
                        if (vehicle.VehicleTags.Contains(key))
                        {
                            behaviorType = ModInit.modSettings.AI_SpawnBehaviorTags[key];
                            goto behaviorEvalFinished;
                        }
                    }
                }
                else if (asset is TurretDef turret)
                {
                    foreach (var key in ModInit.modSettings.AI_SpawnBehaviorTags.Keys)
                    {
                        if (turret.TurretTags.Contains(key))
                        {
                            behaviorType = ModInit.modSettings.AI_SpawnBehaviorTags[key];
                            goto behaviorEvalFinished;
                        }
                    }
                }
                else
                {
                    behaviorType = "DEFAULT";
                }


                behaviorEvalFinished:

                if (behaviorType == "REINFORCE")
                {
                   
                }

                if (behaviorType == "DEFAULT" || behaviorType == "BRAWLER")
                {
                    var maxRange = ability.Def.IntParam2;
                    //var enemyActors = new List<AbstractActor>(actor.Combat.AllEnemies);
                    var enemyActors = actor.team.VisibilityCache.GetAllDetectedEnemies(actor);
                    ModInit.modLog.LogTrace(
                        $"found {enemyActors.Count} to eval");
                    enemyActors.RemoveAll(x => x.WasDespawned || x.IsDead || x.IsFlaggedForDeath || x.WasEjected);

                    ModInit.modLog.LogTrace(
                        $"found {enemyActors.Count} after eval");
                    var center = new Vector3(0, 0, 0);
                    var count = 0;
                    foreach (var enemy in enemyActors)
                    {
                        center += enemy.CurrentPosition;
                        count++;
                        ModInit.modLog.LogTrace(
                            $"enemyActors count = {count}");
                    }

                    var avgCenter = new Vector3();
                    var theCenter = new Vector3();
                    var orientation = new Vector3(0, 0, 0);
                    var finalOrientation = new Vector3();


                    if (count == 0)
                    {
                        ModInit.modLog.LogTrace(
                            $"FINAL enemyActors count = {count}");
                        theCenter = center;
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

                    foreach (var enemy in enemyActors)
                    {
                        orientation += (enemy.CurrentPosition - avgCenter);
                    }

                    finalOrientation = orientation / count;


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