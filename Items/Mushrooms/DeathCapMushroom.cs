using Dawn;
using SnowyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items.Mushrooms;

internal class DeathCapMushroom : Mushroom // Death_Cap_Mushroom (Vow around trees)
{
    public static NamespacedKey<DawnMoonInfo>? currentLevelKey;
    public static List<GameObject> availableTrees = [];

    [StaticInit]
    public static void Init()
    {
        if (!IsServerOrHost) { return; }
        PostGenSpawnableObject.Register(LethalDiseasesKeys.DeathCapMushroom, () =>
        {
            currentLevelKey = RoundManager.Instance.currentLevel.GetDawnInfo().TypedKey;
            if (currentLevelKey == MoonKeys.March) { return UnityEngine.Random.Range(5, 15); }
            if (currentLevelKey == MoonKeys.Vow) { return UnityEngine.Random.Range(5, 30); }
            return 0;
        }, () =>
        {
            if (availableTrees.Count == 0)
            {
                if (currentLevelKey == MoonKeys.March)
                {
                    availableTrees = GameObject.FindGameObjectsWithTag("Tree").Where(x => x.name.StartsWith("WychElmB")).ToList();
                }
                else
                {
                    availableTrees = GameObject.FindGameObjectsWithTag("Tree").ToList();
                }
            }

            if (availableTrees.Count == 0) { return null; }

            var tree = availableTrees.GetRandom();
            Vector3 castPosition = Utils.GetRandomPositionInCircle(tree.transform.position, 1f, 1.5f);
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
        Disease disease = new Disease();
        disease.strengthTime = 0f;
        disease.latencyTime = UnityEngine.Random.Range(300f, 600f);
        disease.stabilityTime = 0f;
        disease.transmissibility = 0f;
        disease.AddSymptom("DeathCapPoisoning");
        localPlayer.InfectAndSync(disease);
    }
}