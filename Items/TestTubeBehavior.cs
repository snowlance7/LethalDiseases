using LethalDiseases.Unlockables;
using UnityEngine;
using static LethalDiseases.Plugin;
using SnowyLib;

namespace LethalDiseases.Items
{
    internal class TestTubeBehavior : PhysicsProp, IChemistryIngredient
    {
        public MeshRenderer fluidRenderer = null!;

        public string storedDisease = "";

        public ChemistryIngredient GetInputIngredient()
        {
            return new ChemistryIngredient(itemProperties, new ChemistryLiquidAppearance(fluidRenderer.material.color, fluidRenderer.material.GetFloat("_EmissionIntensity")), storedDisease);
        }

        public void SetFluidColor(ChemistryLiquidAppearance color)
        {
            Material material = new(fluidRenderer.material);

            material.color = color.liquidColor;
            material.SetColor("_EmissionColor", color.liquidColor);
            material.SetFloat("_EmissionIntensity", color.emissionIntensity);

            fluidRenderer.material = material;
        }

        public void OnOutputIngredient(string specialInstructions)
        {
            storedDisease = specialInstructions;
            Disease? disease = Disease.GetDiseaseFromString(storedDisease);

            if (disease != null)
                SetFluidColor(disease.GetChemistryLiquidAppearance());

        }
    }
}