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

        public string storedDisease = "";

        ChemistryIngredient IChemistryIngredient.GetInputIngredient()
        {
            return new ChemistryIngredient(itemProperties, new ChemistryLiquidAppearance(fluidRenderer.material.color, fluidRenderer.material.GetFloat("_EmissionIntensity")), storedDisease);
        }

        void IChemistryIngredient.OnOutputIngredient(string specialInstructions)
        {
            storedDisease = specialInstructions;
            Disease? disease = Disease.GetDiseaseFromString(storedDisease);

            if (disease != null)
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