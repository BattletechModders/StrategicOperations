using System;
using System.Collections;
using BattleTech;
using BattleTech.Rendering.UrbanWarfare;
using BattleTech.UI;
using UnityEngine;

namespace StrategicOperations.Framework
{
    public class DropPodUtils
    {
        [HarmonyPatch(typeof(CombatHUD))]
        [HarmonyPatch("Init")]
        [HarmonyPatch(MethodType.Normal)]
        [HarmonyPatch(new Type[] { typeof(CombatGameState) })]
        public static class CombatHUD_Init_Hotdrop
        {
            public static void Postfix(CombatHUD __instance, CombatGameState Combat)
            {
                EncounterLayerParent encounterLayerParent = Combat.EncounterLayerData.gameObject.GetComponentInParent<EncounterLayerParent>();
                DropPodSpawner dropSpawner = encounterLayerParent.gameObject.GetComponent<DropPodSpawner>();
                if (dropSpawner == null) { dropSpawner = encounterLayerParent.gameObject.AddComponent<DropPodSpawner>(); }
            }
        }


        public class DropPodSpawner: MonoBehaviour
        {
            //public string SpawnGUID { get; set; }
            //public Action OnDropComplete { get; set; }
            public CombatGameState Combat;

            public Vector3 DropPodPosition;
            public Quaternion DropPodRotation;

            public GameObject DropPodLandedPrefab
            {
                get;
                set;
            }

            public ParticleSystem DropPodVfxPrefab
            {
                get;
                set;
            }

            public bool DropProcessing { get; set; } = false;
            public Vector3 OffscreenDropPodPosition { get; set; } = Vector3.zero;
            public EncounterLayerParent Parent { get; set; } = null;
            public AbstractActor Unit { get; set; } = null;

            public IEnumerator DestroyFlimsyObjects(Vector3 position)
            {
                Collider[] hits = Physics.OverlapSphere(position, 36f, -5, QueryTriggerInteraction.Ignore);
                float impactMagnitude = 3f * Combat.Constants.ResolutionConstants.FlimsyDestructionForceMultiplier;
                for (int i = 0; i < hits.Length; ++i)
                {
                    Collider collider = hits[i];
                    Vector3 normalized = (collider.transform.position - position).normalized;
                    DestructibleObject component1 = collider.gameObject.GetComponent<DestructibleObject>();
                    DestructibleUrbanFlimsy component2 = collider.gameObject.GetComponent<DestructibleUrbanFlimsy>();
                    if (component1 != null && component1.isFlimsy)
                    {
                        component1.TakeDamage(position, normalized, impactMagnitude);
                        component1.Collapse(normalized, impactMagnitude);
                    }
                    if ((UnityEngine.Object)component2 != (UnityEngine.Object)null)
                        component2.PlayDestruction(normalized, impactMagnitude);
                    if (i % 10 == 0)
                        yield return (object)null;
                }
                yield return (object)null;
            }

            public void LoadDropPodPrefabs(ParticleSystem dropPodVfxPrefab, GameObject dropPodLandedPrefab)
            {
                if (dropPodVfxPrefab != null)
                {
                    this.DropPodVfxPrefab = UnityEngine.Object.Instantiate<ParticleSystem>(dropPodVfxPrefab);
                    ModInit.modLog?.Trace?.Write($"instantiated prefabs");
                    this.DropPodVfxPrefab.transform.position = DropPodPosition;
                    ModInit.modLog?.Trace?.Write($"set position");
                    this.DropPodVfxPrefab.Pause();
                    this.DropPodVfxPrefab.Clear();
                }
                if (dropPodLandedPrefab != null)
                {
                    this.DropPodLandedPrefab = UnityEngine.Object.Instantiate<GameObject>(dropPodLandedPrefab, this.OffscreenDropPodPosition, Quaternion.identity);
                }
                ModInit.modLog?.Trace?.Write($"finished load drop prefabs");
            }

            public IEnumerator StartDropPodAnimation(float initialDelay)//, Action unitDropPodAnimationComplete)
            {
                while (!EncounterLayerParent.encounterBegan)
                    yield return (object) null;
                yield return (object) new WaitForSeconds(0.5f + initialDelay);
                int num1 = (int) WwiseManager.PostEvent<AudioEventList_play>(
                    AudioEventList_play.play_dropPod_projectile,
                    WwiseManager.GlobalAudioObject);
                if (this.DropPodVfxPrefab != null)
                {
                    this.DropPodVfxPrefab.transform.position = DropPodPosition;
                    this.DropPodVfxPrefab.Simulate(0.0f);
                    this.DropPodVfxPrefab.Play();
                    ModInit.modLog?.Trace?.Write($"playing droppod anim");
                }
                else
                {
                    ModInit.modLog?.Trace?.Write($"No Drop pod anim for biome");
                }

                yield return (object) new WaitForSeconds(1f);
                int num2 = (int) WwiseManager.PostEvent<AudioEventList_play>(AudioEventList_play.play_dropPod_impact,
                    WwiseManager.GlobalAudioObject);
                yield return (object) this.DestroyFlimsyObjects(DropPodPosition);
                //yield return (object)this.ApplyDropPodDamageToSquashedUnits(sequenceGUID, rootSequenceGUID);
                yield return (object) new WaitForSeconds(3f);
                this.TeleportUnitToSpawnPoint();
                yield return (object) new WaitForSeconds(2f);
                this.DropProcessing = false;
                //Combat.MessageCenter.PublishMessage((MessageCenterMessage) new AddSequenceToStackMessage(this.Unit.DoneWithActor()));
                //unitDropPodAnimationComplete();
                ModInit.modLog?.Trace?.Write($"finish droppod anim");
                Utils.DeployEvasion(this.Unit);
            }

            public void TeleportUnitToSpawnPoint()
            {
                if (this.DropPodLandedPrefab != null)
                {
                    this.DropPodLandedPrefab.transform.position = DropPodPosition;
                    this.DropPodLandedPrefab.transform.rotation = DropPodRotation;
                }
                this.Unit.TeleportActor(DropPodPosition);
                ModInit.modLog?.Trace?.Write($"teleported actor to {DropPodPosition}");
                this.Unit.GameRep.FadeIn(1f);
                this.Unit.OnPlayerVisibilityChanged(VisibilityLevel.LOSFull);
            }
        }
    }
}
