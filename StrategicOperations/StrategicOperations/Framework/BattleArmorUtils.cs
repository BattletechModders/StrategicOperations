using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using UnityEngine;

namespace StrategicOperations.Framework
{
    public static class BattleArmorUtils
    {
        public static bool HasMountedUnits(this AbstractActor actor)
        {
            return ModState.PositionLockMount.ContainsValue(actor.GUID);
        }
        public static bool IsMountedUnit(this AbstractActor actor)
        {
            return ModState.PositionLockMount.ContainsKey(actor.GUID);
        }

        public static bool HasSwarmingUnits(this AbstractActor actor)
        {
            return ModState.PositionLockSwarm.ContainsValue(actor.GUID);
        }
        public static bool IsSwarmingUnit(this AbstractActor actor)
        {
            return ModState.PositionLockSwarm.ContainsKey(actor.GUID);
        }

        public static Vector3 FetchAdjacentHex(AbstractActor actor)
        {
            var points = actor.Combat.HexGrid.GetAdjacentPointsOnGrid(actor.CurrentPosition);
            var point = points.GetRandomElement();
            point.y = actor.Combat.MapMetaData.GetLerpedHeightAt(point, false);
            return point;
        }

    }
}
