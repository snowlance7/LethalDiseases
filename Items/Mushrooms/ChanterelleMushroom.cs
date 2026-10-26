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
            return (int)(GameObject.FindGameObjectsWithTag("Tree").Count() * UnityEngine.Random.Range(0f, 0.15f));
        }, () => GetRandomOutsideSpawn());
    }

    public override void OnEatMushroom()
    {
        return;
    }
}