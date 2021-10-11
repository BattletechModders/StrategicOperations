using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using CustAmmoCategories;
using CustomAmmoCategoriesPatches;
using Harmony;

namespace StrategicOperations.Patches
{
    class DirtyDebugPatches
    {
        [HarmonyPatch(typeof(ReserveActorInvocation), "Invoke", new Type[] { typeof(CombatGameState) })]
        public static class ReserveActorInvocation_Invoke_ShittyBypass
        {
            static bool Prepare() => true; //enabled
            public static void Prefix(ReserveActorInvocation __instance, CombatGameState combatGameState)
            {
                if (__instance.targetRound != combatGameState.TurnDirector.CurrentRound)
                {
                    ModInit.modLog.LogMessage($"[ReserveActorInvocation.Invoke]: Running shitty bypass");
                    var actor = combatGameState.FindActorByGUID(__instance.targetGUID);
                    if (!actor.team.IsLocalPlayer)
                    {
                        __instance.targetRound = combatGameState.TurnDirector.CurrentRound;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Vehicle), "ArmorForLocation")]
        public static class Vehicle_ArmorForLocation_Dirty // dirty skip playimpact on despawned/dead actor?
        {
            static bool Prepare() => false;
            public static void Prefix(Vehicle __instance, ref int loc)
            {
                switch (loc)
                {
                    case 32: loc = 8;
                        break;
                    case 64: loc = 4;
                        break;
                    case 128: loc = 8;
                        break;
                    case 256: loc = 16;
                        break;
                    case 512: loc = 16;
                        break;
                    case 1024: loc = 16;
                        break;
                }
            }
        }
    }
}
