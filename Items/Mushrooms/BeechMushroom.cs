using SnowyCraftingCore;
using SnowyLib;
using System;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items.Mushrooms;

internal class BeechMushroom : Mushroom // Beech_Mushroom: Base of trees in vow
{
    [StaticInit]
    public static void Init()
    {
        if (!IsServerOrHost) { return; }
        PostGenSpawnableObject.Register(LethalDiseasesKeys.BeechMushroom, )
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