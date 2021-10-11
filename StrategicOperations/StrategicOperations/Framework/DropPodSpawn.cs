using System;
using System.Collections;
using BattleTech;
using BattleTech.Rendering.UrbanWarfare;
using UnityEngine;

namespace StrategicOperations.Framework
{
    public class DropPodUtils
    {
        public class DropPodSpawn : MonoBehaviour
        {
            public AbstractActor Unit { get; set; } = null;
            public bool DropProcessing { get; set; } = false;

            public ParticleSystem DropPodVfxPrefab
            {
                get => EncounterLayerParent.Instance.DropPodVfxPrefab;
                set => throw new NotImplementedException();
            }

            public GameObject DropPodLandedPrefab
            {
                get => EncounterLayerParent.Instance.dropPodLandedPrefab;
                set => EncounterLayerParent.Instance.dropPodLandedPrefab = value;
            }

            public Vector3 OffscreenDropPodPosition { get; set; } = Vector3.zero;
            public string SpawnGUID { get; set; }
            public Action OnDropComplete { get; set; }
            public CombatGameState Combat;


            public void LoadDropPodPrefabs(ParticleSystem dropPodVfxPrefab, GameObject dropPodLandedPrefab)
            {
                if (dropPodVfxPrefab != null)
                {
                    this.DropPodVfxPrefab = UnityEngine.Object.Instantiate<ParticleSystem>(dropPodVfxPrefab, this.transform);
                    this.DropPodVfxPrefab.transform.position = this.transform.position;
                    this.DropPodVfxPrefab.Pause();
                    this.DropPodVfxPrefab.Clear();
                }
                if (dropPodLandedPrefab != null)
                {
                    this.DropPodLandedPrefab = UnityEngine.Object.Instantiate<GameObject>(dropPodLandedPrefab, this.OffscreenDropPodPosition, Quaternion.identity);
                }
            }
            public IEnumerator StartDropPodAnimation(AbstractActor actor, float initialDelay,
                Action unitDropPodAnimationComplete)
            {
                while (!EncounterLayerParent.encounterBegan)
                    yield return (object) null;
                yield return (object) new WaitForSeconds(UnityEngine.Random.Range(0.5f, 1.75f) + initialDelay);
                int num1 = (int) WwiseManager.PostEvent<AudioEventList_play>(
                    AudioEventList_play.play_dropPod_projectile,
                    WwiseManager.GlobalAudioObject);
                if (this.DropPodVfxPrefab != null)
                {
                    this.DropPodVfxPrefab.transform.position = this.transform.position;
                    this.DropPodVfxPrefab.Simulate(0.0f);
                    this.DropPodVfxPrefab.Play();
                }
                else
                {
                    //Log.TWL(0, "Null drop pod animation for this biome.");
                }

                yield return (object) new WaitForSeconds(1f);
                int num2 = (int) WwiseManager.PostEvent<AudioEventList_play>(AudioEventList_play.play_dropPod_impact,
                    WwiseManager.GlobalAudioObject);
                yield return (object) this.DestroyFlimsyObjects();
                //yield return (object)this.ApplyDropPodDamageToSquashedUnits(sequenceGUID, rootSequenceGUID);
                yield return (object) new WaitForSeconds(3f);
                this.TeleportUnitToSpawnPoint();
                yield return (object) new WaitForSeconds(2f);
                this.DropProcessing = false;
                Combat.MessageCenter.PublishMessage(
                    (MessageCenterMessage) new AddSequenceToStackMessage(this.Unit.DoneWithActor()));
                unitDropPodAnimationComplete();
            }

            public void TeleportUnitToSpawnPoint()
            {
                Vector3 spawnPosition = this.gameObject.transform.position;
                if (this.DropPodLandedPrefab != null)
                {
                    this.DropPodLandedPrefab.transform.position = spawnPosition;
                    this.DropPodLandedPrefab.transform.rotation = this.transform.rotation;
                }
                this.Unit.TeleportActor(spawnPosition);
                this.Unit.GameRep.FadeIn(1f);
                this.Unit.OnPlayerVisibilityChanged(VisibilityLevel.LOSFull);
            }

            public IEnumerator DestroyFlimsyObjects()
            {
                Collider[] hits = Physics.OverlapSphere(this.gameObject.transform.position, 36f, -5, QueryTriggerInteraction.Ignore);
                float impactMagnitude = 3f * Combat.Constants.ResolutionConstants.FlimsyDestructionForceMultiplier;
                for (int i = 0; i < hits.Length; ++i)
                {
                    Collider collider = hits[i];
                    Vector3 normalized = (collider.transform.position - this.gameObject.transform.position).normalized;
                    DestructibleObject component1 = collider.gameObject.GetComponent<DestructibleObject>();
                    DestructibleUrbanFlimsy component2 = collider.gameObject.GetComponent<DestructibleUrbanFlimsy>();
                    if (component1 != null && component1.isFlimsy)
                    {
                        component1.TakeDamage(this.gameObject.transform.position, normalized, impactMagnitude);
                        component1.Collapse(normalized, impactMagnitude);
                    }
                    if ((UnityEngine.Object)component2 != (UnityEngine.Object)null)
                        component2.PlayDestruction(normalized, impactMagnitude);
                    if (i % 10 == 0)
                        yield return (object)null;
                }
                yield return (object)null;
            }
        }
    }
}
