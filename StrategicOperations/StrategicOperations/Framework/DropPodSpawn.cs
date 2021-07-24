using BattleTech;

namespace StrategicOperations.Framework
{
    public class UnitDropPodSpawner : UnitSpawnPointGameLogic
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

   public class LanceDropPodSpawner : LanceSpawnerGameLogic
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
