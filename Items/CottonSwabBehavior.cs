using Dawn;
using LethalDiseases.Unlockables;
using SnowyCraftingCore;
using SnowyLib;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items
{
    internal class CottonSwabBehavior : PhysicsProp, IDistillableIngredient
    {
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

        public void SetTipColor(ChemistryLiquidAppearance color)
        {
            tipRenderer.enabled = true;
            tipRenderer.material.color = color.liquidColor;
            tipRenderer.material.SetColor("_EmissionColor", color.liquidColor);
            tipRenderer.material.SetFloat("_EmissionIntensity", color.emissionIntensity);
        }

        public void SetDiseaseOnLocalClient(string _disease)
        {
            Disease? disease = Disease.GetDiseaseFromString(_disease);
            if (disease == null) { logger.LogError("Unable to parse disease from id"); return; }

            storedDisease = _disease;
            scanNode.subText = disease.name;

            SetTipColor(disease.GetChemistryLiquidAppearance());
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void SetDiseaseRpc(string _disease)
        {
            SetDiseaseOnLocalClient(_disease);
        }

        ChemistryIngredient? IDistillableIngredient.DistilleryOutput()
        {
            return new ChemistryIngredient(LethalDiseasesKeys.TestTube, new ChemistryLiquidAppearance(tipRenderer.material.color, tipRenderer.material.GetFloat("_EmissionIntensity")), storedDisease);
        }

        float IDistillableIngredient.DistilleryMixTime()
        {
            return 10f;
        }

        bool IDistillableIngredient.DespawnItemAfterDistilleryInput()
        {
            return true;
        }

        ChemistryIngredient? IChemistryIngredient.GetIngredient()
        {
            return new ChemistryIngredient(LethalDiseasesKeys.CottonSwab, new ChemistryLiquidAppearance(tipRenderer.material.color, tipRenderer.material.GetFloat("_EmissionIntensity")), storedDisease);
        }

        void IChemistryIngredient.OnChemicalOutput(string specialInstructions)
        {
            SetDiseaseOnLocalClient(specialInstructions);
        }
    }
}