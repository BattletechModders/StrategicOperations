using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using BattleTech.Framework;
using BattleTech.UI;
using Harmony;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace StrategicOperations.Framework
{
    public static class Utils
    {
        public static Vector3[] MakeCircle(Vector3 start,int numOfPoints, float radius)
        {
            var vectors = new List<Vector3>();
            for (int i = 0; i < numOfPoints; i++)
            {
                float angle = i * Mathf.PI * 2f / 8;
                var newPos = new Vector3(Mathf.Cos(angle) * radius, start.y, Mathf.Sin(angle) * radius);
                vectors.Add(newPos);
            }

            return vectors.ToArray();
        }

        public static Rect[] MakeRectangle(Vector3 start, Vector3 end, float width)
        {
            
            var rectangles = new List<Rect>();
            Vector3 line = end - start;
            float length = Vector3.Distance(start, end);

            Vector3 left = Vector3.Cross(line, Vector3.up).normalized;
            Vector3 right = -left;
            var startLeft = start + (left * width);
            var startRight = start + (right * width);

            var rectLeft = new Rect(startLeft.x, startLeft.y, width, length);
            var rectRight = new Rect(startRight.x, startRight.y, width, length);


            rectangles.Add(rectLeft);
            rectangles.Add(rectRight);

            return rectangles.ToArray();
        }

        public static T GetRandomFromList<T>(List<T> list)
        {
            var idx = UnityEngine.Random.Range(0, list.Count);
            return list[0];
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
            public int UseCostAdjusted => Mathf.RoundToInt(UseCost * ModInit.modSettings.commandUseCostsMulti + AbilityUseCost);
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
        public static Lance CreateCMDLance(Team team)
        {
            Lance lance = new Lance(team, new LanceSpawnerRef[0]);
            var lanceGuid = LanceSpawnerGameLogic.GetLanceGuid(Guid.NewGuid().ToString());
            lance.lanceGuid = lanceGuid;
            var combat = UnityGameInstance.BattleTechGame.Combat;
            combat.ItemRegistry.AddItem(lance);
            team.lances.Add(lance);
            ModInit.modLog.LogMessage($"Created lance {lance.DisplayName} for Team {team.DisplayName}.");
            return lance;
        }
        public static void CooldownAllCMDAbilities()
        {
            for (int i = 0; i < ModState.CommandAbilities.Count; i++)
            {
                ModState.CommandAbilities[i].ActivateMiniCooldown();
            }
        }

        public static void CreateOrUpdateNeutralTeam()
        {
            AITeam aiteam = null;
            var combat = UnityGameInstance.BattleTechGame.Combat;
            if (combat.IsLoadingFromSave)
            {
                aiteam = (combat.GetLoadedTeamByGUID("61612bb3-abf9-4586-952a-0559fa9dcd75") as AITeam);
            }
            if (!combat.IsLoadingFromSave || aiteam == null)
            {
                aiteam = new AITeam("Player 1 Support", Color.yellow, "61612bb3-abf9-4586-952a-0559fa9dcd75", true, combat);
            }
            combat.TurnDirector.AddTurnActor(aiteam);
            combat.ItemRegistry.AddItem(aiteam);
        }

        public static List<MechComponentRef> GetOwnedDeploymentBeacons()
        {
            var sgs = UnityGameInstance.BattleTechGame.Simulation;
            var beacons = new List<MechComponentRef>();
            foreach (var stat in ModInit.modSettings.deploymentBeaconEquipment)
            {
                if (sgs.CompanyStats.GetValue<int>(stat) > 0)
                {
                    string[] array = stat.Split(new char[]
                    {
                        '.'
                    });
                    if (string.Compare(array[1], "MECHPART") != 0)
                    {
                        BattleTechResourceType battleTechResourceType = (BattleTechResourceType)Enum.Parse(typeof(BattleTechResourceType), array[1]);
                        if (battleTechResourceType != BattleTechResourceType.MechDef && sgs.DataManager.Exists(battleTechResourceType, array[2]))
                        {
                            bool flag = array.Length > 3 && array[3].CompareTo("DAMAGED") == 0;
                            MechComponentDef componentDef = sgs.GetComponentDef(battleTechResourceType, array[2]);
                            MechComponentRef mechComponentRef = new MechComponentRef(componentDef.Description.Id, sgs.GenerateSimGameUID(), componentDef.ComponentType, ChassisLocations.None, -1, flag ? ComponentDamageLevel.NonFunctional : ComponentDamageLevel.Functional, false);
                            mechComponentRef.SetComponentDef(componentDef);

                            if (mechComponentRef.Def.ComponentTags.All(x => x != "CanSpawnTurret" && x != "CanStrafe")) continue;
                            var id = mechComponentRef.Def.ComponentTags.FirstOrDefault(x =>
                                x.StartsWith("mechdef_") || x.StartsWith("vehicledef_") ||
                                x.StartsWith("turretdef_"));

                            bool consumeOnUse = mechComponentRef.Def.ComponentTags.Any(x => x == "ConsumeOnUse");
                            var pilotID = mechComponentRef.Def.ComponentTags.FirstOrDefault(x =>
                                x.StartsWith("StratOpsPilot_"))
                                ?.Remove(0, 14);

                            if (ModState.deploymentAssetsStats.All(x => x.ID != id))
                            {
                                var value = sgs.CompanyStats.GetValue<int>(stat);
                                var newStat = new CmdUseStat(id, stat, consumeOnUse, value, value, pilotID);
                                ModState.deploymentAssetsStats.Add(newStat);
                                ModInit.modLog.LogMessage($"Added {id} to deploymentAssetsDict with value {value}.");
                            }
                            beacons.Add(mechComponentRef);
                        }
                    }
                }
            }
            return beacons;
        }

        public static List<MechComponentRef> GetOwnedDeploymentBeaconsOfByTypeAndTag(string type, string tag)
        {
            var sgs = UnityGameInstance.BattleTechGame.Simulation;
            var beacons = new List<MechComponentRef>();
            foreach (var stat in ModInit.modSettings.deploymentBeaconEquipment)
            {
                if (sgs.CompanyStats.GetValue<int>(stat) > 0)
                {
                    string[] array = stat.Split(new char[]
                    {
                        '.'
                    });
                    if (string.Compare(array[1], "MECHPART") != 0)
                    {
                        BattleTechResourceType battleTechResourceType = (BattleTechResourceType)Enum.Parse(typeof(BattleTechResourceType), array[1]);
                        if (battleTechResourceType != BattleTechResourceType.MechDef && sgs.DataManager.Exists(battleTechResourceType, array[2]))
                        {
                            bool flag = array.Length > 3 && array[3].CompareTo("DAMAGED") == 0;
                            MechComponentDef componentDef = sgs.GetComponentDef(battleTechResourceType, array[2]);
                            MechComponentRef mechComponentRef = new MechComponentRef(componentDef.Description.Id, sgs.GenerateSimGameUID(), componentDef.ComponentType, ChassisLocations.None, -1, flag ? ComponentDamageLevel.NonFunctional : ComponentDamageLevel.Functional, false);
                            mechComponentRef.SetComponentDef(componentDef);

                            if ((tag == "CanSpawnTurret" && mechComponentRef.Def.ComponentTags.All(x => x != "CanSpawnTurret")) || (tag == "CanStrafe" && mechComponentRef.Def.ComponentTags.All(x => x != "CanStrafe"))) continue;

                            bool consumeOnUse = mechComponentRef.Def.ComponentTags.Any(x => x == "ConsumeOnUse");

                            var id = mechComponentRef.Def.ComponentTags.FirstOrDefault(x =>
                                x.StartsWith("mechdef_") || x.StartsWith("vehicledef_") ||
                                x.StartsWith("turretdef_"));
                            if (!id.StartsWith(type))
                            {
//                                ModInit.modLog.LogMessage($"{id} != {type}, ignoring.");
                                continue;
                            }
                            var pilotID = mechComponentRef.Def.ComponentTags.FirstOrDefault(x =>
                                    x.StartsWith("StratOpsPilot_"))
                                ?.Remove(0, 14);
                            if (ModState.deploymentAssetsStats.All(x => x.ID != id))
                            {

                                var value = sgs.CompanyStats.GetValue<int>(stat);
                                var newStat = new CmdUseStat(id, stat, consumeOnUse, value, value, pilotID);
                                ModState.deploymentAssetsStats.Add(newStat);
                                ModInit.modLog.LogMessage($"Added {id} to deploymentAssetsDict with value {value}.");
                            }
                            beacons.Add(mechComponentRef);
                        }
                    }
                }
            }
            return beacons;
        }

        public static void DeployEvasion(AbstractActor actor)
        {
            ModInit.modLog.LogMessage($"Adding deploy protection to {actor.DisplayName}.");
            
                if (actor is Turret turret)
                {
                    ModInit.modLog.LogMessage($"{actor.DisplayName} is a turret, skipping.");
                    return;
                }

                if (ModInit.modSettings.deployProtection > 0)
                {
                    ModInit.modLog.LogMessage($"Adding {ModInit.modSettings.deployProtection} evasion pips");
                    actor.EvasivePipsCurrent = ModInit.modSettings.deployProtection;
                    AccessTools.Property(typeof(AbstractActor), "EvasivePipsTotal").SetValue(actor, actor.EvasivePipsCurrent, null);
                }
        }

        public static void DP_AnimationComplete(string encounterObjectGUID)
        {
            EncounterLayerParent.EnqueueLoadAwareMessage(new DropshipAnimationCompleteMessage(LanceSpawnerGameLogic.GetDropshipGuid(encounterObjectGUID)));
        }

        public static MethodInfo _activateSpawnTurretMethod = AccessTools.Method(typeof(Ability), "ActivateSpawnTurret");
        public static MethodInfo _activateStrafeMethod = AccessTools.Method(typeof(Ability), "ActivateStrafe");
        public static MethodInfo _despawnActorMethod = AccessTools.Method(typeof(AbstractActor), "DespawnActor");

    }
}
