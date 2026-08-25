using LethalDiseases.Unlockables;
using UnityEngine;

namespace LethalDiseases.Items
{
    internal class TestTubeBehavior : PhysicsProp, IChemistryIngredient
    {
        public MeshRenderer liquidRenderer = null!;

        public string[] filledDiseases = [];

        Color liquidColor;

        public ChemistryIngredient GetInputIngredient()
        {
            return new ChemistryIngredient(itemProperties, new ChemistryLiquidAppearance(liquidColor, liquidColor, 5f), string.Join("^", filledDiseases));
        }

        public void OnOutputIngredient(string specialInstructions)
        {
            liquidRenderer.enabled = true;
            filledDiseases = specialInstructions.Split("^");
            liquidColor = Random.ColorHSV();

            Material liquidMaterial = liquidRenderer.material;
            liquidMaterial.color = liquidColor;
            liquidMaterial.SetColor("_EmissionColor", liquidColor);
            liquidMaterial.SetFloat("_EmissionIntensity", 5f);
            liquidRenderer.material = liquidMaterial;
        }
    }
}