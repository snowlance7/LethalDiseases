using GameNetcodeStuff;
using HarmonyLib;
using SnowyLib;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.UniqueSymptoms
{
    internal static class InsanitySymptom
    {
        public const string symptomName = "Insanity";

        public static HashSet<PlayerControllerB> affectedPlayers => SymptomAffectedObjects.symptomAffectedPlayers[symptomName];

        [Symptom(symptomName, "Sets insanity to max", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Insanity(Disease disease)
        {
            if (disease.player != null)
                LethalDiseasesNetworkHandler.Instance.AddSymptomAffectedObjectServerRpc(symptomName, disease.player.NetworkObject);
            return new OnRemoveActionEffect(() =>
            {
                if (disease.player == null) { return; }
                LethalDiseasesNetworkHandler.Instance.RemoveSymptomAffectedObjectServerRpc(symptomName, disease.player.NetworkObject);
            }, disease.id, symptomName, disease.strengthTime, Symptoms.SetHighestDurationAndDeny);
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
