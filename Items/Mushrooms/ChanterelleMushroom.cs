using Dawn;
using SnowyLib;
using System.Linq;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items.Mushrooms;

internal class ChanterelleMushroom : Mushroom // Chanterelle_Mushroom (rarely in forested areas)
{
    [StaticInit]
    public static void Init()
    {
        if (!IsServerOrHost) { return; }
        PostGenSpawnableObject.Register(LethalDiseasesKeys.ChanterelleMushroom, () =>
        {
            if (RoundManager.Instance.currentLevel.GetDawnInfo().TypedKey == MoonKeys.Adamance) { return UnityEngine.Random.Range(0, 25); }
            return (int)(Utils.currentLevelTrees.Count() * UnityEngine.Random.Range(0f, 0.1f));
        }, () => GetRandomOutsideSpawn());
    }

    public override void OnEatMushroom()
    {
        return;
    }
}