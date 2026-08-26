using LethalDiseases.Unlockables;
using SnowyLib;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items
{
    internal class CottonSwabBehavior : PhysicsProp, IChemistryIngredient
    {
        public MeshRenderer tipRenderer = null!;

        public string storedDisease = "";

        readonly float maxDistance = 2f;

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0, 0, 0);
            itemProperties.rotationOffset = new Vector3(0, 0, 0);
            itemProperties.floorYOffset = 0;
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!buttonDown || storedDisease != "") { return; }

            if (!Physics.Raycast(playerHeldBy.transform.position, playerHeldBy.transform.forward, out RaycastHit hitInfo, maxDistance, playerEnemiesPropsMask)) { return; }

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
            Material material = new(tipRenderer.material);

            material.color = color.liquidColor;
            material.SetColor("_EmissionColor", color.liquidColor);
            material.SetFloat("_EmissionIntensity", color.emissionIntensity);

            tipRenderer.material = material;
        }

        public void SetDiseaseOnLocalClient(string _disease)
        {
            storedDisease = _disease;

            Disease? disease = Disease.GetDiseaseFromString(storedDisease);
            if (disease == null) { logger.LogError("Unable to parse disease from id"); return; }
            SetTipColor(disease.GetChemistryLiquidAppearance());
        }

        [Rpc(SendTo.Everyone)]
        public void SetDiseaseRpc(string _disease)
        {
            SetDiseaseOnLocalClient(_disease);
        }

        ChemistryIngredient IChemistryIngredient.GetInputIngredient()
        {
            return new ChemistryIngredient(itemProperties, new ChemistryLiquidAppearance(tipRenderer.material.color, tipRenderer.material.GetFloat("_EmissionIntensity")), storedDisease);
        }

        void IChemistryIngredient.OnOutputIngredient(string specialInstructions)
        {
            SetDiseaseOnLocalClient(specialInstructions);
        }
    }
}