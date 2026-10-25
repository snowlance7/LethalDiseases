using SnowyCraftingCore;
using SnowyLib;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items.Mushrooms
{
    internal class HealingMushroom : Mushroom
    {
        [SerializeField] Color color;

        public static int HealingAmount => PluginInstance.Config.Bind("Healing Mushroom Options", "Healing Mushroom | Healing Amount", 10, "How much health the healing mushroom heals when eaten").Value;

        [StaticInit]
        public static void Init()
        {
            _ = HealingAmount;
        }

        public override ChemistryIngredient? GetIngredient()
        {
            return new ChemistryIngredient(LethalDiseasesKeys.Mushroom, Color.red, "Add");
        }

        public override void OnEatMushroom()
        {
            localPlayer.HealPlayerAndSync(HealingAmount);
        }
    }
}