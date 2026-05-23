using HarmonyLib;
using SnowyLib;
using System;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.UniqueSymptoms
{
    internal static class WeaknessSymptom
    {
        static float previousBaseWeight;
        static float previousWeight;

        [Symptom("Weakness", "Your carry weight is doubled", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Weakness(Disease disease) // TODO: test
        {
            if (disease.player != null)
            {
                previousBaseWeight = disease.player.carryWeight;
                previousWeight = previousBaseWeight * 2;
                disease.player.carryWeight = previousWeight;
                StartOfRound.Instance.ChangedCarryWeight += OnWeightChangedLocalClient;
            }
            return new OnRemoveActionEffect(() =>
            {
                if (disease.player == null) { return; }
                StartOfRound.Instance.ChangedCarryWeight -= OnWeightChangedLocalClient;
                disease.player.carryWeight = previousBaseWeight;
            }, disease.id, "Weakness", disease.strengthTime, Symptoms.SetHighestDurationAndDeny);
        }

        static void OnWeightChangedLocalClient()
        {
            float addedWeight = localPlayer.carryWeight - previousWeight;
            previousBaseWeight += addedWeight;
            localPlayer.carryWeight = previousBaseWeight * 2;
            previousWeight = localPlayer.carryWeight;
        }
    }
}
