using Dawn;
using GameNetcodeStuff;
using SnowyCraftingCore;
using SnowyLib;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items
{
    internal class SyringeGun : DiseaseStorable
    {
        public static bool IsEnabled => LethalContent.Items[LethalDiseasesKeys.SyringeGun] != null;
        
        public GameObject syringeProjectilePrefab = null!;
        public AudioClip reloadSFX = null!;
        public AudioClip fireSFX = null!;
        public AudioClip clickSFX = null!;

        Animator animator = null!;
        AudioSource audioSource = null!;

        int mask;

        Ray ray => new Ray(playerHeldBy.gameplayCamera.transform.position + playerHeldBy.gameplayCamera.transform.forward, playerHeldBy.gameplayCamera.transform.forward);
        RaycastHit hit;
        public static float maxDistance = 400f;

        public override void Awake()
        {
            base.Awake();
            mask = LayerMask.GetMask("Player", "Enemies", "Room", "Terrain", "Colliders");
            animator = GetComponent<Animator>();
            audioSource = GetComponent<AudioSource>();
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
            if (!IsFilled)
            {
                audioSource.PlayOneShot(clickSFX);
                return;
            }

            bool hitSomething = false;
            if (Physics.Raycast(ray, out hit, maxDistance, mask))
            {
                hitSomething = true;
            }
            else
            {
                hit.point = ray.GetPoint(maxDistance);
            }

            if (hitSomething)
            {
                Vector3 position = hit.point;
                Quaternion rotation = Quaternion.LookRotation(localPlayer.gameplayCamera.transform.forward);

                if (hit.collider.CompareTag("Player"))
                {
                    PlayerControllerB player = hit.collider.gameObject.GetComponent<PlayerControllerB>();

                    if (!player.thisPlayerModel.TryGetMeshHit(ray, out hit, maxDistance))
                    {
                        SpawnSyringeProjectileRpc(player.actualClientId, position, rotation);
                        return;
                    }

                    var boneHit = player.thisPlayerModel.GetDominantBone(hit);
                    if (boneHit == null)
                    {
                        boneHit = player.bodyParts.GetClosestToPosition(hit.point, (x) => x.position);
                    }

                    string boneHitPath = Utils.GetTransformPath(boneHit, player.transform);
                    SpawnSyringeProjectileRpc(player.actualClientId, hit.point, rotation, boneHitPath);
                }
                else if (hit.collider.CompareTag("Enemy"))
                {
                    EnemyAI enemy = hit.collider.gameObject.GetComponent<EnemyAICollisionDetect>().mainScript;

                    SkinnedMeshRenderer? rendererHit = null;

                    foreach (var skinnedRenderer in enemy.skinnedMeshRenderers)
                    {
                        if (skinnedRenderer.TryGetMeshHit(ray, out hit, maxDistance))
                        {
                            rendererHit = skinnedRenderer;
                            break;
                        }
                    }

                    if (rendererHit == null)
                    {
                        SpawnSyringeProjectileRpc(enemy.gameObject.GetComponent<NetworkObject>(), position, rotation);
                        return;
                    }

                    var boneHit = rendererHit.GetDominantBone(hit);
                    if (boneHit == null)
                    {
                        SpawnSyringeProjectileRpc(enemy.gameObject.GetComponent<NetworkObject>(), position, rotation);
                        return;
                    }

                    string boneHitPath = Utils.GetTransformPath(boneHit, enemy.transform);
                    SpawnSyringeProjectileRpc(enemy.gameObject.GetComponent<NetworkObject>(), hit.point, rotation, boneHitPath);
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

        public SkinnedMeshRenderer? TryGetEnemyMeshHit(EnemyAI enemy)
        {
            foreach (var skinnedRenderer in enemy.skinnedMeshRenderers)
            {
                if (skinnedRenderer.TryGetMeshHit(ray, out hit, maxDistance))
                {
                    return skinnedRenderer;
                }
            }

            return null;
        }

        public void Reload()
        {
            int ammoSlot = FindAmmoInInventory();
            if (ammoSlot == -1)
            {
                audioSource.PlayOneShot(clickSFX);
                return;
            }

            Syringe syringe = (Syringe)localPlayer.ItemSlots[ammoSlot];
            string diseaseString = syringe.StoredFluid;
            var color = syringe.FluidColor;
            localPlayer.DestroyItemInSlotAndSync(ammoSlot);
            FillRpc(diseaseString, color, true);
        }

        private int FindAmmoInInventory()
        {
            for (int i = 0; i < playerHeldBy.ItemSlots.Length; i++)
            {
                if (!(playerHeldBy.ItemSlots[i] == null))
                {
                    Syringe? syringe = playerHeldBy.ItemSlots[i] as Syringe;
                    if (syringe != null && syringe.IsFilled)
                    {
                        return i;
                    }
                }
            }
            if (playerHeldBy.ItemOnlySlot != null)
            {
                Syringe? syringe = playerHeldBy.ItemOnlySlot as Syringe;
                if (syringe != null && syringe.IsFilled)
                {
                    return 50;
                }
            }
            return -1;
        }

        public override void Fill(string fluidString, ChemistryLiquidAppearance color, bool doAnimation = false)
        {
            base.Fill(fluidString, color, doAnimation);
            audioSource.PlayOneShot(reloadSFX);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void SpawnSyringeRpc(Vector3 position)
        {
            animator.SetTrigger("fire");
            audioSource.PlayOneShot(fireSFX);

            if (!IsServer) { return; }
            string diseaseString = StoredFluid;
            var color = FluidColor;
            Utils.SpawnItem(LethalDiseasesKeys.Syringe, position, actionAfterSpawn: (x) => ((Syringe)x).FillRpc(diseaseString, color));
            EmptyRpc();
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void SpawnSyringeProjectileRpc(Vector3 position, Quaternion rotation)
        {
            animator.SetTrigger("fire");
            audioSource.PlayOneShot(fireSFX);

            if (!IsServer) { return; }
            GameObject syringeObj = Instantiate(syringeProjectilePrefab, position, rotation);
            syringeObj.GetComponent<NetworkObject>().Spawn(destroyWithScene: true);
            EmptyRpc();
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void SpawnSyringeProjectileRpc(ulong clientId, Vector3 position, Quaternion rotation, string boneHitPath = "")
        {
            animator.SetTrigger("fire");
            audioSource.PlayOneShot(fireSFX);

            PlayerControllerB player = PlayerFromId(clientId);

            if (localPlayer == player)
            {
                localPlayer.inSpecialInteractAnimation = true;
                localPlayer.DamagePlayer(localPlayer.health > 1 ? 1 : 0, hasDamageSFX: false, causeOfDeath: CauseOfDeath.Stabbing);
                localPlayer.inSpecialInteractAnimation = false;
            }

            if (!IsServer) { return; }
            GameObject syringeObj = Instantiate(syringeProjectilePrefab, position, rotation);

            if (!string.IsNullOrWhiteSpace(boneHitPath))
            {
                syringeObj.GetComponent<NetworkObject>().Spawn(destroyWithScene: true, actionAfterSpawn: (x) => ParentSyringeToBoneRpc(x, clientId, boneHitPath)); // TODO: Test this
            }
            else
            {
                syringeObj.GetComponent<NetworkObject>().Spawn(destroyWithScene: true, actionAfterSpawn: (x) => x.TrySetParent(player.gameObject.GetComponent<NetworkObject>(), worldPositionStays: true)); // TODO: Test this
            }

            player.InfectSynced(StoredFluid);
            EmptyRpc();
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void ParentSyringeToBoneRpc(NetworkObjectReference syringeNetRef, ulong clientId, string boneHitPath)
        {
            if (!syringeNetRef.TryGet(out NetworkObject syringeNetObj)) { logger.LogError("ParentSyringeToBoneRpc: Failed to get networkobject from networkobjectreference"); return; }
            PlayerControllerB player = PlayerFromId(clientId);
            Transform bone = player.transform.Find(boneHitPath);
            syringeNetObj.transform.SetParent(bone, worldPositionStays: true);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void SpawnSyringeProjectileRpc(NetworkObjectReference enemyNetRef, Vector3 position, Quaternion rotation, string boneHitPath = "")
        {
            animator.SetTrigger("fire");
            audioSource.PlayOneShot(fireSFX);

            if (!IsServer) { return; }
            if (!enemyNetRef.TryGet(out NetworkObject netObj)) { logger.LogError("Failed to get networkobject from networkobjectreference"); return; }
            EnemyAI enemy = netObj.GetComponent<EnemyAI>();

            enemy.HitEnemyServerRpc(0, (int)playerHeldBy.actualClientId, false);

            GameObject syringeObj = Instantiate(syringeProjectilePrefab, position, rotation);

            if (!string.IsNullOrWhiteSpace(boneHitPath))
            {
                syringeObj.GetComponent<NetworkObject>().Spawn(destroyWithScene: true, actionAfterSpawn: (x) => ParentSyringeToBoneRpc(x, enemyNetRef, boneHitPath)); // TODO: Test this
            }
            else
            {
                syringeObj.GetComponent<NetworkObject>().Spawn(destroyWithScene: true, actionAfterSpawn: (x) => x.TrySetParent(enemy.gameObject.GetComponent<NetworkObject>(), worldPositionStays: true)); // TODO: Test this
            }

            enemy.InfectSynced(StoredFluid);
            EmptyRpc();
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void ParentSyringeToBoneRpc(NetworkObjectReference syringeNetRef, NetworkObjectReference enemyNetRef, string boneHitPath)
        {
            if (!syringeNetRef.TryGet(out NetworkObject syringeNetObj)) { logger.LogError("ParentSyringeToBoneRpc: Failed to get networkobject from networkobjectreference"); return; }
            if (!enemyNetRef.TryGet(out NetworkObject enemyNetObj)) { logger.LogError("ParentSyringeToBoneRpc: Failed to get networkobject from networkobjectreference"); return; }
            Transform bone = enemyNetObj.transform.Find(boneHitPath);
            syringeNetObj.transform.SetParent(bone, worldPositionStays: true);
        }

        public override bool AllowChemistryOutputContainer()
        {
            return false;
        }
    }

    public class SyringeProjectile : NetworkBehaviour // TODO: Test this on player
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