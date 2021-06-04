using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using BattleTech.Framework;

namespace StrategicOperations.Framework
{
    public static class Utils
    {
        public static Lance CreateCMDLance(Team team)
        {
            Lance lance = new Lance(team, new LanceSpawnerRef[0]);
            var lanceGuid = LanceSpawnerGameLogic.GetLanceGuid(Guid.NewGuid().ToString());
            lance.lanceGuid = lanceGuid;
            var combat = UnityGameInstance.BattleTechGame.Combat;
            combat.ItemRegistry.AddItem(lance);
            team.lances.Add(lance);
            return lance;
        }
        public static void CooldownAllCMDAbilities()
        {
            for (int i = 0; i < ModState.CommandAbilities.Count; i++)
            {
                ModState.CommandAbilities[i].ActivateMiniCooldown();
            }
        }
    }
}
