using Dawn;
using Dawn.Interfaces;
using GameNetcodeStuff;
using Newtonsoft.Json.Linq;
using SnowyCraftingCore;
using System;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items
{
    internal class TestTubeBehavior : PhysicsProp, IAnalyzableIngredient, IMixableIngredient, IDawnSaveData
    {
        public static bool IsEnabled => LethalContent.Items[LethalDiseasesKeys.TestTube] != null;

        public MeshRenderer fluidRenderer = null!;

        public ScanNodeProperties scanNode = null!;

        public string storedDisease = "";

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0.05f, 0.05f, 0.01f);
            itemProperties.rotationOffset = new Vector3(0, 10, 0);
            itemProperties.floorYOffset = 90;
            scanNode = gameObject.GetComponentInChildren<ScanNodeProperties>();
        }

        public void SetFluidColor(ChemistryLiquidAppearance color)
        {
            logger.LogDebug("Setting fluid color to " + color.ToString());
            fluidRenderer.enabled = true;
            fluidRenderer.material.color = color.liquidColor;
            fluidRenderer.material.SetColor("_EmissiveColor", color.liquidColor);
            fluidRenderer.material.SetFloat("_EmissiveIntensity", color.emissionIntensity);
        }

        ChemistryIngredient IChemistryIngredient.GetIngredient()
        {
            return new ChemistryIngredient(LethalDiseasesKeys.TestTube, new ChemistryLiquidAppearance(fluidRenderer.material.color, fluidRenderer.material.GetFloat("_EmissiveIntensity")), storedDisease);
        }

        public void OnChemicalOutput(ChemistryIngredient ingredient)
        {
            SetDisease(ingredient.specialInstructions);
        }

        Action<AnalyzableIngredient, PlayerControllerB> IAnalyzableIngredient.OnAnalyze()
        {
            
            return static (ingredient, player) =>
            {
                Disease? disease = Disease.GetDiseaseFromString(ingredient.specialInstructions);
                if (disease == null) { HUDManager.Instance.DisplayTip("Analysis failed", "No diseases found", true); return; }

                disease.name = $"disease{Disease.namedDiseases.Count}";

                HUDManager.Instance.DisplayTip("Analysis complete", $"Results sent to terminal ({disease.name})");
            };
        }

        bool IMixableIngredient.DespawnItemAfterInput()
        {
            return true;
        }

        bool IAnalyzableIngredient.HoldItem()
        {
            return true;
        }

        public void SetDisease(string diseaseId)
        {
            logger.LogDebug($"Setting stored disease to disease with id: {diseaseId}");
            Disease? disease = Disease.GetDiseaseFromString(diseaseId);
            if (disease == null) { logger.LogError($"Unable to parse disease from id ({diseaseId})"); return; }

            storedDisease = diseaseId;
            scanNode.subText = disease.name;

            SetFluidColor(disease.GetChemistryLiquidAppearance());
        }

        public JToken GetDawnDataToSave()
        {
            return JToken.FromObject(storedDisease);
        }

        public void LoadDawnSaveData(JToken saveData)
        {
            storedDisease = saveData.Value<string>();
            if (string.IsNullOrWhiteSpace(storedDisease)) { return; }
            SetDisease(storedDisease);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void SetDiseaseRpc(string diseaseId)
        {
            SetDisease(diseaseId);
        }
    }
}