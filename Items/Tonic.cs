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
using static LethalDiseases.Disease;
using static LethalDiseases.Plugin;
using static LethalDiseases.Symptom;

namespace LethalDiseases.Items;

internal class Tonic : PhysicsProp, IChemistryIngredient
{
    public static bool IsEnabled => LethalContent.Items[LethalDiseasesKeys.Tonic] != null;

    [SerializeField] GameObject[] tonicObjs = [];
    [SerializeField] MeshRenderer[] tonicFluidRenderers = []; // TODO: Set this in unity

    ScanNodeProperties scanNode = null!;

    int tonicDefIndex = -1;

    public void Awake()
    {
        scanNode = gameObject.GetComponentInChildren<ScanNodeProperties>();
    }

    public override void Start()
    {
        base.Start();
        if (!transform.position.IsOutside()) SetRandomWildTonicDefinition();
    }

    public void SetRandomWildTonicDefinition()
    {
        var def = RegisteredTonicDefinitions.Where(x => x.wild == true).GetRandom(Utils.randomGlobal)!;
        tonicDefIndex = RegisteredTonicDefinitions.IndexOf(def);
        SetTonicDefinition(tonicDefIndex);
    }

    public void SetTonicDefinition(int tonicDefIndex)
    {
        this.tonicDefIndex = tonicDefIndex;
        var def = RegisteredTonicDefinitions[tonicDefIndex];
        int tonicObjsIndex = ((int)def.tonicType) * ((int)def.tonicAction);
        tonicObjs[tonicObjsIndex].SetActive(true);
        scanNode.headerText = $"{(!string.IsNullOrWhiteSpace(def.name) ? $"{def.name} " : "")}Tonic";
        scanNode.subText = def.description;
        SetFluidColor(GetChemistryLiquidAppearance());
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

    public ChemistryIngredient? GetIngredient()
    {
        return new ChemistryIngredient(LethalDiseasesKeys.Tonic, GetChemistryLiquidAppearance(), tonicDefIndex.ToString());
    }

    public void OnChemicalOutput(ChemistryIngredient ingredient)
    {
        SetTonicDefinition(int.Parse(ingredient.specialInstructions));
    }
}

internal class TonicDefinition : IEquatable<TonicDefinition>
{
    public static List<TonicDefinition> RegisteredTonicDefinitions = new List<TonicDefinition>();

    public string name = "";
    public string description = "";
    public TonicAction tonicAction;
    public TonicType tonicType;
    public Func<Disease, Disease> diseaseModification;
    public bool wild;

    public TonicDefinition(string description, TonicAction tonicAction, TonicType tonicType, Func<Disease, Disease> diseaseModification, bool wild = false)
    {
        this.description = description;
        this.tonicAction = tonicAction;
        this.tonicType = tonicType;
        this.diseaseModification = diseaseModification;
        this.wild = wild;
    }

    public TonicDefinition(string name, string description, TonicAction tonicAction, TonicType tonicType, Func<Disease, Disease> diseaseModification, bool wild = false)
    {
        this.name = name;
        this.description = description;
        this.tonicAction = tonicAction;
        this.tonicType = tonicType;
        this.diseaseModification = diseaseModification;
        this.wild = wild;
    }

    public enum TonicAction
    {
        Add,
        Remove,
        Replace
    }

    public enum TonicType
    {
        Symptom,
        Stat,
        TransmissionType
    }

    private static TonicAction? TryParseTonicAction(string value)
    {
        switch (value)
        {
            case "Add":
                return TonicAction.Add;
            case "Remove":
                return TonicAction.Remove;
            case "Replace":
                return TonicAction.Replace;
            default:
                return null;
        }
    }

