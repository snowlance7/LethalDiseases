using SnowyCraftingCore;
using SnowyLib;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;

namespace LethalDiseases.Items.Mushrooms;

internal abstract class Mushroom : AttachableObject, IMixableIngredient
{
    public static readonly List<string> availableTypes = ["Add", "Remove", "Replace", "Symptom", "Stat", "TransmissionType", "Add", "Remove", "Replace", "Symptom", "Stat", "TransmissionType"];
    public static List<string> newTypes = [];
    public static Dictionary<Type, string> MushroomTypes = [];
    [SerializeField] Color color;

    [OnResetShip]
    public static void SetMushroomTypes()
    {
        MushroomTypes.Clear();

        newTypes = [.. availableTypes];

        AddMushroomType(typeof(BeechMushroom)); // TODO: Continue here
    }

    public static void AddMushroomType(Type type)
    {
        if (newTypes.Count == 0)
        {
            MushroomTypes.Add(type, availableTypes.GetRandom(Utils.randomGlobal));
        }

        string typeString = newTypes.GetRandom(Utils.randomGlobal); // TODO: Test to make sure randomGlobal exists here
        newTypes.Remove(typeString);
        MushroomTypes.Add(type, typeString);
    }

    public bool DespawnItemAfterInput()
    {
        return true;
    }

    public ChemistryIngredient? GetIngredient()
    {
        return new ChemistryIngredient(LethalDiseasesKeys.Mushroom, new ChemistryLiquidAppearance(color, 5f), MushroomTypes[GetType()]);
    }

    public void OnChemicalOutput(ChemistryIngredient ingredient)
    {
        return;
    }

    public abstract void OnEatMushroom();

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (!buttonDown) { return; }

        OnEatMushroom();
        playerHeldBy.DespawnHeldObject();
    }
}