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

    public Animator animator = null!;
    public GameObject dartPrefab = null!;
    public AudioSource audioSource = null!;
    public AudioClip reloadSFX = null!;
    public AudioClip fireSFX = null!;
    public AudioClip clickSFX = null!;
    public Transform ammoDropPosition = null!;

    public bool IsLoaded => !string.IsNullOrWhiteSpace(storedDisease);

    int mask;

    bool hitSomething;
    RaycastHit hit;

    public string storedDisease = "";
    public static float maxDistance = 500f;

    public void Awake()
    {
        itemProperties.positionOffset = new Vector3(-0.04f, 0.26f, 0.07f);
        itemProperties.rotationOffset = new Vector3(10, -90, 90);
        itemProperties.floorYOffset = 0;
        mask = LayerMask.GetMask("Player", "Enemies", "Room", "Terrain", "Colliders");
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

        GetEndPoint();

        if (hitSomething)
        {
            Vector3 position = hit.point;
            Quaternion rotation = Quaternion.LookRotation(localPlayer.gameplayCamera.transform.forward);

            if (hit.collider.CompareTag("Player"))
            {
                PlayerControllerB player = hit.collider.gameObject.GetComponent<PlayerControllerB>();
                //SpawnSyringeProjectileRpc(player.actualClientId, position, rotation);
            }
            else if (hit.collider.CompareTag("Enemy"))
            {
                EnemyAI enemy = hit.collider.gameObject.GetComponent<EnemyAICollisionDetect>().mainScript;
                //SpawnSyringeProjectileRpc(enemy.NetworkObject, position, rotation);
            }
            else
            {
                //SpawnSyringeProjectileRpc(position, rotation);
            }
        }
        else
        {
            //SpawnSyringeRpc(hit.point);
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
}

internal class DartGunAmmo : GunAmmo, IDawnSaveData
{
    public static bool IsEnabled => LethalContent.Items[LethalDiseasesKeys.DartGunAmmo] != null;

    public static List<string> spawningStoredDiseases = new List<string>();

    public MeshRenderer fluidRenderer = null!;


    ScanNodeProperties scanNode = null!;

    [HideInInspector]
    public string storedDisease = "";

    public int ammoLeft = 0;

    public static int MaxDartsInAmmo => PluginInstance.Config.Bind("Dart Gun Ammo Options", "Dart Gun Ammo | Max Darts", 10, "The max amount of darts in a clip").Value;

    [StaticInit]
    public static void InitConfigs()
    {
        _ = MaxDartsInAmmo;
    }

    public override void Start()
    {
        base.Start();

        scanNode = gameObject.GetComponentInChildren<ScanNodeProperties>();

        if (spawningStoredDiseases.Count > 0)
        {
            ammoLeft = MaxDartsInAmmo;
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
        string data = $"{ammoLeft}/{storedDisease}";
        return JToken.FromObject(data);
    }

    public void LoadDawnSaveData(JToken saveData)
    {
        string data = saveData.Value<string>()!;

        if (string.IsNullOrWhiteSpace(data)) { return; }

        string[] args = data.Split("/");

        ammoLeft = int.Parse(args[0]);
        if (args.Length == 1) { return; } // TODO: Test this

        SetDisease(args[1]);
    }
}

internal class DartProjectile : MonoBehaviour
{
    public Rigidbody rigidbody = null!;

    float stickTime = 5f;

    public void Update()
    {
        if (stickTime > 0f)
            stickTime -= Time.deltaTime;

        if (stickTime <= 0 && rigidbody.isKinematic)
        {
            rigidbody.isKinematic = false;
            transform.SetParent(null);
        }
    }
}
