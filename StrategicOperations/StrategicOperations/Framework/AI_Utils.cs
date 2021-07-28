using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using UnityEngine;

namespace StrategicOperations.Framework
{
    public class AI_Utils
    {
        public static float EvaluateStrafing(AbstractActor actor, out Ability ability, out Vector3 startpoint, out Vector3 endpoint)
        {
            startpoint = default(Vector3);
            endpoint = default(Vector3);
            if (!CanStrafe(actor, out ability)) return 0f;
            var dmg = CalcExpectedDamage(actor, ability.Def.ActorResource);

            var targets = TargetsForStrafe(actor, out startpoint, out endpoint);

            return dmg * targets;
        }

        public static void GenerateAIStrategicAbilities(Team team, int difficulty)
        {
            var dm = team.Combat.DataManager;
            var cmdAbilities = new List<Ability>();
            foreach (var ability in ModInit.modSettings.commandAbilities_AI)
            {
                dm.AbilityDefs.TryGet(ability, out var def);
                cmdAbilities.Add(new Ability(def));
            }

            foreach (var unit in team.units)
            {
                unit.ComponentAbilities.Add(Utils.GetRandomFromList(cmdAbilities));
            }
        }

        public static bool CanStrafe(AbstractActor actor, out Ability ability)
        {
            var _ability =
                actor.ComponentAbilities.FirstOrDefault(x => x.Def.specialRules == AbilityDef.SpecialRules.Strafe);
            if (_ability != null)
            {
                ability = _ability;
                return true;
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

        public static int TargetsForStrafe(AbstractActor actor, out Vector3 startPos, out Vector3 endPos) //switch to Icombatant
        {
            var maxCount = 0;
            var savedEndVector = new Vector3();
            var savedStartVector = new Vector3();
            var ability =
                actor.ComponentAbilities.FirstOrDefault(x => x.Def.specialRules == AbilityDef.SpecialRules.Strafe);

            if (ability != null)
            {
                var maxRange = ability.Def.IntParam2;

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

                    var vectors = Utils.MakeCircle(possibleStart,steps, ability.Def.FloatParam2);
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
    }
}
