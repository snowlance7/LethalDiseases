using LethalDiseases.Unlockables;
using UnityEngine;
using static LethalDiseases.Plugin;
using SnowyLib;
using SnowyCraftingCore;
using System;
using System.Diagnostics.CodeAnalysis;

namespace LethalDiseases.Items
{
    internal class TestTubeBehavior : PhysicsProp, IAnalyzableIngredient, IMixableIngredient
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

        public void SetFluidColor(ChemistryLiquidAppearance color)
        {
            fluidRenderer.material.color = color.liquidColor;
            fluidRenderer.material.SetColor("_EmissionColor", color.liquidColor);
            fluidRenderer.material.SetFloat("_EmissionIntensity", color.emissionIntensity);
        }

        ChemistryIngredient IChemistryIngredient.GetIngredient()
        {
            return new ChemistryIngredient(itemProperties, new ChemistryLiquidAppearance(fluidRenderer.material.color, fluidRenderer.material.GetFloat("_EmissionIntensity")), storedDisease);
        }

        void IChemistryIngredient.OnChemicalMixerOutput(string specialInstructions)
        {
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
            };
        }

        bool IAnalyzableIngredient.DespawnItemAfterAnalyzing()
        {
            return true;
        }

        bool IMixableIngredient.DespawnItemAfterInput()
        {
            return true;
        }
    }
}