using BattleTech;
using BattleTech.UI;

namespace StrategicOperations.Patches
{
    [HarmonyPatch(typeof(CombatHUDPhaseIcons), "GetIconHolderForTeam")]
    internal static class CombatHUDPhaseIcons_GetIconHolderForTeam
    {
        [HarmonyPrefix]
        internal static void Prefix(ref bool __runOriginal, CombatHUDPhaseIcons __instance, string teamGUID, Dictionary<string, int> ___teamMap, ref GameObject __result)
        {
            if (!__runOriginal)
            {
                return;
            }
            GameObject[] columns = __instance.teamColumns;
            if (___teamMap.TryGetValue(teamGUID, out int index) && index < columns.Length)
            {
                return;
            }

            int fallbackColumn = FallbackColumn(teamGUID, ___teamMap, columns.Length);
            __result = columns[fallbackColumn];
            __runOriginal = false;
        }

        private static int FallbackColumn(string teamGUID, Dictionary<string, int> teamMap, int columnCount)
        {
            CombatGameState combat = UnityGameInstance.BattleTechGame.Combat;
            Team team = combat?.Teams.Find(t => t.GUID == teamGUID);
            if (team != null)
            {
                foreach (Team other in combat.Teams)
                {
                    if (other != team && team.IsFriendly(other) && teamMap.TryGetValue(other.GUID, out int index) && index < columnCount)
                    {
                        return index;
                    }
                }
            }
            return columnCount - 1;
        }
    }
}
