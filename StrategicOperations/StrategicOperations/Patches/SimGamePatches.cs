using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using BattleTech.Framework;
using BattleTech.UI;
using Harmony;
using StrategicOperations.Framework;
using UnityEngine;

namespace StrategicOperations.Patches
{
    class SimGamePatches
    {
        [HarmonyPatch(typeof(AAR_ContractObjectivesWidget), "FillInObjectives")]
        public static class AAR_ContractObjectivesWidget_FillInObjectives_Patch
        {
            public static void Postfix(AAR_ContractObjectivesWidget __instance, Contract ___theContract)
            {
                if (UnityGameInstance.BattleTechGame.Simulation == null) return;
                if (ModState.CommandUses.Count <= 0 || !(ModInit.modSettings.commandUseCostsMulti > 0)) return;
                var addObjectiveMethod = Traverse.Create(__instance).Method("AddObjective", new Type[] { typeof(MissionObjectiveResult) });
                var finalCommandCosts = 0;
                foreach (var cmdUse in ModState.CommandUses)
                {
                    var cmdUseCost = $"Command Ability Costs for {cmdUse.CommandName}: {cmdUse.UnitName}: {cmdUse.UseCount} Uses x {cmdUse.UseCostAdjusted} ea. = ¢-{cmdUse.TotalCost}";

                    var cmdUseCostResult = new MissionObjectiveResult($"{cmdUseCost}", Guid.NewGuid().ToString(), false, true, ObjectiveStatus.Failed, false);

                    addObjectiveMethod.GetValue(cmdUseCostResult);
                    finalCommandCosts += cmdUse.TotalCost;
                    ModInit.modLog.LogMessage($"{cmdUseCost} in command costs for {cmdUse.CommandName}: {cmdUse.UnitName}. Current Total Command Cost: {finalCommandCosts}");
                }

                var moneyResults = ___theContract.MoneyResults - finalCommandCosts;
                Traverse.Create(___theContract).Property("MoneyResults").SetValue(moneyResults);
            }
        }
    }
}
