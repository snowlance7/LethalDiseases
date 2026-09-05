using LethalDiseases.Unlockables;
using UnityEngine;
using static LethalDiseases.Plugin;
using SnowyLib;
using SnowyCraftingCore;
using System;
using System.Diagnostics.CodeAnalysis;
using TerminalApi;

namespace LethalDiseases.Items
{
    internal class TestTubeBehavior : PhysicsProp, IAnalyzableIngredient, IMixableIngredient
    {
        public static int resultCounter = 0;
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

        public void SetFluidColor(ChemistryLiquidAppearance color)
        {
            logger.LogDebug("Setting fluid color to " + color.ToString());
            fluidRenderer.material.color = color.liquidColor;
            fluidRenderer.material.SetColor("_EmissionColor", color.liquidColor);
            fluidRenderer.material.SetFloat("_EmissionIntensity", color.emissionIntensity);
        }

        ChemistryIngredient IChemistryIngredient.GetIngredient()
        {
            return new ChemistryIngredient(itemProperties, new ChemistryLiquidAppearance(fluidRenderer.material.color, fluidRenderer.material.GetFloat("_EmissionIntensity")), storedDisease);
        }

        void IChemistryIngredient.OnChemicalOutput(string specialInstructions)
        {
            logger.LogDebug("Test tube OnChemicalOutput start");
            Disease? disease = Disease.GetDiseaseFromString(specialInstructions);
            if (disease == null) { logger.LogError("Unable to parse disease from id"); return; }

            storedDisease = specialInstructions;
            scanNode.subText = disease.name;

            SetFluidColor(disease.GetChemistryLiquidAppearance());
        }

        Action<AnalyzableIngredient> IAnalyzableIngredient.OnAnalyze()
        {
            return static (ingredient) =>
            {
                // TODO
                Disease? disease = Disease.GetDiseaseFromString(ingredient.specialInstructions);
                if (disease == null) { HUDManager.Instance.DisplayTip("Analysis failed", "No diseases found", true); return; }

                resultCounter++;
                TerminalApi.TerminalApi.AddCommand($"result{resultCounter}", disease.GetTerminalDisplayText());
            };
        }

        bool IAnalyzableIngredient.DespawnItemAfterAnalyzing()
        {
            return false;
        }

        bool IMixableIngredient.DespawnItemAfterInput()
        {
            return true;
        }
    }
}