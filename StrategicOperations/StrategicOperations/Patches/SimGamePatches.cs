using System;
using System.Collections.Generic;
using BattleTech;
using BattleTech.Framework;
using BattleTech.Save;
using BattleTech.UI;
using CustAmmoCategories;
using IRBTModUtils.Logging;
using Newtonsoft.Json;
using StrategicOperations.Framework;

namespace StrategicOperations.Patches
{
    public class SimGamePatches
    {
        [HarmonyPatch(typeof(AAR_ContractObjectivesWidget), "FillInObjectives")]
        public static class AAR_ContractObjectivesWidget_FillInObjectives_Patch
        {
            public static void Postfix(AAR_ContractObjectivesWidget __instance)
            {
                if (UnityGameInstance.BattleTechGame.Simulation == null) return;
                if (ModState.CommandUses.Count <= 0) return;
                //var addObjectiveMethod = Traverse.Create(__instance).Method("AddObjective", new Type[] { typeof(MissionObjectiveResult) });
                foreach (var cmdUse in ModState.CommandUses)
                {
                    if (cmdUse.TotalCost <= 0) continue;
                    var cmdUseCost = $"Command Ability Costs for {cmdUse.CommandName}: {cmdUse.UnitName}: {cmdUse.UseCount} Uses x {cmdUse.UseCostAdjusted} ea. = ¢-{cmdUse.TotalCost}";

                    var cmdUseCostResult = new MissionObjectiveResult($"{cmdUseCost}", Guid.NewGuid().ToString(), false, true, ObjectiveStatus.Ignored, false);
                    __instance.AddObjective(cmdUseCostResult);
                    //addObjectiveMethod.GetValue(cmdUseCostResult);
                }
            }
        }

        [HarmonyPatch(typeof(Contract), "CompleteContract", new Type[] {typeof(MissionResult), typeof(bool)})]
        public static class Contract_CompleteContract_Patch
        {
            public static void Postfix(Contract __instance, MissionResult result, bool isGoodFaithEffort)
            {
                if (UnityGameInstance.BattleTechGame.Simulation == null) return;
                if (ModState.CommandUses.Count <= 0) return;
                var finalCommandCosts = 0;
                foreach (var cmdUse in ModState.CommandUses)
                {
                    finalCommandCosts += cmdUse.TotalCost;
                    ModInit.modLog?.Info?.Write($"{cmdUse.TotalCost} in command costs for {cmdUse.CommandName}: {cmdUse.UnitName}. Current Total Command Cost: {finalCommandCosts}");
                }

                var moneyResults = __instance.MoneyResults - finalCommandCosts;
                //Traverse.Create(__instance).Property("MoneyResults").SetValue(moneyResults);
                __instance.MoneyResults = moneyResults;
            }
        }
    }
    
    [HarmonyPatch(typeof (SimGameState), "Rehydrate")]
    public static class SimGameState_Rehdyrate_Patch
    {
        public static void Postfix(GameInstanceSave gameInstanceSave, SimGameState __instance)
        {
            Statistic statisic = StatisticHelper.GetOrCreateStatisic<string>(__instance.CompanyStats, LastUsedMounts.LAST_MOUNTS_STATISTIC_NAME, "[]");
            ModState.LastMounts = JsonConvert.DeserializeObject<List<LastUsedMounts>>(statisic.Value<string>());
            ModInit.modLog?.Info?.Write($"Last mount stat: {statisic.Value<string>()}");
        }
    }
    
    [HarmonyPatch(typeof (GameInstanceSave), "PreSerialization")]
    public static class GameInstanceSave_PreSerialization_Patch
    {
        public static void Prefix()
        {
            ModInit.modLog?.Info?.Write("Saving LastMounts to company stats");
            Statistic statisic = StatisticHelper.GetOrCreateStatisic<string>(UnityGameInstance.BattleTechGame.Simulation.CompanyStats, LastUsedMounts.LAST_MOUNTS_STATISTIC_NAME, "[]");
            statisic.SetValue<string>(JsonConvert.SerializeObject((object) ModState.LastMounts, Formatting.Indented));
            ModInit.modLog?.Info?.Write($"Last mount stat: {statisic.Value<string>()}");
        }
    }
    
    [HarmonyPatch(typeof (GameInstanceSave), "PostSerialization")]
    public static class GameInstanceSave_PostSerialization_Patch
    {
        [HarmonyPostfix]
        public static void RemoveLastMounts()
        {
            ModInit.modLog?.Info?.Write("Clearing LastMounts from company stats");
            UnityGameInstance.BattleTechGame.Simulation.CompanyStats.RemoveStatistic(LastUsedMounts.LAST_MOUNTS_STATISTIC_NAME);
        }
    }
}
