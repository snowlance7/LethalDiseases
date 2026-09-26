using Dawn;
using GameNetcodeStuff;
using SnowyLib;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;

// give darts rigidbody, if it hits against a wall richochet and land on the ground and destroy self in a minute, if hits player, stick into player for a few seconds and then fall out. magazines are main ammo you have to buy seperately.

namespace LethalDiseases.Items;

internal class DartGunBehavior : PhysicsProp
{
    public static bool IsEnabled => LethalContent.Items[LethalDiseasesKeys.DartGun] != null;

    [SerializeField] GameObject dartPrefab = null!;
    [SerializeField] AudioClip loadSFX = null!;
    [SerializeField] AudioClip unloadSFX = null!;
    [SerializeField] AudioClip fireSFX = null!;
    [SerializeField] AudioClip clickSFX = null!;
    [SerializeField] MeshRenderer clipRenderer = null!; // TODO: Set this and use for enableitemmeshes

    AudioSource audioSource = null!;
    Animator animator = null!;

    public DartGunAmmo? loadedAmmo;

    int mask;

    bool hitSomething;
    RaycastHit hit;

    public static float MaxRange => PluginInstance.Config.Bind("Dart Gun Options", "Dart Gun | Max Range", 500f, "The max distance the dart gun can shoot darts").Value;
    public static float DartLifetime => PluginInstance.Config.Bind("Dart Gun Options", "Dart Gun | Dart Lifetime", 60f, "How long it takes for dart gun darts to despawn after being shot").Value;

    [StaticInit]
    public static void InitConfigs()
    {
        _ = MaxRange;
        _ = DartLifetime;
    }

