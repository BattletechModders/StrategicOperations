using System;
using BattleTech;
using BattleTech.Framework;
using BattleTech.UI;
using Harmony;
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
                var addObjectiveMethod = Traverse.Create(__instance).Method("AddObjective", new Type[] { typeof(MissionObjectiveResult) });
                foreach (var cmdUse in ModState.CommandUses)
                {
                    var cmdUseCost = $"Command Ability Costs for {cmdUse.CommandName}: {cmdUse.UnitName}: {cmdUse.UseCount} Uses x {cmdUse.UseCostAdjusted} ea. = ¢-{cmdUse.TotalCost}";

                    var cmdUseCostResult = new MissionObjectiveResult($"{cmdUseCost}", Guid.NewGuid().ToString(), false, true, ObjectiveStatus.Failed, false);

                    addObjectiveMethod.GetValue(cmdUseCostResult);
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
                    ModInit.modLog.LogMessage($"{cmdUse.TotalCost} in command costs for {cmdUse.CommandName}: {cmdUse.UnitName}. Current Total Command Cost: {finalCommandCosts}");
                }

                var moneyResults = __instance.MoneyResults - finalCommandCosts;
                Traverse.Create(__instance).Property("MoneyResults").SetValue(moneyResults);
            }
        }
    }
}
