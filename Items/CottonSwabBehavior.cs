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
    internal class CottonSwabBehavior : PhysicsProp, IDistillableIngredient, IDawnSaveData
    {
        public static bool IsEnabled => LethalContent.Items[LethalDiseasesKeys.CottonSwab] != null;

        public MeshRenderer tipRenderer = null!;

        ScanNodeProperties scanNode = null!;

        public string storedDisease = "";

        readonly float maxDistance = 5f;

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
            if (!buttonDown || storedDisease != "") { return; }

            if (!Physics.Raycast(playerHeldBy.gameplayCamera.transform.position + playerHeldBy.gameplayCamera.transform.forward, playerHeldBy.gameplayCamera.transform.forward, out RaycastHit hitInfo, maxDistance, PluginInstance.playerEnemiesPropsMask)) { return; }

            logger.LogDebug(hitInfo.collider.gameObject.name);

            if (!hitInfo.collider.gameObject.TryGetComponentInChildren(out NetworkObject? netObj)) { return; } // TODO: Test this
            if (netObj == null) { return; }
            logger.LogDebug("Got network object: " + netObj.name);

            List<Disease> diseases = netObj.GetDiseases();
            if (diseases.Count == 0) { return; }
            Disease? disease = diseases.GetRandom();
            if (disease == null) { return; }
            SetDiseaseRpc(disease.ToString());
        }

        public void SetFluidColor(ChemistryLiquidAppearance color)
        {
            tipRenderer.enabled = true;
            tipRenderer.material.color = color.liquidColor;
            tipRenderer.material.SetColor("_EmissiveColor", color.liquidColor);
            tipRenderer.material.SetFloat("_EmissiveIntensity", color.emissionIntensity);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void SetDiseaseRpc(string disease)
        {
            SetDisease(disease);
        }

        public ChemistryIngredient? DistilleryOutput()
        {
            return new ChemistryIngredient(LethalDiseasesKeys.TestTube, new ChemistryLiquidAppearance(tipRenderer.material.color, tipRenderer.material.GetFloat("_EmissiveIntensity")), storedDisease);
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
            return new ChemistryIngredient(LethalDiseasesKeys.CottonSwab, new ChemistryLiquidAppearance(tipRenderer.material.color, tipRenderer.material.GetFloat("_EmissiveIntensity")), storedDisease);
        }

        public void OnChemicalOutput(ChemistryIngredient ingredient)
        {
            SetDisease(ingredient.specialInstructions);
        }

        public void SetDisease(string diseaseId)
        {
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
    }
}