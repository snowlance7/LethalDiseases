using GameNetcodeStuff;
using HarmonyLib;
using SnowyLib;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;
using static LethalDiseases.SymptomAffectedObjects;

namespace LethalDiseases.Symptoms
{
    internal static partial class Symptoms
    {
        [Symptom("Insanity", "Sets insanity to max", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Insanity(Disease disease)
        {
            if (disease.player != null)
                networkHandler.AddSymptomAffectedObjectServerRpc("Insanity", disease.player.NetworkObject);
            return new OnRemoveActionEffect(() =>
            {
                if (disease.player == null) { return; }
                networkHandler.RemoveSymptomAffectedObjectServerRpc("Insanity", disease.player.NetworkObject);
            }, disease.id, "Insanity", disease.strengthTime, SetHighestDurationAndDeny);
        }

        public static void InsanityUpdate(float deltaTime)
        {
            foreach (var player in symptomAffectedPlayers["Insanity"])
            {
                if (player == null) { continue; }
                player.insanityLevel = 50;
            }
        }
    }
}
