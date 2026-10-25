using SnowyCraftingCore;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalDiseases.Items.Mushrooms
{
    internal class ShaggyManeMushroom : Mushroom // Shaggy Mane Mushroom (Assurance and offense)
    {
        [SerializeField] Color color;

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