    private static TonicType? TryParseTonicType(string value)
    {
        switch (value)
        {
            case "Symptom":
                return TonicType.Symptom;
            case "Stat":
                return TonicType.Stat;
            case "TransmissionType":
                return TonicType.TransmissionType;
            default:
                return null;
        }
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
            TonicType.Symptom => Color.red,
            TonicType.Stat => Color.green,
            TonicType.TransmissionType => Color.blue,
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

    public static void Register(string name, string description, TonicAction tonicAction, TonicType tonicType, Func<Disease, Disease> diseaseModification, bool wild = false)
    {
        Register(new TonicDefinition(name, description, tonicAction, tonicType, diseaseModification, wild));
    }

    public static void Register(string description, TonicAction tonicAction, TonicType tonicType, Func<Disease, Disease> diseaseModification, bool wild = false)
    {
        Register(new TonicDefinition(description, tonicAction, tonicType, diseaseModification, wild));
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
        ChemistryIngredient.Register(tonicIngredient);

        ChemistryRecipe.Register(new ChemistryIngredient(LethalDiseasesKeys.TestTube), tonicIngredient, (ingredientA, ingredientB) =>
        {
            Disease? disease = GetDiseaseFromIngredient(ingredientA);
            if (disease == null) { disease = GetDiseaseFromIngredient(ingredientB); }
            if (disease == null) { return new ChemistryIngredient(LethalDiseasesKeys.TestTube); }

            int tonicDefIndex;
            if (!int.TryParse(ingredientA.specialInstructions, out tonicDefIndex)) { tonicDefIndex = int.Parse(ingredientB.specialInstructions); }
            var tonicDef = RegisteredTonicDefinitions[tonicDefIndex];

            Disease? newDisease = tonicDef.diseaseModification.Invoke(disease);
            if (newDisease == null) { return new ChemistryIngredient(LethalDiseasesKeys.TestTube); }

            return new ChemistryIngredient(LethalDiseasesKeys.TestTube, newDisease.GetChemistryLiquidAppearance(), newDisease.ToString());
        });

        ChemistryIngredient mushroomIngredient = new ChemistryIngredient(LethalDiseasesKeys.Mushroom);
        ChemistryIngredient.Register(mushroomIngredient);

        ChemistryRecipe.Register(mushroomIngredient, mushroomIngredient, (ingredientA, ingredientB) =>
        {
            TonicAction? tonicAction;
            TonicType? tonicType;

            tonicAction = TryParseTonicAction(ingredientA.specialInstructions);
            tonicAction ??= TryParseTonicAction(ingredientB.specialInstructions);

            tonicType = TryParseTonicType(ingredientA.specialInstructions);
            tonicType ??= TryParseTonicType(ingredientB.specialInstructions);

            if (tonicAction == null || tonicType == null) { return new ChemistryIngredient(LethalDiseasesKeys.TestTube); }

            int tonicDefIndex = RegisteredTonicDefinitions.IndexOf(RegisteredTonicDefinitions.Where(x => x.tonicType == tonicType && x.tonicAction == tonicAction && !x.wild).GetRandom(Utils.randomGlobal));

            return new ChemistryIngredient(LethalDiseasesKeys.Tonic, specialInstructions: tonicDefIndex.ToString());
        });

        //// Symptoms

        // Add
        Register("50% chance to add a good or neutral symptom", TonicAction.Add, TonicType.Symptom, (disease) => Utils.randomGlobal.NextBool() ? disease.AddRandomSymptom(SymptomType.Good) : disease.AddRandomSymptom(SymptomType.Neutral));
        Register("Adds a random good symptom", TonicAction.Add, TonicType.Symptom, (disease) => disease.AddRandomSymptom(SymptomType.Good));

        Register("50% chance to add a bad or neutral symptom", TonicAction.Add, TonicType.Symptom, (disease) => Utils.randomGlobal.NextBool() ? disease.AddRandomSymptom(SymptomType.Bad) : disease.AddRandomSymptom(SymptomType.Neutral));
        Register("Adds a random bad symptom", TonicAction.Add, TonicType.Symptom, (disease) => disease.AddRandomSymptom(SymptomType.Bad));

        Register("Adds a random neutral symptom", TonicAction.Add, TonicType.Symptom, (disease) => disease.AddRandomSymptom(SymptomType.Neutral));

        // Remove
        Register("50% chance to remove a good or neutral symptom", TonicAction.Remove, TonicType.Symptom, (disease) => Utils.randomGlobal.NextBool() ? disease.RemoveRandomSymptom(SymptomType.Good) : disease.RemoveRandomSymptom(SymptomType.Neutral));
        Register("Removes a random good symptom", TonicAction.Remove, TonicType.Symptom, (disease) => disease.RemoveRandomSymptom(SymptomType.Good));

        Register("50% chance to remove a bad or neutral symptom", TonicAction.Remove, TonicType.Symptom, (disease) => Utils.randomGlobal.NextBool() ? disease.RemoveRandomSymptom(SymptomType.Bad) : disease.RemoveRandomSymptom(SymptomType.Neutral));
        Register("Removes a random bad symptom", TonicAction.Remove, TonicType.Symptom, (disease) => disease.RemoveRandomSymptom(SymptomType.Bad));

        Register("Removes a random neutral symptom", TonicAction.Remove, TonicType.Symptom, (disease) => disease.RemoveRandomSymptom(SymptomType.Neutral));

        // Replace
        Register("50% chance to replace a random good symptom with a random bad or neutral symptom", TonicAction.Replace, TonicType.Symptom, (disease) => Utils.randomGlobal.NextBool() ? disease.ReplaceRandomSymptom(SymptomType.Good, SymptomType.Bad) : disease.ReplaceRandomSymptom(SymptomType.Good, SymptomType.Neutral));
        Register("Replaces a random good symptom with a random bad symptom", TonicAction.Replace, TonicType.Symptom, (disease) => disease.ReplaceRandomSymptom(SymptomType.Good, SymptomType.Bad));

        Register("50% chance to replace a random bad symptom with a random good or neutral symptom", TonicAction.Replace, TonicType.Symptom, (disease) => Utils.randomGlobal.NextBool() ? disease.ReplaceRandomSymptom(SymptomType.Bad, SymptomType.Good) : disease.ReplaceRandomSymptom(SymptomType.Bad, SymptomType.Neutral));
        Register("Replaces a random bad symptom with a random good symptom", TonicAction.Replace, TonicType.Symptom, (disease) => disease.ReplaceRandomSymptom(SymptomType.Bad, SymptomType.Good));

        Register("Replaces a random neutral symptom with a random good symptom", TonicAction.Replace, TonicType.Symptom, (disease) => disease.ReplaceRandomSymptom(SymptomType.Neutral, SymptomType.Good));
        Register("Replaces a random neutral symptom with a random bad symptom", TonicAction.Replace, TonicType.Symptom, (disease) => disease.ReplaceRandomSymptom(SymptomType.Neutral, SymptomType.Bad));

        // Wild
        Register("Adds a random symptom", TonicAction.Add, TonicType.Symptom, (disease) => disease.AddRandomSymptom(), wild: true);

        Register("Removes a random symptom", TonicAction.Remove, TonicType.Symptom, (disease) => disease.RemoveRandomSymptom(), wild: true);

        Register("Replaces a random symptom with another random symptom", TonicAction.Replace, TonicType.Symptom, (disease) => disease.ReplaceRandomSymptom(), wild: true);

        //// Stats

        // Add
        Register("Increases strength by 10%", TonicAction.Add, TonicType.Stat, (disease) => { disease.strength *= 1.1f; return disease; });
        Register("Increases stability by 10%", TonicAction.Add, TonicType.Stat, (disease) => { disease.stability *= 1.1f; return disease; });
        Register("Increases latency by 10%", TonicAction.Add, TonicType.Stat, (disease) => { disease.latency *= 1.1f; return disease; });
        Register("Increases transmissibility by 10%", TonicAction.Add, TonicType.Stat, (disease) => { disease.transmissibility *= 1.1f; return disease; });

        Register("Increases strength by 15%", TonicAction.Add, TonicType.Stat, (disease) => { disease.strength *= 1.15f; return disease; });
        Register("Increases stability by 15%", TonicAction.Add, TonicType.Stat, (disease) => { disease.stability *= 1.15f; return disease; });
        Register("Increases latency by 15%", TonicAction.Add, TonicType.Stat, (disease) => { disease.latency *= 1.15f; return disease; });
        Register("Increases transmissibility by 15%", TonicAction.Add, TonicType.Stat, (disease) => { disease.transmissibility *= 1.15f; return disease; });

        // Remove
        Register("Decreases strength by 10%", TonicAction.Remove, TonicType.Stat, (disease) => { disease.strength *= 0.9f; return disease; });
        Register("Decreases stability by 10%", TonicAction.Remove, TonicType.Stat, (disease) => { disease.stability *= 0.9f; return disease; });
        Register("Decreases latency by 10%", TonicAction.Remove, TonicType.Stat, (disease) => { disease.latency *= 0.9f; return disease; });
        Register("Decreases transmissibility by 10%", TonicAction.Remove, TonicType.Stat, (disease) => { disease.transmissibility *= 0.9f; return disease; });

        Register("Decreases strength by 15%", TonicAction.Remove, TonicType.Stat, (disease) => { disease.strength *= 0.85f; return disease; });
        Register("Decreases stability by 15%", TonicAction.Remove, TonicType.Stat, (disease) => { disease.stability *= 0.85f; return disease; });
        Register("Decreases latency by 15%", TonicAction.Remove, TonicType.Stat, (disease) => { disease.latency *= 0.85f; return disease; });
        Register("Decreases transmissibility by 15%", TonicAction.Remove, TonicType.Stat, (disease) => { disease.transmissibility *= 0.85f; return disease; });

        // Replace
        // Strength ↑, Stability ↓
        Register("Increases strength by 10%, but decreases stability by 10%", TonicAction.Replace, TonicType.Stat, (disease) => { disease.strength *= 1.1f; disease.stability *= 0.9f; return disease; });
        Register("Increases strength by 15%, but decreases stability by 15%", TonicAction.Replace, TonicType.Stat, (disease) => { disease.strength *= 1.15f; disease.stability *= 0.85f; return disease; });

        // Strength ↑, Latency ↓
        Register("Increases strength by 10%, but decreases latency by 10%", TonicAction.Replace, TonicType.Stat, (disease) => { disease.strength *= 1.1f; disease.latency *= 0.9f; return disease; });
        Register("Increases strength by 15%, but decreases latency by 15%", TonicAction.Replace, TonicType.Stat, (disease) => { disease.strength *= 1.15f; disease.latency *= 0.85f; return disease; });

        // Strength ↑, Transmissibility ↓
        Register("Increases strength by 10%, but decreases transmissibility by 10%", TonicAction.Replace, TonicType.Stat, (disease) => { disease.strength *= 1.1f; disease.transmissibility *= 0.9f; return disease; });
        Register("Increases strength by 15%, but decreases transmissibility by 15%", TonicAction.Replace, TonicType.Stat, (disease) => { disease.strength *= 1.15f; disease.transmissibility *= 0.85f; return disease; });

        // Stability ↑, Strength ↓
        Register("Increases stability by 10%, but decreases strength by 10%", TonicAction.Replace, TonicType.Stat, (disease) => { disease.stability *= 1.1f; disease.strength *= 0.9f; return disease; });
        Register("Increases stability by 15%, but decreases strength by 15%", TonicAction.Replace, TonicType.Stat, (disease) => { disease.stability *= 1.15f; disease.strength *= 0.85f; return disease; });

        // Stability ↑, Latency ↓
        Register("Increases stability by 10%, but decreases latency by 10%", TonicAction.Replace, TonicType.Stat, (disease) => { disease.stability *= 1.1f; disease.latency *= 0.9f; return disease; });
        Register("Increases stability by 15%, but decreases latency by 15%", TonicAction.Replace, TonicType.Stat, (disease) => { disease.stability *= 1.15f; disease.latency *= 0.85f; return disease; });

        // Stability ↑, Transmissibility ↓
        Register("Increases stability by 10%, but decreases transmissibility by 10%", TonicAction.Replace, TonicType.Stat, (disease) => { disease.stability *= 1.1f; disease.transmissibility *= 0.9f; return disease; });
        Register("Increases stability by 15%, but decreases transmissibility by 15%", TonicAction.Replace, TonicType.Stat, (disease) => { disease.stability *= 1.15f; disease.transmissibility *= 0.85f; return disease; });

        // Latency ↑, Strength ↓
        Register("Increases latency by 10%, but decreases strength by 10%", TonicAction.Replace, TonicType.Stat, (disease) => { disease.latency *= 1.1f; disease.strength *= 0.9f; return disease; });
        Register("Increases latency by 15%, but decreases strength by 15%", TonicAction.Replace, TonicType.Stat, (disease) => { disease.latency *= 1.15f; disease.strength *= 0.85f; return disease; });

        // Latency ↑, Stability ↓
        Register("Increases latency by 10%, but decreases stability by 10%", TonicAction.Replace, TonicType.Stat, (disease) => { disease.latency *= 1.1f; disease.stability *= 0.9f; return disease; });
        Register("Increases latency by 15%, but decreases stability by 15%", TonicAction.Replace, TonicType.Stat, (disease) => { disease.latency *= 1.15f; disease.stability *= 0.85f; return disease; });

        // Latency ↑, Transmissibility ↓
        Register("Increases latency by 10%, but decreases transmissibility by 10%", TonicAction.Replace, TonicType.Stat, (disease) => { disease.latency *= 1.1f; disease.transmissibility *= 0.9f; return disease; });
        Register("Increases latency by 15%, but decreases transmissibility by 15%", TonicAction.Replace, TonicType.Stat, (disease) => { disease.latency *= 1.15f; disease.transmissibility *= 0.85f; return disease; });

        // Transmissibility ↑, Strength ↓
        Register("Increases transmissibility by 10%, but decreases strength by 10%", TonicAction.Replace, TonicType.Stat, (disease) => { disease.transmissibility *= 1.1f; disease.strength *= 0.9f; return disease; });
        Register("Increases transmissibility by 15%, but decreases strength by 15%", TonicAction.Replace, TonicType.Stat, (disease) => { disease.transmissibility *= 1.15f; disease.strength *= 0.85f; return disease; });

        // Transmissibility ↑, Stability ↓
        Register("Increases transmissibility by 10%, but decreases stability by 10%", TonicAction.Replace, TonicType.Stat, (disease) => { disease.transmissibility *= 1.1f; disease.stability *= 0.9f; return disease; });
        Register("Increases transmissibility by 15%, but decreases stability by 15%", TonicAction.Replace, TonicType.Stat, (disease) => { disease.transmissibility *= 1.15f; disease.stability *= 0.85f; return disease; });

        // Transmissibility ↑, Latency ↓
        Register("Increases transmissibility by 10%, but decreases latency by 10%", TonicAction.Replace, TonicType.Stat, (disease) => { disease.transmissibility *= 1.1f; disease.latency *= 0.9f; return disease; });
        Register("Increases transmissibility by 15%, but decreases latency by 15%", TonicAction.Replace, TonicType.Stat, (disease) => { disease.transmissibility *= 1.15f; disease.latency *= 0.85f; return disease; });

        // Wild
        Register("Increases strength by a random percent", TonicAction.Add, TonicType.Stat, (disease) => { disease.strength *= (1 + Utils.randomGlobal.NextFloat(0f, 1f)); return disease; }, wild: true);
        Register("Increases stability by a random percent", TonicAction.Add, TonicType.Stat, (disease) => { disease.stability *= (1 + Utils.randomGlobal.NextFloat(0f, 1f)); return disease; }, wild: true);
        Register("Increases latency by a random percent", TonicAction.Add, TonicType.Stat, (disease) => { disease.latency *= (1 + Utils.randomGlobal.NextFloat(0f, 1f)); return disease; }, wild: true);
        Register("Increases transmissibility by a random percent", TonicAction.Add, TonicType.Stat, (disease) => { disease.transmissibility *= (1 + Utils.randomGlobal.NextFloat(0f, 1f)); return disease; }, wild: true);

        Register("Decreases strength by a random percent", TonicAction.Add, TonicType.Stat, (disease) => { disease.strength *= (1 - Utils.randomGlobal.NextFloat(0f, 1f)); return disease; }, wild: true);
        Register("Decreases stability by a random percent", TonicAction.Add, TonicType.Stat, (disease) => { disease.stability *= (1 - Utils.randomGlobal.NextFloat(0f, 1f)); return disease; }, wild: true);
        Register("Decreases latency by a random percent", TonicAction.Add, TonicType.Stat, (disease) => { disease.latency *= (1 - Utils.randomGlobal.NextFloat(0f, 1f)); return disease; }, wild: true);
        Register("Decreases transmissibility by a random percent", TonicAction.Add, TonicType.Stat, (disease) => { disease.transmissibility *= (1 - Utils.randomGlobal.NextFloat(0f, 1f)); return disease; }, wild: true);

        Register("Increases a random disease stat by a random percent", TonicAction.Add, TonicType.Stat, (disease) =>
        {
            Action[] stats =
            {
                () => disease.strength *= 1 + Utils.randomGlobal.NextFloat(0f, 1f),
                () => disease.stability *= 1 + Utils.randomGlobal.NextFloat(0f, 1f),
                () => disease.latency *= 1 + Utils.randomGlobal.NextFloat(0f, 1f),
                () => disease.transmissibility *= 1 + Utils.randomGlobal.NextFloat(0f, 1f)
            };

            stats[Utils.randomGlobal.Next(0, stats.Length)]();

            return disease;
        }, wild: true);

        Register("Decreases a random disease stat by a random percent", TonicAction.Add, TonicType.Stat, (disease) =>
        {
            Action[] stats =
            {
                () => disease.strength *= 1 - Utils.randomGlobal.NextFloat(0f, 1f),
                () => disease.stability *= 1 - Utils.randomGlobal.NextFloat(0f, 1f),
                () => disease.latency *= 1 - Utils.randomGlobal.NextFloat(0f, 1f),
                () => disease.transmissibility *= 1 - Utils.randomGlobal.NextFloat(0f, 1f)
            };

            stats[Utils.randomGlobal.Next(0, stats.Length)]();

            return disease;
        }, wild: true);

        Register("Increases or decrease a random disease stat by a random percent", TonicAction.Replace, TonicType.Stat, (disease) =>
        {
            Action[] increaseStats =
            {
                () => disease.strength *= 1 + Utils.randomGlobal.NextFloat(0f, 1f),
                () => disease.stability *= 1 + Utils.randomGlobal.NextFloat(0f, 1f),
                () => disease.latency *= 1 + Utils.randomGlobal.NextFloat(0f, 1f),
                () => disease.transmissibility *= 1 + Utils.randomGlobal.NextFloat(0f, 1f)
            };
            Action[] decreaseStats =
            {
                () => disease.strength *= 1 - Utils.randomGlobal.NextFloat(0f, 1f),
                () => disease.stability *= 1 - Utils.randomGlobal.NextFloat(0f, 1f),
                () => disease.latency *= 1 - Utils.randomGlobal.NextFloat(0f, 1f),
                () => disease.transmissibility *= 1 - Utils.randomGlobal.NextFloat(0f, 1f)
            };

            if (Utils.randomGlobal.NextBool()) increaseStats[Utils.randomGlobal.Next(0, increaseStats.Length)]();
            else decreaseStats[Utils.randomGlobal.Next(0, decreaseStats.Length)]();

            return disease;
        }, wild: true);

        Register("Increases/decreases all disease stats by a random amount", TonicAction.Replace, TonicType.Stat, (disease) =>
        {
            Action[] increaseStats =
            {
                () => disease.strength *= 1 + Utils.randomGlobal.NextFloat(0f, 1f),
                () => disease.stability *= 1 + Utils.randomGlobal.NextFloat(0f, 1f),
                () => disease.latency *= 1 + Utils.randomGlobal.NextFloat(0f, 1f),
                () => disease.transmissibility *= 1 + Utils.randomGlobal.NextFloat(0f, 1f)
            };
            Action[] decreaseStats =
            {
                () => disease.strength *= 1 - Utils.randomGlobal.NextFloat(0f, 1f),
                () => disease.stability *= 1 - Utils.randomGlobal.NextFloat(0f, 1f),
                () => disease.latency *= 1 - Utils.randomGlobal.NextFloat(0f, 1f),
                () => disease.transmissibility *= 1 - Utils.randomGlobal.NextFloat(0f, 1f)
            };

            for (int i = 0; i < 4; i++)
            {
                if (Utils.randomGlobal.NextBool()) increaseStats[i].Invoke();
                else decreaseStats[i].Invoke();
            }

            return disease;
        }, wild: true);

        //// Transmission Types // TODO
        
        // Adding
        Register("Adds the airborne transmission type", TonicAction.Add, TonicType.TransmissionType, (disease) => disease.AddTransmissionType(TransmissionType.Airborne));
        Register("Adds the contact transmission type", TonicAction.Add, TonicType.TransmissionType, (disease) => disease.AddTransmissionType(TransmissionType.Contact));
        Register("Adds the blood transmission type", TonicAction.Add, TonicType.TransmissionType, (disease) => disease.AddTransmissionType(TransmissionType.Blood));
        Register("Adds the foodborne transmission type", TonicAction.Add, TonicType.TransmissionType, (disease) => disease.AddTransmissionType(TransmissionType.Foodborne));

        // Removing
        Register("Removes the airborne transmission type", TonicAction.Remove, TonicType.TransmissionType, (disease) => disease.RemoveTransmissionType(TransmissionType.Airborne));
        Register("Removes the contact transmission type", TonicAction.Remove, TonicType.TransmissionType, (disease) => disease.RemoveTransmissionType(TransmissionType.Contact));
        Register("Removes the blood transmission type", TonicAction.Remove, TonicType.TransmissionType, (disease) => disease.RemoveTransmissionType(TransmissionType.Blood));
        Register("Removes the foodborne transmission type", TonicAction.Remove, TonicType.TransmissionType, (disease) => disease.RemoveTransmissionType(TransmissionType.Foodborne));

        // Replacing
        Register("Replaces the airborne transmission type with a random transmission type", TonicAction.Replace, TonicType.TransmissionType, (disease) => disease.ReplaceTransmissionType(TransmissionType.Airborne));
        Register("Replaces the contact transmission type with a random transmission type", TonicAction.Replace, TonicType.TransmissionType, (disease) => disease.ReplaceTransmissionType(TransmissionType.Contact));
        Register("Replaces the blood transmission type with a random transmission type", TonicAction.Replace, TonicType.TransmissionType, (disease) => disease.ReplaceTransmissionType(TransmissionType.Blood));
        Register("Replaces the foodborne transmission type with a random transmission type", TonicAction.Replace, TonicType.TransmissionType, (disease) => disease.ReplaceTransmissionType(TransmissionType.Foodborne));

        Register("Adds a random transmission type", TonicAction.Add, TonicType.TransmissionType, (disease) => disease.AddRandomTransmissionType(), wild: true);
        Register("Removes a random transmission type", TonicAction.Remove, TonicType.TransmissionType, (disease) => disease.RemoveRandomTransmissionType(), wild: true);
        Register("Replaces a random transmission type", TonicAction.Replace, TonicType.TransmissionType, (disease) => disease.ReplaceRandomTransmissionType(), wild: true);
    }
}