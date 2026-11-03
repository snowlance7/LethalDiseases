using SnowyLib;
using System.Linq;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items.Mushrooms;

internal class OysterMushroom : Mushroom
{
    [StaticInit]
    public static void Init()
    {
        if (!IsServerOrHost) { return; }
        PostGenSpawnableObject.Register(LethalDiseasesKeys.OysterMushroom, () =>
        {
            return (int)(Utils.currentLevelTrees.Count() * UnityEngine.Random.Range(0.1f, 0.25f));
        }, () => GetRandomOutsideSpawn());
    }

    public override void OnEatMushroom()
    {
        localPlayer.HealPlayerSynced(1);
    }
}