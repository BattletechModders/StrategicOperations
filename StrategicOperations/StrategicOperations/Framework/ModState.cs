﻿using System;
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

        public static List<Action> deferredInvokeSpawns = new List<Action>();

        public static void ResetAll()
        {
            CommandAbilities.Clear();
            deferredInvokeSpawns = new List<Action>();
        }

        public static void ResetDeferredSpawners()
        {
            deferredInvokeSpawns = new List<Action>();
        }
    }

}
