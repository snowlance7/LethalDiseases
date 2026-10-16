using Dawn;
using Dawn.Utils;
using SnowyCraftingCore;
using SnowyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Items.TonicDefinition;
using static LethalDiseases.Plugin;
using static LethalDiseases.Symptom;

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

internal class TonicDefinition(string description, TonicAction tonicAction, TonicType tonicType, Func<Disease, Disease> diseaseModification, string name = "", bool spawnable = true, bool craftable = true, bool wild = false) : IEquatable<TonicDefinition>
{
    public static List<TonicDefinition> RegisteredTonicDefinitions = new List<TonicDefinition>();

    public string name = name;
    public string description = description;
    public TonicAction tonicAction = tonicAction;
    public TonicType tonicType = tonicType;
    public Func<Disease, Disease> diseaseModification = diseaseModification;
    public bool spawnable = spawnable;
    public bool craftable = craftable;
    public bool wild = wild;

    public enum TonicAction
    {
        Add,
        Remove,
        Replace,
        Other
    }

    public enum TonicType
    {
        Symptoms,
        Stats,
        TransmissionTypes,
        Other
    }

    public Color GetColor()
    {
        Color actionColor = tonicAction switch
        {
            TonicAction.Add => Color.green,
            TonicAction.Remove => Color.red,
            TonicAction.Replace => Color.magenta,
            _ => UnityEngine.Random.ColorHSV()
        };

        Color typeColor = tonicType switch
        {
            TonicType.Symptoms => Color.red,
            TonicType.Stats => Color.green,
            TonicType.TransmissionTypes => Color.blue,
            _ => UnityEngine.Random.ColorHSV()
        };

        return Color.Lerp(actionColor, typeColor, 0.35f);
    }

    public ChemistryLiquidAppearance GetChemistryLiquidAppearance()
    {
        return new ChemistryLiquidAppearance(GetColor(), wild ? 5f : 0.5f); // TODO: Test this
    }

    public bool Equals(TonicDefinition other)
    {
        return name == other.name && description == other.description;
    }

    public static void Register(string description, TonicAction tonicAction, TonicType tonicType, Func<Disease, Disease> diseaseModification, string name = "", bool spawnable = true)
    {
        Register(new TonicDefinition(description, tonicAction, tonicType, diseaseModification, name, spawnable));
    }

    public static void Register(TonicDefinition tonicDefinition)
    {
        if (RegisteredTonicDefinitions.Contains(tonicDefinition)) { logger.LogError($"Failed to register tonic definition: {tonicDefinition.ToString()}. Tonic definition already exists."); return; }
        RegisteredTonicDefinitions.Add(tonicDefinition);
    }

    public static int GetTonicIndexFromName(string name)
    {
        var definition = RegisteredTonicDefinitions.FirstOrDefault(x => x.name == name);
        if (definition == null) { return -1; }
        return RegisteredTonicDefinitions.IndexOf(definition);
    }

