using SnowyLib;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items.Mushrooms;

internal class BlackTrumpetMushroom : Mushroom // Black_Trumpet Mushroom (All over the ground outside in Adamance)
{
    public override void OnEatMushroom()
    {
        localPlayer.HealPlayerAndSync(1);
    }
}