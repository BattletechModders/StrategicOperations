using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using UnityEngine;

namespace StrategicOperations.Framework
{
    public class Classes
    {
        public class ColorSetting
        {
            public int r;
            public int g;
            public int b;

            public float Rf => r / 255f;
            public float Gf => g / 255f;
            public float Bf => b / 255f;
        }

        public class CmdUseStat
        {
            public string ID;
            public string stat;
            public string pilotID;
            public bool consumeOnUse;
            public int contractUses;
            public int simStatCount;

            public CmdUseStat(string ID, string stat, bool consumeOnUse, int contractUses, int simStatCount, string pilotID = null)
            {
                this.ID = ID;
                this.stat = stat;
                this.pilotID = pilotID;
                this.consumeOnUse = consumeOnUse;
                this.contractUses = contractUses;
                this.simStatCount = simStatCount;
            }
        }
        public class CmdUseInfo
        {
            public string UnitID;
            public string CommandName;
            public string UnitName;
            public int UseCost;
            public int AbilityUseCost;
            public int UseCostAdjusted => Mathf.RoundToInt((UseCost * ModInit.modSettings.commandUseCostsMulti) + AbilityUseCost);
            public int UseCount;
            public int TotalCost => UseCount * UseCostAdjusted;

            public CmdUseInfo(string unitID, string CommandName, string UnitName, int UseCost, int AbilityUseCost)
            {
                this.UnitID = unitID;
                this.CommandName = CommandName;
                this.UnitName = UnitName;
                this.UseCost = UseCost;
                this.AbilityUseCost = AbilityUseCost;
                this.UseCount = 1;
            }
        }

        public class AI_CmdInvocation
        {
            public Ability ability;
            public Vector3 vectorOne;
            public Vector3 vectorTwo;
            public bool active;

            public AI_CmdInvocation()
            {
                this.ability = default(Ability);
                this.vectorOne = new Vector3();
                this.vectorTwo = new Vector3();
                this.active = false;
            }
            public AI_CmdInvocation(Ability cmdAbility, Vector3 firstVector, Vector3 secondVector, bool active)
            {
                this.ability = cmdAbility;
                this.vectorOne = firstVector;
                this.vectorTwo = secondVector;
                this.active = active;
            }
        }
    }
}
