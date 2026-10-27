using SnowyCraftingCore;
using SnowyLib;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Dawn;
using UnityEngine.AI;

namespace LethalDiseases.Items.Mushrooms;

internal abstract class Mushroom : AttachableObject, IMixableIngredient
{
    public static readonly List<NamespacedKey<DawnItemInfo>> mushroomKeys = [LethalDiseasesKeys.BeechMushroom, LethalDiseasesKeys.BellcapMushroom, LethalDiseasesKeys.BlackTrumpetMushroom, LethalDiseasesKeys.CaveMushroom, LethalDiseasesKeys.ChanterelleMushroom, LethalDiseasesKeys.DeathCapMushroom, LethalDiseasesKeys.Mushroom, LethalDiseasesKeys.JackOLanternMushroom, LethalDiseasesKeys.JellyMushroom, LethalDiseasesKeys.MonsterMushroom, LethalDiseasesKeys.OysterMushroom, LethalDiseasesKeys.RadioMushroom, LethalDiseasesKeys.ShaggyManeMushroom];
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
        AddMushroomType(typeof(BellcapMushroom));
        AddMushroomType(typeof(CaveMushroom));
        AddMushroomType(typeof(ChanterelleMushroom));
        AddMushroomType(typeof(DeathCapMushroom));
        AddMushroomType(typeof(JackOLanternMushroom));
        AddMushroomType(typeof(JellyMushroom));
        AddMushroomType(typeof(MonsterMushroom));
        AddMushroomType(typeof(OysterMushroom));
        AddMushroomType(typeof(RadioMushroom));
        AddMushroomType(typeof(ShaggyManeMushroom));
        AddMushroomType(typeof(BlackTrumpetMushroom));
        AddMushroomType(typeof(HealingMushroom));
    }

    public static void AddMushroomType(Type type)
    {
        if (newTypes.Count == 0)
        {
            MushroomTypes.Add(type, availableTypes.GetRandom(Utils.randomGlobal));
            return;
        }

        string typeString = newTypes.GetRandom(Utils.randomGlobal); // TODO: Test to make sure randomGlobal exists here
        newTypes.Remove(typeString);
        MushroomTypes.Add(type, typeString);
    }

    public static SpawnDefinition GetRandomOutsideSpawn()
    {
        return new SpawnDefinition(RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(Utils.outsideAINodes.GetRandom().transform.position, 30f, default(NavMeshHit), layerMask: -8193), Quaternion.identity, destroyWithScene: true);
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