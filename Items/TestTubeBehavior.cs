using LethalDiseases.Unlockables;
using UnityEngine;
using static LethalDiseases.Plugin;
using SnowyLib;
using SnowyCraftingCore;

namespace LethalDiseases.Items
{
    internal class TestTubeBehavior : PhysicsProp, IChemistryIngredient
    {
        public MeshRenderer fluidRenderer = null!;

        ScanNodeProperties scanNode = null!;

        public string storedDisease = "";

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0.05f, 0.05f, 0.01f);
            itemProperties.rotationOffset = new Vector3(0, 10, 0);
            itemProperties.floorYOffset = 90;
            scanNode = gameObject.GetComponentInChildren<ScanNodeProperties>();
        }

        ChemistryIngredient IChemistryIngredient.GetInputIngredient()
        {
            return new ChemistryIngredient(itemProperties, new ChemistryLiquidAppearance(fluidRenderer.material.color, fluidRenderer.material.GetFloat("_EmissionIntensity")), storedDisease);
        }

        void IChemistryIngredient.OnOutputIngredient(string specialInstructions)
        {
            Disease? disease = Disease.GetDiseaseFromString(storedDisease);
            if (disease == null) { logger.LogError("Unable to parse disease from id"); return; }

            storedDisease = specialInstructions;
            scanNode.subText = disease.name;

            SetFluidColor(disease.GetChemistryLiquidAppearance());
        }

        bool IChemistryIngredient.DespawnItemAfterInput()
        {
            return true;
        }

        public void SetFluidColor(ChemistryLiquidAppearance color)
        {
            Material material = new(fluidRenderer.material);

            material.color = color.liquidColor;
            material.SetColor("_EmissionColor", color.liquidColor);
            material.SetFloat("_EmissionIntensity", color.emissionIntensity);

            fluidRenderer.material = material;
        }
    }
}