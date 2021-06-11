using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;

namespace StrategicOperations.Framework
{
    public static class ModState
    {
        public static List<Ability> CommandAbilities = new List<Ability>();

        public static Action deferredInvokeSpawn;

        public static void ResetAll()
        {
            CommandAbilities.Clear();
            deferredInvokeSpawn = null;
        }

        public static void ResetDeferredSpawner()
        {
            deferredInvokeSpawn = null;
        }
    }

}
