using SnowyLib;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items.Mushrooms
{
    internal class HealingMushroom : AttachableObject
    {
        public static int HealingAmount => PluginInstance.Config.Bind("Healing Mushroom Options", "Healing Mushroom | Healing Amount", 10, "How much health the healing mushroom heals when eaten").Value;

        [StaticInit]
        public static void Init()
        {
            _ = HealingAmount;
        }

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0, 0, 0); // TODO: Set these
            itemProperties.rotationOffset = new Vector3(0, 0, 0);
            itemProperties.floorYOffset = 0;
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!buttonDown) { return; }

            localPlayer.HealPlayerAndSync(HealingAmount);
            localPlayer.DespawnHeldObject();
        }
    }
}