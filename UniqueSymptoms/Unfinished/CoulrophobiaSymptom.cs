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
    internal static class LocalPlayerAffectedSymptom
    {
        internal static bool localPlayerAffected;

        [Symptom("Coulrophobia", "Dont go inside", Symptom.SymptomType.Bad, 5)] // TODO: Make the jester spawn and target the player when they go inside
        public static StatusEffect Coulrophobia(Disease disease)
        {
            if (disease.player != null)
                localPlayerAffected = true;
            return new OnRemoveActionEffect(() =>
            {
                if (disease.player == null) { return; }
                localPlayerAffected = false;
            }, disease.id, "Coulrophobia", disease.strengthTime, Symptoms.SetHighestDurationAndDeny);
        }
    }

    [HarmonyPatch]
    public class CoulrophobiaSymptomPatches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.CheckLineOfSightForPlayer))]
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
