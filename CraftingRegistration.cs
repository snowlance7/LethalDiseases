using LethalDiseases.Items;
using SnowyCraftingCore;
using SnowyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LethalDiseases;

internal static class CraftingRegistration
{
    [StaticInit]
    public static void Init()
    {
        ChemistryIngredient testTubeIngredient = new ChemistryIngredient(LethalDiseasesKeys.TestTube);
        CraftingAPI.RegisterIngredient(testTubeIngredient);

        ChemistryIngredient cottonSwabIngredient = new ChemistryIngredient(LethalDiseasesKeys.CottonSwab);
        CraftingAPI.RegisterIngredient(cottonSwabIngredient);

        CraftingAPI.RegisterRecipe(new ChemistryRecipe(testTubeIngredient, testTubeIngredient, (ingredientA, ingredientB) =>
        {
            Disease? disease1 = Disease.GetDiseaseFromString(ingredientA.specialInstructions);
            Disease? disease2 = Disease.GetDiseaseFromString(ingredientB.specialInstructions);

            if (disease1 != null && disease2 != null)
            {
                var array1 = disease1.symptoms;
                var array2 = disease2.symptoms;

                var result = array1.Except(array2).Concat(array2.Except(array1)).ToList();
                Disease disease3 = Disease.MergeDiseases(disease1, disease2);
                disease3.symptoms = result;
                return new ChemistryIngredient(LethalDiseasesKeys.TestTube, disease3.GetChemistryLiquidAppearance(), disease3.ToString());
            }
            else if (disease1 != null)
            {
                return ingredientA;
            }
            else if (disease2 != null)
            {
                return ingredientB;
            }
            else
            {
                return null; // TODO
            }
        }));
    }
}