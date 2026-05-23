using GameNetcodeStuff;
using HarmonyLib;
using SnowyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.UniqueSymptoms
{
    internal static class InsanitySymptom
    {
        public static HashSet<PlayerControllerB> affectedPlayers = SymptomAffectedPlayers.symptomAffectedPlayers["Insanity"];

        [Symptom("Insanity", "Sets insanity to max", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Insanity(Disease disease)
        {
            if (disease.player != null)
                LethalDiseasesNetworkHandler.Instance.AddSymptomAffectedPlayerServerRpc("Insanity", disease.player.actualClientId);
            return new OnRemoveActionEffect(() =>
            {
                if (disease.player == null) { return; }
                LethalDiseasesNetworkHandler.Instance.RemoveSymptomAffectedPlayerServerRpc("Insanity", disease.player.actualClientId);
            }, disease.id, "Insanity", disease.strengthTime, Symptoms.SetHighestDurationAndDeny);
        }

        public static void Update(float deltaTime)
        {
            foreach (var player in affectedPlayers)
            {
                if (player == null) { continue; }
                player.insanityLevel = 50;
            }
        }
    }
}
