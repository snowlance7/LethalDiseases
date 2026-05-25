using HarmonyLib;
using SnowyLib;
using System;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.UniqueSymptoms
{
    internal static class MalfunctionSymptom
    {
        public const string symptomName = "Malfunction";

        public static bool localPlayerAffected;

        [Symptom(symptomName, "Your scanner doesnt work", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Malfunction(Disease disease)
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
    public class MalfunctionSymptomPatches
    {
        [HarmonyPrefix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.PingScan_performed))]
        static bool HUDManager_PingScan_performedPreFix()
        {
            try
            {
                return !MalfunctionSymptom.localPlayerAffected;
            }
            catch (Exception e)
            {
                logger.LogError(e);
                return true;
            }
        }
    }
}
