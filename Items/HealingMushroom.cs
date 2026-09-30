using SnowyLib;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items
{
    internal class HealingMushroom : AttachableObject
    {
        public static int HealingAmount => PluginInstance.Config.Bind("Healing Mushroom Options", "Healing Mushroom | Healing Amount", 10, "How much health the healing mushroom heals when eaten").Value;

        [StaticInit]
        public static void Init()
        {
            _ = HealingAmount;
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