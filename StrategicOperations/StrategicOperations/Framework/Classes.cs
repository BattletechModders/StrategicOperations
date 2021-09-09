using System.Collections.Generic;
using BattleTech;
using IRBTModUtils.CustomInfluenceMap;
using IRBTModUtils.Extension;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace StrategicOperations.Framework
{

    public class Classes
    {
        public class BA_DamageTracker
        {
            public string TargetGUID = ""; // guid of carrier unit.
            public bool IsSquadInternal;
            public Dictionary<int, int> BA_MountedLocations = new Dictionary<int, int>(); // key is BA unit chassis location (HD, CT, LT, RT, LA, RA), value is BA mounted ARMOR location on carrier.

            public BA_DamageTracker()
            {
                this.TargetGUID = "";
                this.IsSquadInternal = false;
                this.BA_MountedLocations = new Dictionary<int, int>();
            }

            public BA_DamageTracker(string targetGUID, bool internalSquad, Dictionary<int, int> mountedLocations)
            {
                this.TargetGUID = targetGUID;
                this.IsSquadInternal = internalSquad;
                this.BA_MountedLocations = mountedLocations;
            }
        }

        public class BA_TargetEffect
        {
            public string ID = "";
            public string Name = "";
            public string Description = "";

            [JsonIgnore]
            public List<EffectData> effects = new List<EffectData>();
            public List<JObject> effectDataJO = new List<JObject>();

        }
        public class SpawnCoords
        {
            public string ID;
            public Vector3 Loc;
            public float DistFromTarget;

            public SpawnCoords(string id, Vector3 loc, float distFromTarget)
            {
                this.ID = id;
                this.Loc = loc;
                this.DistFromTarget = distFromTarget;
            }
        }
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

            public CmdUseStat(string id, string stat, bool consumeOnUse, int contractUses, int simStatCount, string pilotId = null)
            {
                this.ID = id;
                this.stat = stat;
                this.pilotID = pilotId;
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

            public CmdUseInfo(string unitId, string commandName, string unitName, int useCost, int abilityUseCost)
            {
                this.UnitID = unitId;
                this.CommandName = commandName;
                this.UnitName = unitName;
                this.UseCost = useCost;
                this.AbilityUseCost = abilityUseCost;
                this.UseCount = 1;
            }
        }

        public class BA_MountOrSwarmInvocation
        {
            public Ability ability;
            public AbstractActor targetActor;
            public bool active;

            public BA_MountOrSwarmInvocation()
            {
                this.ability = default(Ability);
                this.targetActor = default(AbstractActor);
                this.active = false;
            }
            public BA_MountOrSwarmInvocation(Ability cmdAbility, AbstractActor targetActor, bool active)
            {
                this.ability = cmdAbility;
                this.targetActor = targetActor;
                this.active = active;
            }
        }

        public class AI_CmdInvocation
        {
            public Ability ability;
            public Vector3 vectorOne;
            public Vector3 vectorTwo;
            public bool active;
            public float dist;

            public AI_CmdInvocation()
            {
                this.ability = default(Ability);
                this.vectorOne = new Vector3();
                this.vectorTwo = new Vector3();
                this.active = false;
                this.dist = Vector3.Distance(vectorOne, vectorTwo);
            }
            public AI_CmdInvocation(Ability cmdAbility, Vector3 firstVector, Vector3 secondVector, bool active)
            {
                this.ability = cmdAbility;
                this.vectorOne = firstVector;
                this.vectorTwo = secondVector;
                this.active = active;
                this.dist = Vector3.Distance(vectorOne, vectorTwo);
            }
        }
        public class AI_SpawnBehavior
        {
            public string Tag;
            public string Behavior;
            public int MinRange;

            public AI_SpawnBehavior()
            {
                Tag = "DEFAULT";
                Behavior = "DEFAULT";
                MinRange = 50;
            }
        }
        public class AI_CommandAbilitySetting
        {
            public string AbilityID;
            public float AddChance;
            public float DiffMod;
        }
    }
}