    public void Awake()
    {
        itemProperties.positionOffset = new Vector3(0f, 0.3f, 0.05f);
        itemProperties.rotationOffset = new Vector3(10, -90, 90);
        itemProperties.floorYOffset = 0;

        itemProperties.toolTips = ["Fire [LMB]", "Reload [E]", "Unload [Q]"];
        mask = LayerMask.GetMask("Player", "Enemies", "Room", "Terrain", "Colliders");
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    public override void Update()
    {
        base.Update();

        if (loadedAmmo != null)
        {
            loadedAmmo.transform.position = transform.position;
        }
    }

    public override void SetControlTipsForItem()
    {
        HUDManager.Instance.ChangeControlTipMultiple([$"Fire [LMB]{(loadedAmmo != null ? $"[{loadedAmmo.AmmoLeft} left]" : "")}", "Reload [E]", "Unload [Q]"], holdingItem: playerHeldBy != null && playerHeldBy == localPlayer && !isPocketed, itemProperties);
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

        if (loadedAmmo == null || loadedAmmo.AmmoLeft <= 0)
        {
            audioSource.PlayOneShot(clickSFX);
            return;
        }

        GetEndPoint();

        if (hitSomething)
        {
            Vector3 position = hit.point;
            Quaternion rotation = Quaternion.LookRotation(localPlayer.gameplayCamera.transform.forward);

            if (hit.collider.CompareTag("Player"))
            {
                PlayerControllerB player = hit.collider.gameObject.GetComponent<PlayerControllerB>();
                SpawnDartProjectileRpc(player.actualClientId, position, rotation);
            }
            else if (hit.collider.CompareTag("Enemy"))
            {
                EnemyAI enemy = hit.collider.gameObject.GetComponent<EnemyAICollisionDetect>().mainScript;
                SpawnDartProjectileRpc(enemy.NetworkObject, position, rotation);
            }
            else
            {
                SpawnDartProjectileRpc(position, rotation);
            }
        }
        else
        {
            FireDartEffectsRpc();
        }
    }

    public override void ItemInteractLeftRight(bool right)
    {
        base.ItemInteractLeftRight(right);

        if (right) // Reload [E]
        {
            if (loadedAmmo != null)
            {
                if (loadedAmmo.AmmoLeft > 0) { return; }

                UnloadAmmoRpc();
            }

            int ammoSlot = FindAmmoInInventory();
            if (ammoSlot == -1)
            {
                audioSource.PlayOneShot(clickSFX);
                return;
            }

            var ammo = localPlayer.ItemSlots[ammoSlot];
            localPlayer.DiscardItemInSlot(ammoSlot);
            LoadAmmoRpc(ammo.NetworkObject);
        }
        else // Unload [Q]
        {
            UnloadAmmoRpc();
        }
    }

    void GetEndPoint()
    {
        hitSomething = false;
        Ray ray = new Ray(playerHeldBy.gameplayCamera.transform.position + playerHeldBy.gameplayCamera.transform.forward, playerHeldBy.gameplayCamera.transform.forward);
        if (Physics.Raycast(ray, out hit, MaxRange, mask))
        {
            hitSomething = true;
            return;
        }

        logger.LogDebug("Couldnt find hittable surface");
        hit.point = ray.GetPoint(MaxRange);
    }

    private int FindAmmoInInventory()
    {
        for (int i = 0; i < playerHeldBy.ItemSlots.Length; i++)
        {
            if (!(playerHeldBy.ItemSlots[i] == null))
            {
                DartGunAmmo? ammo = playerHeldBy.ItemSlots[i] as DartGunAmmo;
                if (ammo != null && ammo.IsFilled)
                {
                    return i;
                }
            }
        }
        return -1;
    }

    public override void EnableItemMeshes(bool enable)
    {
        base.EnableItemMeshes(enable);
        clipRenderer.enabled = enable && loadedAmmo != null;
    }

    private void FireDart()
    {
        loadedAmmo?.UseAmmo();
        //animator.SetTrigger("fire");
        audioSource.PlayOneShot(fireSFX);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    private void UnloadAmmoRpc() // TODO: Test this
    {
        if (loadedAmmo == null) { return; }

        loadedAmmo.transform.SetParent(null);
        loadedAmmo.parentObject = null;
        loadedAmmo.EnablePhysics(true);
        loadedAmmo.EnableItemMeshes(true);
        loadedAmmo.transform.position = transform.position;
        loadedAmmo.FallToGround();

        animator.SetTrigger("unload");
        audioSource.PlayOneShot(unloadSFX);
        loadedAmmo = null;
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    private void LoadAmmoRpc(NetworkObjectReference netRef) // TODO: Test this
    {
        if (loadedAmmo != null) { return; }
        if (!netRef.TryGet(out NetworkObject netObj)) { logger.LogError("LoadAmmoRpc: Failed to get networkobject from networkobjectreference"); return; }
        
        loadedAmmo = netObj.GetComponent<DartGunAmmo>();

        loadedAmmo.transform.position = transform.position;
        loadedAmmo.transform.SetParent(transform);
        loadedAmmo.parentObject = transform;
        loadedAmmo.EnablePhysics(false);
        loadedAmmo.EnableItemMeshes(false);

        animator.SetTrigger("load");
        audioSource.PlayOneShot(loadSFX);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void SpawnDartProjectileRpc(Vector3 position, Quaternion rotation)
    {
        FireDart();

        GameObject dartObj = Instantiate(dartPrefab, position, rotation);
        Destroy(dartObj, DartLifetime);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void SpawnDartProjectileRpc(ulong clientId, Vector3 position, Quaternion rotation)
    {
        FireDart();

        PlayerControllerB? player = PlayerFromId(clientId);
        if (player == null) { logger.LogError("Failed to get player from player client id"); return; }

        if (IsServer)
            player.Infect(loadedAmmo!.storedDisease);

        if (localPlayer == player)
        {
            if (localPlayer.health != 1)
            {
                localPlayer.inSpecialInteractAnimation = true;
                localPlayer.DamagePlayer(1, hasDamageSFX: false, causeOfDeath: CauseOfDeath.Stabbing);
                localPlayer.inSpecialInteractAnimation = false;
            }
        }

        GameObject dartObj = Instantiate(dartPrefab, position, rotation, player.transform);
        Destroy(dartObj, DartLifetime);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void SpawnDartProjectileRpc(NetworkObjectReference enemyNetRef, Vector3 position, Quaternion rotation)
    {
        FireDart();

        if (!enemyNetRef.TryGet(out NetworkObject netObj)) { logger.LogError("Failed to get networkobject from networkobjectreference"); return; }
        EnemyAI enemy = netObj.GetComponent<EnemyAI>();

        if (IsServer)
            enemy.Infect(loadedAmmo!.storedDisease);

        GameObject dartObj = Instantiate(dartPrefab, position, rotation, enemy.transform); // TODO: Test this
        Destroy(dartObj, DartLifetime);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void FireDartEffectsRpc()
    {
        FireDart();
    }
}

internal class DartProjectile : MonoBehaviour
{
    float stickTime = 5f;

    Rigidbody rigidbody = null!;

    public void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

    public void Update()
    {
        if (stickTime > 0f)
            stickTime -= Time.deltaTime;

        if (stickTime <= 0 && rigidbody.isKinematic)
        {
            transform.SetParent(null);
            rigidbody.isKinematic = false;
        }
    }
}
