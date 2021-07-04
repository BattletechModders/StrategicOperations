using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using HBS.Math;
using UnityEngine;

namespace StrategicOperations.Framework
{
    class AI_Utils
    {

        public void GenerateAIStrategicAbilities(Team team, int difficulty)
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

        public int TargetsForStrafe(AbstractActor actor)
        {
            var maxCount = 0;
            var savedEndVector = new Vector3();
            var savedStartVector = new Vector3();
            var ability =
                actor.ComponentAbilities.FirstOrDefault(x => x.Def.specialRules == AbilityDef.SpecialRules.Strafe);

            if (ability != null)
            {
                var maxrange = ability.Def.IntParam2;
                var possibleStart = new Vector3();

                var circ = ability.Def.IntParam2 * 2 * Math.PI;
                var steps = Mathf.RoundToInt((float)circ / (ability.Def.FloatParam1 * 2));

                for (int k = 0; k < actor.BehaviorTree.enemyUnits.Count; k++)
                {
                    AbstractActor enemyActor = actor.BehaviorTree.enemyUnits[k] as AbstractActor;
                    if (enemyActor == null) continue;
                    if (Mathf.RoundToInt(Vector3.Distance(actor.CurrentPosition, enemyActor.CurrentPosition)) < maxrange)
                    {
                        possibleStart = enemyActor.CurrentPosition;
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
                            for (int l = 0; l < actor.BehaviorTree.enemyUnits.Count; l++)
                            {
                                AbstractActor newTarget = actor.BehaviorTree.enemyUnits[k] as AbstractActor;
                                if (newTarget == null) continue;
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
                ModState.selectedAIVectors.Add(savedStartVector);
                ModState.selectedAIVectors.Add(savedEndVector);
                return maxCount;
            }

            return 0;
        }
    }
}
