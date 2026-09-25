using Dawn;
using Dawn.Interfaces;
using GameNetcodeStuff;
using LethalDiseases;
using LethalDiseases.Items;
using Newtonsoft.Json.Linq;
using SnowyCraftingCore;
using SnowyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;
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
        itemProperties.positionOffset = new Vector3(-0.04f, 0.26f, 0.07f);
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

    private void FireDartEffects()
    {
        loadedAmmo?.UseAmmo();
        animator.SetTrigger("fire");
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
        FireDartEffects();

        GameObject dartObj = Instantiate(dartPrefab, position, rotation);
        Destroy(dartObj, DartLifetime);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void SpawnDartProjectileRpc(ulong clientId, Vector3 position, Quaternion rotation)
    {
        FireDartEffects();

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
        FireDartEffects();

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
        FireDartEffects();
    }
}

internal class DartGunAmmo : PhysicsProp, IDawnSaveData
{
    public static bool IsEnabled => LethalContent.Items[LethalDiseasesKeys.DartGunAmmo] != null;

    public static List<string> spawningStoredDiseases = new List<string>();

    public MeshRenderer fluidRenderer = null!;


    ScanNodeProperties scanNode = null!;

    [HideInInspector]
    public string storedDisease = "";

    public int AmmoLeft { get; private set; } = 0;
    public bool IsFilled => AmmoLeft > 0;

    public static int MaxDartsInAmmo => PluginInstance.Config.Bind("Dart Gun Ammo Options", "Dart Gun Ammo | Max Darts", 10, "The max amount of darts in a clip").Value;

    public void Awake()
    {
        scanNode = gameObject.GetComponentInChildren<ScanNodeProperties>();
    }

    [StaticInit]
    public static void InitConfigs()
    {
        _ = MaxDartsInAmmo;
    }

    public override void Start()
    {
        base.Start();

        if (spawningStoredDiseases.Count > 0)
        {
            AmmoLeft = MaxDartsInAmmo;
            SetDisease(spawningStoredDiseases.First());
            spawningStoredDiseases.RemoveAt(0);
        }
    }

    public void SetFluidColor(ChemistryLiquidAppearance color)
    {
        fluidRenderer.material.color = color.liquidColor;
        fluidRenderer.material.SetColor("_EmissiveColor", color.liquidColor);
        fluidRenderer.material.SetFloat("_EmissiveIntensity", color.emissionIntensity);
    }

    public void UseAmmo()
    {
        AmmoLeft--;
    }

    public void SetDisease(string diseaseId)
    {
        Disease? disease = Disease.GetDiseaseFromString(diseaseId);
        if (disease == null) { logger.LogError($"Failed to parse disease from string: {diseaseId}"); return; }

        storedDisease = diseaseId;
        SetFluidColor(disease.GetChemistryLiquidAppearance());
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void SetDiseaseRpc(string diseaseId)
    {
        SetDisease(diseaseId);
    }

    public JToken GetDawnDataToSave()
    {
        string data = $"{AmmoLeft}/{storedDisease}";
        return JToken.FromObject(data);
    }

    public void LoadDawnSaveData(JToken saveData)
    {
        string data = saveData.Value<string>()!;

        if (string.IsNullOrWhiteSpace(data)) { return; }

        string[] args = data.Split("/");

        AmmoLeft = int.Parse(args[0]);
        if (args.Length == 1) { return; } // TODO: Test this

        SetDisease(args[1]);
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
