using Dawn;
using SnowyCraftingCore;
using SnowyLib;
using System;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items.Mushrooms
{
    internal class BellcapMushroom : Mushroom // Panaeolus_Papilionaceus_Mushroom (Bellcap) (Adamance after rain)
    {
        [SerializeField] Color color;

        [StaticInit]
        public static void Init()
        {
            if (!IsServerOrHost) { return; }
            PostGenSpawnableObject.Register(LethalDiseasesKeys.BellcapMushroom, () =>
            {
                var currentLevelKey = RoundManager.Instance.currentLevel.GetDawnInfo().TypedKey;
                if (currentLevelKey == LethalContent.Moons[MoonKeys.Adamance].TypedKey && RoundManager.Instance.currentLevel.currentWeather == LevelWeatherType.Rainy) { return UnityEngine.Random.Range(15, 30); }
                return 0;
            }, () =>
            {
                return new SpawnDefinition(RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(Utils.outsideAINodes.GetRandom().transform.position, 15f, default(NavMeshHit), layerMask: -8193), Quaternion.identity, destroyWithScene: true);
            });
        }

        public override ChemistryIngredient? GetIngredient()
        {
            throw new NotImplementedException();
        }

        public override void OnEatMushroom()
        {
            throw new NotImplementedException();
        }
    }
}
