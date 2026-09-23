using Dawn;
using GameNetcodeStuff;
using SnowyCraftingCore;
using SnowyLib;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items
{
    internal class SyringeGunBehavior : PhysicsProp
    {
        public static bool IsEnabled => LethalContent.Items[LethalDiseasesKeys.SyringeGun] != null;

        public Animator animator = null!;
        public AudioSource audioSource = null!;
        public SkinnedMeshRenderer syringeFluidRenderer = null!;
        public GameObject syringeProjectilePrefab = null!;
        public AudioClip reloadSFX = null!;
        public AudioClip fireSFX = null!;
        public AudioClip clickSFX = null!;

        public bool IsLoaded => !string.IsNullOrWhiteSpace(storedDisease);

        int mask;

        bool hitSomething;
        RaycastHit hit;

        public string storedDisease = "";
        public static float maxDistance = 400f;

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(-0.04f, 0.26f, 0.07f);
            itemProperties.rotationOffset = new Vector3(10, -90, 90);
            itemProperties.floorYOffset = 0;
            mask = LayerMask.GetMask("Player", "Enemies", "Room", "Terrain", "Colliders");
        }

        public void SetFluidColor(ChemistryLiquidAppearance color)
        {
            syringeFluidRenderer.material.color = color.liquidColor;
            syringeFluidRenderer.material.SetColor("_EmissiveColor", color.liquidColor);
            syringeFluidRenderer.material.SetFloat("_EmissiveIntensity", color.emissionIntensity);
        }

        public override void EquipItem()
        {
            base.EquipItem();
            playerHeldBy.equippedUsableItemQE = true;
        }

        public override void DiscardItem()
        {
            playerHeldBy.equippedUsableItemQE = false;
            base.DiscardItem();
        }

        public override void PocketItem()
        {
            playerHeldBy.equippedUsableItemQE = false;
            base.PocketItem();
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!buttonDown) { return; }
            Fire();
        }

        public override void ItemInteractLeftRight(bool right)
        {
            base.ItemInteractLeftRight(right);
            if (!right) { return; }
            Reload();
        }

        public void Fire()
        {
            if (!IsLoaded)
            {
                audioSource.PlayOneShot(clickSFX);
                return;
            }

            // scp4666 knife
            GetEndPoint();

            if (hitSomething)
            {
                Vector3 position = hit.point;
                Quaternion rotation = Quaternion.LookRotation(localPlayer.gameplayCamera.transform.forward);

                if (hit.collider.CompareTag("Player"))
                {
                    PlayerControllerB player = hit.collider.gameObject.GetComponent<PlayerControllerB>();
                    SpawnSyringeProjectileRpc(player.actualClientId, position, rotation);
                }
                else if (hit.collider.CompareTag("Enemy"))
                {
                    EnemyAI enemy = hit.collider.gameObject.GetComponent<EnemyAICollisionDetect>().mainScript;
                    SpawnSyringeProjectileRpc(enemy.NetworkObject, position, rotation);
                }
                else
                {
                    SpawnSyringeProjectileRpc(position, rotation);
                }
            }
            else
            {
                SpawnSyringeRpc(hit.point);
            }
        }

        void GetEndPoint()
        {
            hitSomething = false;
            Ray ray = new Ray(playerHeldBy.gameplayCamera.transform.position + playerHeldBy.gameplayCamera.transform.forward, playerHeldBy.gameplayCamera.transform.forward);
            if (Physics.Raycast(ray, out hit, maxDistance, mask))
            {
                hitSomething = true;
                return;
            }

            logger.LogDebug("Couldnt find hittable surface");
            hit.point = ray.GetPoint(maxDistance);
        }

        public void Reload()
        {
            int ammoSlot = FindAmmoInInventory();
            if (ammoSlot == -1)
            {
                audioSource.PlayOneShot(clickSFX);
                return;
            }

            string diseaseId = ((SyringeBehavior)localPlayer.ItemSlots[ammoSlot]).storedDisease;
            localPlayer.DestroyItemInSlotAndSync(ammoSlot);
            ReloadRpc(diseaseId);
        }

        private int FindAmmoInInventory()
        {
            for (int i = 0; i < playerHeldBy.ItemSlots.Length; i++)
            {
                if (!(playerHeldBy.ItemSlots[i] == null))
                {
                    SyringeBehavior? syringe = playerHeldBy.ItemSlots[i] as SyringeBehavior;
                    if (syringe != null && syringe.isFilled)
                    {
                        return i;
                    }
                }
            }
            if (playerHeldBy.ItemOnlySlot != null)
            {
                SyringeBehavior? syringe = playerHeldBy.ItemOnlySlot as SyringeBehavior;
                if (syringe != null && syringe.isFilled)
                {
                    return 50;
                }
            }
            return -1;
        }



        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void ReloadRpc(string diseaseId)
        {
            Disease? disease = Disease.GetDiseaseFromString(diseaseId);
            if (disease == null) { logger.LogError("Failed to parse disease from disease id"); return; }
            storedDisease = diseaseId;
            SetFluidColor(disease.GetChemistryLiquidAppearance());
            animator.SetTrigger("reload");
            audioSource.PlayOneShot(reloadSFX);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void SpawnSyringeRpc(Vector3 position)
        {
            animator.SetTrigger("fire");
            audioSource.PlayOneShot(fireSFX);

            string diseaseId = storedDisease;
            storedDisease = "";

            if (!IsServer) { return; }
            SyringeBehavior? syringe = Utils.SpawnItem(LethalDiseasesKeys.Syringe, position) as SyringeBehavior;
            if (syringe == null) { logger.LogError($"Failed to spawn syringe at {position}"); return; }

            IEnumerator setDiseaseAfterNetworkSpawn()
            {
                yield return new WaitUntil(() => syringe.NetworkObject != null && syringe.NetworkObject.IsSpawned);
                syringe.SetDiseaseRpc(diseaseId, false);
            }

            StartCoroutine(setDiseaseAfterNetworkSpawn());
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void SpawnSyringeProjectileRpc(Vector3 position, Quaternion rotation)
        {
            animator.SetTrigger("fire");
            audioSource.PlayOneShot(fireSFX);

            string diseaseId = storedDisease;
            storedDisease = "";

            if (!IsServer) { return; }
            GameObject syringeObj = Instantiate(syringeProjectilePrefab, position, rotation);
            syringeObj.GetComponent<NetworkObject>().Spawn(destroyWithScene: true);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void SpawnSyringeProjectileRpc(ulong clientId, Vector3 position, Quaternion rotation)
        {
            animator.SetTrigger("fire");
            audioSource.PlayOneShot(fireSFX);

            string diseaseId = storedDisease;
            storedDisease = "";

            PlayerControllerB? player = PlayerFromId(clientId);
            if (player == null) { logger.LogError("Failed to get player from player client id"); return; }

            if (localPlayer == player)
            {
                if (localPlayer.health != 1)
                {
                    localPlayer.inSpecialInteractAnimation = true;
                    localPlayer.DamagePlayer(1, hasDamageSFX: false, causeOfDeath: CauseOfDeath.Stabbing);
                    localPlayer.inSpecialInteractAnimation = false;
                }
            }

            if (!IsServer) { return; }
            GameObject syringeObj = Instantiate(syringeProjectilePrefab, position, rotation);
            var syringe = syringeObj.GetComponent<SyringeProjectile>();
            syringeObj.GetComponent<NetworkObject>().Spawn(destroyWithScene: true);

            IEnumerator parentSyringeProjectile()
            {
                yield return new WaitUntil(() => syringe.NetworkObject != null && syringe.NetworkObject.IsSpawned);
                syringe.NetworkObject.TrySetParent(player.NetworkObject, worldPositionStays: true);
            }

            StartCoroutine(parentSyringeProjectile());
            player.Infect(diseaseId);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void SpawnSyringeProjectileRpc(NetworkObjectReference enemyNetRef, Vector3 position, Quaternion rotation)
        {
            animator.SetTrigger("fire");
            audioSource.PlayOneShot(fireSFX);

            string diseaseId = storedDisease;
            storedDisease = "";

            if (!IsServer) { return; }
            if (!enemyNetRef.TryGet(out NetworkObject netObj)) { logger.LogError("Failed to get networkobject from networkobjectreference"); return; }
            EnemyAI enemy = netObj.GetComponent<EnemyAI>();

            GameObject syringeObj = Instantiate(syringeProjectilePrefab, position, rotation);
            var syringe = syringeObj.GetComponent<SyringeProjectile>();
            syringeObj.GetComponent<NetworkObject>().Spawn(destroyWithScene: true);

            IEnumerator parentSyringeProjectile()
            {
                yield return new WaitUntil(() => syringe.NetworkObject != null && syringe.NetworkObject.IsSpawned);
                syringe.NetworkObject.TrySetParent(enemy.NetworkObject, worldPositionStays: true);
            }

            StartCoroutine(parentSyringeProjectile());
            enemy.Infect(diseaseId);
        }
    }

    public class SyringeProjectile : NetworkBehaviour
    {
        public void OnTriggerInteract() // InteractTrigger
        {
            localPlayer.SpawnAndGrabItem(LethalDiseasesKeys.Syringe);
            DespawnRpc();
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        public void DespawnRpc()
        {
            NetworkObject.Despawn(destroy: true);
        }
    }
}