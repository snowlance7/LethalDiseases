using Dawn;
using SnowyLib;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items.Mushrooms;

internal class BellcapMushroom : Mushroom // Panaeolus_Papilionaceus_Mushroom (Bellcap) (Adamance after rain)
{
    [StaticInit]
    public static void Init()
    {
        if (!IsServerOrHost) { return; }
        PostGenSpawnableObject.Register(LethalDiseasesKeys.BellcapMushroom, () =>
        {
            if (RoundManager.Instance.currentLevel.GetDawnInfo().TypedKey == MoonKeys.Adamance && RoundManager.Instance.currentLevel.currentWeather == LevelWeatherType.Rainy) { return UnityEngine.Random.Range(15, 30); }
            return 0;
        }, () => GetRandomOutsideSpawn());
    }

    public override void OnEatMushroom()
    {
        localPlayer.HealPlayerAndSync(1);
    }
}