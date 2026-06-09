using HarmonyLib;
using SnowyLib;
using System;
using UnityEngine;
using static LethalDiseases.Plugin;
using static LethalDiseases.SymptomAffectedObjects;

namespace LethalDiseases.Symptoms
{
    internal static partial class Symptoms
    {
        static float weaknessPreviousBaseWeight;
        static float weaknessPreviousWeight;

        [Symptom("Weakness", "Your carry weight is doubled", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Weakness(Disease disease) // TODO: test
        {
            if (disease.player != null)
            {
                weaknessPreviousBaseWeight = disease.player.carryWeight;
                weaknessPreviousWeight = weaknessPreviousBaseWeight * 2;
                disease.player.carryWeight = weaknessPreviousWeight;
                StartOfRound.Instance.ChangedCarryWeight += OnWeightChangedLocalClient;
            }
            return new OnRemoveActionEffect(() =>
            {
                if (disease.player == null) { return; }
                StartOfRound.Instance.ChangedCarryWeight -= OnWeightChangedLocalClient;
                disease.player.carryWeight = weaknessPreviousBaseWeight;
            }, disease.id, "Weakness", disease.strengthTime, SetHighestDurationAndDeny);
        }

        static void OnWeightChangedLocalClient()
        {
            float addedWeight = localPlayer.carryWeight - weaknessPreviousWeight;
            weaknessPreviousBaseWeight += addedWeight;
            localPlayer.carryWeight = weaknessPreviousBaseWeight * 2;
            weaknessPreviousWeight = localPlayer.carryWeight;
        }
    }
}
