using SnowyCraftingCore;
using SnowyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalDiseases.Items.Mushrooms
{
    internal class TrumpetMushroom : Mushroom // Black_Trumpet Mushroom (All over the ground outside in Adamance)
    {
        [SerializeField] Color color;

        public static void Init()
        {
            //PostGenSpawnableObject.re
        }

        public override ChemistryIngredient? GetIngredient()
        {
            throw new NotImplementedException();
        }

        public override void OnEatMushroom()
        {
            throw new NotImplementedException();
        }
    }
}