    [StaticInit]
    public static void Init()
    {
        // TODO

        ChemistryIngredient tonicIngredient = new ChemistryIngredient(LethalDiseasesKeys.Tonic);
        CraftingAPI.RegisterIngredient(tonicIngredient);

        CraftingAPI.RegisterRecipe(new ChemistryRecipe(new ChemistryIngredient(LethalDiseasesKeys.TestTube), tonicIngredient, (ingredientA, ingredientB) =>
        {
            Disease? disease = Disease.GetDiseaseFromIngredient(ingredientA);
            if (disease == null) { disease = Disease.GetDiseaseFromIngredient(ingredientB); }
            if (disease == null) { return new ChemistryIngredient(LethalDiseasesKeys.TestTube); }

            int tonicDefIndex;
            if (!int.TryParse(ingredientA.specialInstructions, out tonicDefIndex)) { tonicDefIndex = int.Parse(ingredientB.specialInstructions); }
            var tonicDef = RegisteredTonicDefinitions[tonicDefIndex];

            Disease? newDisease = tonicDef.diseaseModification.Invoke(disease);
            if (newDisease == null) { return new ChemistryIngredient(LethalDiseasesKeys.TestTube); }

            return new ChemistryIngredient(LethalDiseasesKeys.TestTube, newDisease.GetChemistryLiquidAppearance(), newDisease.ToString());
        }));

        // TODO: Have each ingredient define an tonicaction and tonictype, then choose a random tonic with the tonicaction and tonictype. 

        //// Symptoms

        // Add
        Register("50% chance to add a good symptom", TonicAction.Add, TonicType.Symptoms, (disease) => Utils.randomGlobal.NextBool() ? disease.AddRandomSymptom(SymptomType.Good) : disease);
        Register("50% chance to add a good or neutral symptom", TonicAction.Add, TonicType.Symptoms, (disease) => Utils.randomGlobal.NextBool() ? disease.AddRandomSymptom(SymptomType.Good) : disease.AddRandomSymptom(SymptomType.Neutral));
        Register("Adds a random good symptom", TonicAction.Add, TonicType.Symptoms, (disease) => disease.AddRandomSymptom(SymptomType.Good));

        Register("50% chance to add a bad symptom", TonicAction.Add, TonicType.Symptoms, (disease) => Utils.randomGlobal.NextBool() ? disease.AddRandomSymptom(SymptomType.Bad) : disease);
        Register("50% chance to add a bad or neutral symptom", TonicAction.Add, TonicType.Symptoms, (disease) => Utils.randomGlobal.NextBool() ? disease.AddRandomSymptom(SymptomType.Bad) : disease.AddRandomSymptom(SymptomType.Neutral));
        Register("Adds a random bad symptom", TonicAction.Add, TonicType.Symptoms, (disease) => disease.AddRandomSymptom(SymptomType.Bad));

        Register("Adds a random neutral symptom", TonicAction.Add, TonicType.Symptoms, (disease) => disease.AddRandomSymptom(SymptomType.Neutral));

        Register("50% chance to add a random symptom", TonicAction.Add, TonicType.Symptoms, (disease) => Utils.randomGlobal.NextBool() ? disease.AddRandomSymptom() : disease);
        Register("Adds a random symptom", TonicAction.Add, TonicType.Symptoms, (disease) => disease.AddRandomSymptom());

        // Remove
        Register("50% chance to remove a good symptom", TonicAction.Remove, TonicType.Symptoms, (disease) => Utils.randomGlobal.NextBool() ? disease.RemoveRandomSymptom(SymptomType.Good) : disease);
        Register("50% chance to remove a good or neutral symptom", TonicAction.Remove, TonicType.Symptoms, (disease) => Utils.randomGlobal.NextBool() ? disease.RemoveRandomSymptom(SymptomType.Good) : disease.RemoveRandomSymptom(SymptomType.Neutral));
        Register("Removes a random good symptom", TonicAction.Remove, TonicType.Symptoms, (disease) => disease.RemoveRandomSymptom(SymptomType.Good));

        Register("50% chance to remove a bad symptom", TonicAction.Remove, TonicType.Symptoms, (disease) => Utils.randomGlobal.NextBool() ? disease.RemoveRandomSymptom(SymptomType.Bad) : disease);
        Register("50% chance to remove a bad or neutral symptom", TonicAction.Remove, TonicType.Symptoms, (disease) => Utils.randomGlobal.NextBool() ? disease.RemoveRandomSymptom(SymptomType.Bad) : disease.RemoveRandomSymptom(SymptomType.Neutral));
        Register("Removes a random bad symptom", TonicAction.Remove, TonicType.Symptoms, (disease) => disease.RemoveRandomSymptom(SymptomType.Bad));

        Register("Removes a random neutral symptom", TonicAction.Remove, TonicType.Symptoms, (disease) => disease.RemoveRandomSymptom(SymptomType.Neutral));

        Register("50% chance to remove a random symptom", TonicAction.Remove, TonicType.Symptoms, (disease) => Utils.randomGlobal.NextBool() ? disease.RemoveRandomSymptom() : disease);
        Register("Removes a random symptom", TonicAction.Remove, TonicType.Symptoms, (disease) => disease.RemoveRandomSymptom());

        // Replace
        Register("50% chance to replace a random good symptom with a random bad symptom", TonicAction.Replace, TonicType.Symptoms, (disease) => Utils.randomGlobal.NextBool() ? disease.ReplaceRandomSymptom(SymptomType.Good, SymptomType.Bad) : disease);
        Register("50% chance to replace a random good symptom with a random bad or neutral symptom", TonicAction.Replace, TonicType.Symptoms, (disease) => Utils.randomGlobal.NextBool() ? disease.ReplaceRandomSymptom(SymptomType.Good, SymptomType.Bad) : disease.ReplaceRandomSymptom(SymptomType.Good, SymptomType.Neutral));
        Register("Replaces a random good symptom with a random bad symptom", TonicAction.Replace, TonicType.Symptoms, (disease) => disease.ReplaceRandomSymptom(SymptomType.Good, SymptomType.Bad));

        Register("50% chance to replace a random bad symptom with a random good symptom", TonicAction.Replace, TonicType.Symptoms, (disease) => Utils.randomGlobal.NextBool() ? disease.ReplaceRandomSymptom(SymptomType.Bad, SymptomType.Good) : disease);
        Register("50% chance to replace a random bad symptom with a random good or neutral symptom", TonicAction.Replace, TonicType.Symptoms, (disease) => Utils.randomGlobal.NextBool() ? disease.ReplaceRandomSymptom(SymptomType.Bad, SymptomType.Good) : disease.ReplaceRandomSymptom(SymptomType.Bad, SymptomType.Neutral));
        Register("Replaces a random bad symptom with a random good symptom", TonicAction.Replace, TonicType.Symptoms, (disease) => disease.ReplaceRandomSymptom(SymptomType.Bad, SymptomType.Good));

        Register("Replaces a random neutral symptom with a random good symptom", TonicAction.Replace, TonicType.Symptoms, (disease) => disease.ReplaceRandomSymptom(SymptomType.Neutral, SymptomType.Good));
        Register("Replaces a random neutral symptom with a random bad symptom", TonicAction.Replace, TonicType.Symptoms, (disease) => disease.ReplaceRandomSymptom(SymptomType.Neutral, SymptomType.Bad));

        Register("50% chance to replace a random symptom with another random symptom", TonicAction.Replace, TonicType.Symptoms, (disease) => Utils.randomGlobal.NextBool() ? disease.ReplaceRandomSymptom() : disease);
        Register("Replaces a random symptom with another random symptom", TonicAction.Replace, TonicType.Symptoms, (disease) => disease.ReplaceRandomSymptom());

        //// Stats

        // Adding
        //Register("Increases strength by 5%", TonicAction.Add, TonicType.Stats, (disease) =>  );
        //Register("Increases stability by 5%");
        //Register("Increases latency by 5%");
        //Register("Increases transmission by 5%");

        //Register("Increases strength by 10%");
        //Register("Increases stability by 10%");
        //Register("Increases latency by 10%");
        //Register("Increases transmission by 10%");

        //Register("Increases strength by 15%");
        //Register("Increases stability by 15%");
        //Register("Increases latency by 15%");
        //Register("Increases transmission by 15%");

        //Register("Random percent chance to increase strength by a random percent");
        //Register("Random percent chance to increase stability by a random percent");
        //Register("Random percent chance to increase latency by a random percent");
        //Register("Random percent chance to increase transmission by a random percent");

        //Register("Increase strength by a random percent");
        //Register("Increase strength by a random percent");
        //Register("Increase strength by a random percent");
        //Register("Increase strength by a random percent");

        //// Wildcard
        //Register("Random percent chance to increase a random disease stat by a random percent");
        //Register("Increase a random disease stat by a random percent");

        //Register("Random percent chance to decrease a random disease stat by a random percent");
        //Register("Decrease a random disease stat by a random percent");

        //Register("Random percent chance to increase or decrease a random disease stat by a random percent");
        //Register("Increase or decrease a random disease stat by a random percent");

        //Register("Increase/decrease all disease stats by a random amount");
    }
}