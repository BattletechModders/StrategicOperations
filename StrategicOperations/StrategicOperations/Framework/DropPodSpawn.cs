using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using UnityEngine;

namespace StrategicOperations.Framework
{
    class UnitDropPodSpawner : UnitSpawnPointGameLogic
    {
 //       private ParticleSystem dropPodVfxPrefab;
 //       private GameObject dropPodLandedPrefab;
 //       private readonly Vector3 offscreenDropPodPosition = new Vector3(10000f, 10000f, 10000f);

        private static UnitDropPodSpawner _instance;

        public static UnitDropPodSpawner UnitDropPodSpawnerInstance
        {
            get
            {
                if (_instance == null) _instance = new UnitDropPodSpawner();
                return _instance;
            }
        }
    }

    class LanceDropPodSpawner : LanceSpawnerGameLogic
    {
        //       private ParticleSystem dropPodVfxPrefab;
        //       private GameObject dropPodLandedPrefab;
        //       private readonly Vector3 offscreenDropPodPosition = new Vector3(10000f, 10000f, 10000f);

        private static LanceDropPodSpawner _instance;

        public static LanceDropPodSpawner LanceDropPodSpawnerInstance
        {
            get
            {
                if (_instance == null) _instance = new LanceDropPodSpawner();
                return _instance;
            }
        }
    }
}
