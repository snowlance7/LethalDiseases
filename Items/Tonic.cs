using SnowyCraftingCore;
using SnowyLib;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Items.Tonic;
using static LethalDiseases.Items.TonicDefinition;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items;

internal class Tonic : PhysicsProp, IMixableIngredient
{
    [SerializeField] GameObject[] tonicObjs = [];
    [SerializeField] MeshRenderer[] tonicFluidRenderers = []; // TODO: Set this in unity

    ScanNodeProperties scanNode = null!;

    int tonicDefIndex = -1;

    public void Awake()
    {
        scanNode = gameObject.GetComponentInChildren<ScanNodeProperties>();
    }

    public void SetTonicDefinition(int tonicDefIndex)
    {
        this.tonicDefIndex = tonicDefIndex;
        var def = RegisteredTonicDefinitions[tonicDefIndex];
        int tonicObjsIndex = ((int)def.tonicType) * ((int)def.tonicAction);
        tonicObjs[tonicObjsIndex].SetActive(true);
        scanNode.headerText = $"{def.name} Tonic";
        scanNode.subText = def.description;
    }

    public void SetFluidColor(ChemistryLiquidAppearance color)
    {
        logger.LogDebug("Setting fluid color to " + color.ToString());
        tonicFluidRenderers[tonicDefIndex].material.color = color.liquidColor;
        tonicFluidRenderers[tonicDefIndex].material.SetColor("_EmissiveColor", color.liquidColor);
        tonicFluidRenderers[tonicDefIndex].material.SetFloat("_EmissiveIntensity", color.emissionIntensity);
    }

    public ChemistryLiquidAppearance GetChemistryLiquidAppearance()
    {
        return RegisteredTonicDefinitions[tonicDefIndex].GetChemistryLiquidAppearance();
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void SetTonicDefinitionRpc(int tonicDefIndex)
    {
        SetTonicDefinition(tonicDefIndex);
    }

    public bool DespawnItemAfterInput()
    {
        return true;
    }

    public ChemistryIngredient? GetIngredient()
    {
        return new ChemistryIngredient(LethalDiseasesKeys.Tonic, GetChemistryLiquidAppearance(), tonicDefIndex.ToString());
    }

    public void OnChemicalOutput(ChemistryIngredient ingredient)
    {
        SetTonicDefinition(int.Parse(ingredient.specialInstructions));
    }
}

internal class TonicDefinition(string name, string description, TonicAction tonicAction, TonicType tonicType, Func<Disease, Disease> diseaseModification)
{
    public static List<TonicDefinition> RegisteredTonicDefinitions = new List<TonicDefinition>();

    public string name = name;
    public string description = description;
    public TonicAction tonicAction = tonicAction;
    public TonicType tonicType = tonicType;
    public Func<Disease, Disease> diseaseModification = diseaseModification;

    public enum TonicAction
    {
        Adding,
        Removing,
        Replacing
    }

    public enum TonicType
    {
        Symptoms,
        Stats,
        TransmissionTypes
    }

    [StaticInit]
    public static void Init()
    {
        // TODO
    }

    public Color GetColor()
    {
        Color actionColor = tonicAction switch
        {
            TonicAction.Adding => Color.green,
            TonicAction.Removing => Color.red,
            TonicAction.Replacing => Color.magenta,
            _ => Color.white
        };

        Color typeColor = tonicType switch
        {
            TonicType.Symptoms => Color.red,
            TonicType.Stats => Color.green,
            TonicType.TransmissionTypes => Color.blue,
            _ => Color.white
        };

        return Color.Lerp(actionColor, typeColor, 0.35f);
    }

    public ChemistryLiquidAppearance GetChemistryLiquidAppearance()
    {
        return new ChemistryLiquidAppearance(GetColor(), 5f);
    }
}