using Dawn;
using Dawn.Interfaces;
using Newtonsoft.Json.Linq;
using SnowyCraftingCore;
using SnowyLib;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items
{
    internal class CottonSwab : PhysicsProp, IDistillableIngredient, IDawnSaveData, IDiseaseStorable
    {
        public static bool IsEnabled => LethalContent.Items[LethalDiseasesKeys.CottonSwab] != null;

        public string StoredDisease { get; set; } = "";

        public MeshRenderer tipRenderer = null!;

        ScanNodeProperties scanNode = null!;

        readonly float maxDistance = 5f;

        [StaticInit]
        public static void Init()
        {
            ChemistryIngredient.Register(LethalDiseasesKeys.CottonSwab);
        }

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0.1f, 0.08f, 0.02f);
            itemProperties.rotationOffset = new Vector3(0, 10, 0);
            itemProperties.floorYOffset = 90;
            scanNode = gameObject.GetComponentInChildren<ScanNodeProperties>();
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!buttonDown || tipRenderer.enabled) { return; }

            if (!Physics.Raycast(playerHeldBy.gameplayCamera.transform.position + playerHeldBy.gameplayCamera.transform.forward, playerHeldBy.gameplayCamera.transform.forward, out RaycastHit hitInfo, maxDistance, PluginInstance.playerEnemiesPropsMask)) { return; }

            logger.LogDebug(hitInfo.collider.gameObject.name);

            if (!hitInfo.collider.gameObject.TryGetComponentInChildren(out NetworkObject? netObj)) { return; } // TODO: Test this
            if (netObj == null) { return; }
            logger.LogDebug("Got network object: " + netObj.name);

            List<Disease> diseases = netObj.GetDiseases();
            if (diseases.Count == 0)
            {
                SetDiseaseRpc("", new ChemistryLiquidAppearance(UnityEngine.Random.ColorHSV(), UnityEngine.Random.Range(0f, 10f)));
                return;
            }
            Disease disease = diseases.GetRandom();
            SetDiseaseRpc(disease.ToString(), disease.GetChemistryLiquidAppearance());
        }

        public void SetFluidColor(ChemistryLiquidAppearance color)
        {
            tipRenderer.enabled = true;
            tipRenderer.material.color = color.liquidColor;
            tipRenderer.material.SetColor("_EmissiveColor", color.liquidColor);
            tipRenderer.material.SetFloat("_EmissiveIntensity", color.emissionIntensity);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void SetDiseaseRpc(string disease, ChemistryLiquidAppearance color)
        {
            SetDisease(disease, color);
        }

        public ChemistryIngredient? DistilleryOutput()
        {
            return new ChemistryIngredient(LethalDiseasesKeys.TestTube, new ChemistryLiquidAppearance(tipRenderer.material.color, tipRenderer.material.GetFloat("_EmissiveIntensity")), StoredDisease);
        }

        public float DistilleryMixTime()
        {
            return 10f;
        }

        public bool DespawnItemAfterDistilleryInput()
        {
            return true;
        }

        public ChemistryIngredient? GetIngredient()
        {
            return new ChemistryIngredient(LethalDiseasesKeys.CottonSwab, new ChemistryLiquidAppearance(tipRenderer.material.color, tipRenderer.material.GetFloat("_EmissiveIntensity")), StoredDisease);
        }

        public void OnChemicalOutput(ChemistryIngredient ingredient)
        {
            SetDisease(ingredient.specialInstructions, ingredient.chemistryLiquidAppearance);
        }

        public void SetDisease(string diseaseString, ChemistryLiquidAppearance color)
        {
            StoredDisease = diseaseString;
            scanNode.subText = Disease.GetNameFromString(diseaseString);
            SetFluidColor(color);
        }

        public JToken GetDawnDataToSave()
        {
            return JToken.FromObject(StoredDisease);
        }

        public void LoadDawnSaveData(JToken saveData)
        {
            string? diseaseString = saveData.Value<string>();
            if (string.IsNullOrWhiteSpace(diseaseString)) { return; }
            Disease? disease = Disease.GetDiseaseFromString(diseaseString);
            if (disease == null) { return; }
            SetDisease(diseaseString, disease.GetChemistryLiquidAppearance());
        }
    }
}