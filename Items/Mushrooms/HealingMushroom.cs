using SnowyLib;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items.Mushrooms;

internal class HealingMushroom : Mushroom
{
    public override void OnEatMushroom()
    {
        localPlayer.HealPlayerAndSync(10);
    }
}