/*using Dawn.Utils;
using GameNetcodeStuff;
using HarmonyLib;
using SnowyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Symptoms.Unfinished
{
    internal static class _Symptom
    {
        const string symptomName = "";
        internal static bool localPlayerAffected;

        [Symptom(symptomName, "", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect _(Disease disease)
        {
            if (disease.player != null)
                localPlayerAffected = true;
            return new OnRemoveActionEffect(() =>
            {
                if (disease.player == null) { return; }
                localPlayerAffected = false;
            }, disease.id, symptomName, disease.strengthTime, Symptoms.SetHighestDurationAndDeny);
        }

        public static void Update(float deltaTime)
        {

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