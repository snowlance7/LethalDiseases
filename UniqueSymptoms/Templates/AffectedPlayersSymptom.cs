/*using GameNetcodeStuff;
using HarmonyLib;
using SnowyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.UniqueSymptoms
{
    internal static class _Symptom
    {
        const string symptomName = "";
        public static HashSet<PlayerControllerB> affectedPlayers = SymptomAffectedPlayers.symptomAffectedPlayers[symptomName];

        [Symptom(symptomName, "", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect _(Disease disease)
        {
            if (disease.player != null)
                LethalDiseasesNetworkHandler.Instance.AddSymptomAffectedPlayerServerRpc(symptomName, disease.player.actualClientId);
            return new OnRemoveActionEffect(() =>
            {
                if (disease.player == null) { return; }
                LethalDiseasesNetworkHandler.Instance.RemoveSymptomAffectedPlayerServerRpc(symptomName, disease.player.actualClientId);
            }, disease.id, symptomName, disease.strengthTime, Symptoms.SetHighestDurationAndDeny);
        }

        public static void Update(float deltaTime)
        {
            foreach (var player in affectedPlayers)
            {
                if (player == null) { continue; }

            }
        }
    }

    [HarmonyPatch]
    internal static class _SymptomPatches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.CheckLineOfSightForPlayer))]
        static void CheckLineOfSightForPlayerPostFix(EnemyAI __instance, ref PlayerControllerB __result, float width, int range, int proximityAwareness)
        {
            try
            {

            }
            catch (Exception e)
            {
                logger.LogError(e);
                return;
            }
        }
    }
}*/