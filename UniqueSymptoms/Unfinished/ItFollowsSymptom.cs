using GameNetcodeStuff;
using HarmonyLib;
using SnowyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.UniqueSymptoms
{
    internal static class ItFollowsSymptom
    {
        static EnemyAI? entity;
        public const string symptomName = "It Follows";

        [Symptom(symptomName, "???", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect ItFollows(Disease disease)
        {
            if (disease.player != null || disease.enemy != null)
                LethalDiseasesNetworkHandler.Instance.AddSymptomAffectedObjectServerRpc(symptomName, disease.networkObject);
            return new OnRemoveActionEffect(() =>
            {
                if (disease.player == null && disease.enemy == null) { return; }
                LethalDiseasesNetworkHandler.Instance.RemoveSymptomAffectedObjectServerRpc(symptomName, disease.networkObject);
            }, disease.id, symptomName, disease.strengthTime, Symptoms.SetHighestDurationAndDeny);
        }

        public static void Update(float deltaTime)
        {


            foreach (var affected in affected)
            {

            }
        }
    }

    [HarmonyPatch]
    public class _SymptomPatches
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
}