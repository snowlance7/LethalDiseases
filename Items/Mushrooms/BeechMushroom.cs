using Dawn;
using SnowyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items.Mushrooms;

internal class BeechMushroom : Mushroom // Beech_Mushroom: Base of trees in vow and rarely in march
{
    public static NamespacedKey<DawnMoonInfo>? currentLevelKey;
    public static List<GameObject> availableTrees = [];

    [StaticInit]
    public static void Init()
    {
        if (!IsServerOrHost) { return; }
        PostGenSpawnableObject.Register(LethalDiseasesKeys.BeechMushroom, () =>
        {
            currentLevelKey = RoundManager.Instance.currentLevel.GetDawnInfo().TypedKey;
            if (currentLevelKey == MoonKeys.March) { return UnityEngine.Random.Range(10, 15); }
            if (currentLevelKey == MoonKeys.Vow) { return UnityEngine.Random.Range(10, 30); }
            return (int)(Utils.currentLevelTrees.Count() * UnityEngine.Random.Range(0f, 0.25f));
        }, () =>
        {
            if (availableTrees.Count == 0)
            {
                if (currentLevelKey == MoonKeys.March)
                {
                    availableTrees = Utils.currentLevelTrees.Where(x => x.name.StartsWith("WychElmB")).ToList();
                }
                else
                {
                    availableTrees = Utils.currentLevelTrees.ToList();
                }
            }

            if (availableTrees.Count == 0) { return null; }

            var tree = availableTrees.GetRandom();
            Vector3 castPosition = Utils.GetRandomPositionInCircle(new Vector3(tree.transform.position.x, tree.transform.position.y + 1f, tree.transform.position.z), 1f, 1.5f);
            Vector3 position = castPosition.GetFloorPosition();
            return new SpawnDefinition(position, Quaternion.identity, destroyWithScene: true);
        }, () =>
        {
            availableTrees.Clear();
            currentLevelKey = null;
        });
    }

    public override void OnEatMushroom()
    {
        localPlayer.HealPlayerAndSync(1);
    }
}