using Dawn.Utils;
using GameNetcodeStuff;
using HarmonyLib;
using SnowyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static LethalDiseases.Plugin;

namespace LethalDiseases.UniqueSymptoms.Unfinished
{
    internal static class ScopophobiaSymptom
    {
        public const string symptomName = "Scopophobia";

        internal static bool localPlayerAffected;

        [Symptom(symptomName, "They are watching", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Scopophobia(Disease disease)
        {
            if (disease.player != null)
                localPlayerAffected = true;
            return new OnRemoveActionEffect(() =>
            {
                if (disease.player == null) { return; }
                localPlayerAffected = false;
            }, disease.id, symptomName, disease.strengthTime, Symptoms.SetHighestDurationAndDeny);
        }
    }

    [HarmonyPatch]
    internal static class ScopophobiaSymptomPatches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.CheckLineOfSightForPlayer))] // TODO: make all enemies stare at the player if they are looking at them
        static void CheckLineOfSightForPlayerPostFix(EnemyAI __instance, ref PlayerControllerB __result, float width, int range, int proximityAwareness)
        {
            try
            {
                return;
            }
            catch (Exception e)
            {
                logger.LogError(e);
                return;
            }
        }
    }
}
