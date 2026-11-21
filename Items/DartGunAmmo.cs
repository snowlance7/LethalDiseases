using Dawn;
using Dawn.Interfaces;
using Newtonsoft.Json.Linq;
using SnowyCraftingCore;
using SnowyLib;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items
{
    internal class DartGunAmmo : PhysicsProp, IDawnSaveData, IDiseaseStorable // TODO: Lots of bugs omgah
    {
        public static bool IsEnabled => LethalContent.Items[LethalDiseasesKeys.DartGunAmmo] != null;

        [SerializeField]
        MeshRenderer[] dartRenderers = null!;

        ScanNodeProperties scanNode = null!;

        [HideInInspector]
        public string StoredDisease { get; set; } = "";

        int dartsToShow = 0;

        public int AmmoLeft { get; private set; } = 0;
        public bool IsFilled => AmmoLeft > 0;

        public static int MaxDartsInAmmo => PluginInstance.Config.Bind("Dart Gun Options", "Dart Gun | Max Darts", 10, "The max amount of darts in a clip").Value;

        bool itemMeshEnabled = true;

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0.07f, 0.12f, 0.02f);
            itemProperties.rotationOffset = new Vector3(20, -80, 100);

            scanNode = gameObject.GetComponentInChildren<ScanNodeProperties>();
        }

        [StaticInit]
        public static void InitConfigs()
        {
            _ = MaxDartsInAmmo;
        }

        public override void EnableItemMeshes(bool enable)
        {
            base.EnableItemMeshes(enable);
            itemMeshEnabled = enable;
            SetAmmoVisual();
        }

        public void SetFluidColor(ChemistryLiquidAppearance color)
        {
            foreach (var dartRenderer in dartRenderers)
            {
                dartRenderer.materials[2].color = color.liquidColor;
                dartRenderer.materials[2].SetColor("_EmissiveColor", color.liquidColor);
                dartRenderer.materials[2].SetFloat("_EmissiveIntensity", color.emissionIntensity);
            }
        }

        public void UseAmmo()
        {
            SetAmmo(AmmoLeft - 1);
        }

        public void SetAmmo(int ammoAmount)
        {
            AmmoLeft = ammoAmount;
            dartsToShow = Mathf.CeilToInt((float)AmmoLeft / MaxDartsInAmmo * 5);
            SetAmmoVisual();
        }

        public void SetAmmoVisual()
        {
            for (int i = 0; i < 5; i++)
            {
                dartRenderers[i].enabled = i < dartsToShow && itemMeshEnabled;
            }
        }

        public void SetDisease(string diseaseId)
        {
            Disease? disease = Disease.GetDiseaseFromString(diseaseId);
            if (disease == null) { logger.LogError($"Failed to parse disease from string: {diseaseId}"); return; }

            logger.LogDebug("Setting disease to " + diseaseId);
            StoredDisease = diseaseId;
            SetFluidColor(disease.GetChemistryLiquidAppearance());
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void SetDiseaseRpc(string diseaseId)
        {
            SetDisease(diseaseId);
            SetAmmo(MaxDartsInAmmo);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void SetDiseaseRpc(string diseaseId, int ammoAmount)
        {
            SetDisease(diseaseId);
            SetAmmo(ammoAmount);
        }

        public JToken GetDawnDataToSave()
        {
            string data = $"{AmmoLeft}/{StoredDisease}";
            return JToken.FromObject(data);
        }

        public void LoadDawnSaveData(JToken saveData)
        {
            string data = saveData.Value<string>()!;

            if (string.IsNullOrWhiteSpace(data)) { return; }

            string[] args = data.Split("/");

            SetAmmo(int.Parse(args[0]));
            if (args.Length == 1) { return; } // TODO: Test this

            SetDisease(args[1]);
        }
    }
}
