using Dawn;
using SnowyCraftingCore;
using SnowyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items.Mushrooms;

internal class BeechMushroom : Mushroom // Beech_Mushroom: Base of trees in vow
{
    public static NamespacedKey<DawnMoonInfo>? currentLevelKey;
    public static List<GameObject> availableTrees = [];

    [StaticInit]
    public static void Init()
    {
        if (!IsServerOrHost) { return; }
        PostGenSpawnableObject.Register(LethalDiseasesKeys.BeechMushroom, () =>
        {
            currentLevelKey = RoundManager.Instance.currentLevel.GetDawnInfo().TypedKey;
            if (currentLevelKey == LethalContent.Moons[MoonKeys.March].TypedKey) { return UnityEngine.Random.Range(10, 15); }
            if (currentLevelKey == LethalContent.Moons[MoonKeys.Vow].TypedKey) { return UnityEngine.Random.Range(10, 30); }
            return 0;
        }, () =>
        {
            if (availableTrees.Count == 0)
            {
                if (currentLevelKey == LethalContent.Moons[MoonKeys.March].TypedKey)
                {
                    availableTrees = GameObject.FindGameObjectsWithTag("Tree").Where(x => x.name.StartsWith("WychElmB")).ToList();
                }
                else
                {
                    availableTrees = GameObject.FindGameObjectsWithTag("Tree").ToList();
                }
            }

            if (availableTrees.Count == 0) { return null; }
            // TODO: Continue here
        });
    }

    public override ChemistryIngredient? GetIngredient()
    {
        return new ChemistryIngredient(LethalDiseasesKeys.Mushroom, Color.grey, "Add");
    }

    public override void OnEatMushroom()
    {
        throw new NotImplementedException();
    }
}