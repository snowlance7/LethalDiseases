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
        [Symptom("Malfunction", "Your scanner doesnt work", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Malfunction(Disease disease)
        {
            if (disease.player != null)
                localPlayerAffected["Malfunction"] = true;
            return new OnRemoveActionEffect(() =>
            {
                if (disease.player == null) { return; }
                localPlayerAffected["Malfunction"] = false;
            }, disease.id, "Malfunction", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }

    [HarmonyPatch]
    internal static class MalfunctionSymptomPatches
    {
        [HarmonyPrefix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.PingScan_performed))]
        static bool HUDManager_PingScan_performedPreFix()
        {
            try
            {
                return !localPlayerAffected["Malfunction"];
            }
            catch (Exception e)
            {
                logger.LogError(e);
                return true;
            }
        }
    }
}
