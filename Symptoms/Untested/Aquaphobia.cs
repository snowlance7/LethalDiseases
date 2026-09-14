using GameNetcodeStuff;
using SnowyLib;
using UnityEngine;

namespace LethalDiseases.Symptoms
{
    internal static partial class Symptoms
    {
        [Symptom("Aquaphobia", "You fear water", Symptom.SymptomType.Neutral, 50)]
        public static StatusEffect Aquaphobia(Disease disease)
        {
            LayerMask mask = LayerMask.GetMask("Water");

            return new IntervalActionEffect(0.2f, () =>
            {
                PlayerControllerB? player = disease.player;
                if (player == null) { return; }

                if (Physics.Raycast(player.gameplayCamera.transform.position, player.gameplayCamera.transform.forward, 100f, mask) || (!player.isInsideFactory && (RoundManager.Instance.currentLevel.currentWeather == LevelWeatherType.Stormy || RoundManager.Instance.currentLevel.currentWeather == LevelWeatherType.Rainy || RoundManager.Instance.currentLevel.currentWeather == LevelWeatherType.Flooded)))
                {
                    player.JumpToFearLevel(1f);
                }

            }, disease.ToString(), "Aquaphobia", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }
}
