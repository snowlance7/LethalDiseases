using LethalDiseases.Unlockables;
using UnityEngine;
using static LethalDiseases.Plugin;
using SnowyLib;
using SnowyCraftingCore;
using System;
using System.Diagnostics.CodeAnalysis;
using TerminalApi;
using GameNetcodeStuff;
using TerminalApi.Classes;
using SnowyCraftingCore.TerminalAdditions;
using Dawn;

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

        public void OnChemicalOutput(string specialInstructions)
        {
            logger.LogDebug("Test tube OnChemicalOutput start");
            Disease? disease = Disease.GetDiseaseFromString(specialInstructions);
            if (disease == null) { logger.LogError("Unable to parse disease from id"); return; }

            storedDisease = specialInstructions;
            scanNode.subText = disease.name;

            SetFluidColor(disease.GetChemistryLiquidAppearance());
        }

        Action<AnalyzableIngredient, PlayerControllerB> IAnalyzableIngredient.OnAnalyze()
        {
            return static (ingredient, player) =>
            {
                Disease? disease = Disease.GetDiseaseFromString(ingredient.specialInstructions);
                if (disease == null) { HUDManager.Instance.DisplayTip("Analysis failed", "No diseases found", true); return; }

                resultCounter++;
                TerminalApi.TerminalApi.AddCommand($"result{resultCounter}", disease.GetTerminalDisplayText());
                HUDManager.Instance.DisplayTip("Analysis complete", $"Results sent to terminal (result{resultCounter})");

                if (ApparatusPowerPort.Instance == null || SmallItemDispenser.Instance == null) { return; }

                TerminalApi.TerminalApi.AddCommand($"synthesize result{resultCounter}", new CommandInfo()
                {
                    Title = "synthesize [diseaseName/result]",
                    DisplayTextSupplier = () =>
                    {
                        if (!ApparatusPowerPort.Instance.IsApparatusInSlot) { return "Not enough power to synthesize, alternative power source required"; }
                        if (!ApparatusPowerPort.Instance.UsePower(0.1f)) { return "Not enough power available, new alternative power source required"; }
                        SmallItemDispenser.Instance.ItemDispenseOperation(LethalContent.Items[LethalDiseasesKeys.TestTube].Item, (item) =>
                        {
                            ((TestTubeBehavior)item).OnChemicalOutput(disease.ToString());
                        }, 30f);
                        return "Disease synthesized, 10% power used";
                    },
                    Category = "Other"
                });
            };
        }

        bool IMixableIngredient.DespawnItemAfterInput()
        {
            return true;
        }

        bool IAnalyzableIngredient.DespawnItemOnAnalyze()
        {
            return false;
        }

        bool IAnalyzableIngredient.HoldItem()
        {
            return true;
        }
    }
}