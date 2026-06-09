using HarmonyLib;
using SnowyLib;
using System;
using static LethalDiseases.Plugin;
using static LethalDiseases.SymptomAffectedObjects;

namespace LethalDiseases.Symptoms
{
    internal static partial class Symptoms
    {
        [Symptom("ButterFingers", "50/50 chance to drop anything you pick up", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect ButterFingers(Disease disease)
        {
            if (disease.player != null)
                localPlayerAffected["ButterFingers"] = true;
            return new OnRemoveActionEffect(() =>
            {
                if (disease.player == null) { return; }
                localPlayerAffected["ButterFingers"] = false;
            }, disease.id, "ButterFingers", disease.strengthTime, Symptoms.SetHighestDurationAndDeny);
        }
    }

    [HarmonyPatch]
    internal static class ButterFingersSymptomPatches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.GrabItemOnClient))]
        static void GrabbableObject_GrabItemOnClientPostFix(GrabbableObject __instance)
        {
            try
            {
                if (localPlayerAffected["ButterFingers"] && UnityEngine.Random.Range(0, 2) == 1)
                    localPlayer.DiscardHeldObject();
            }
            catch (Exception e)
            {
                logger.LogError(e);
                return;
            }
        }
    }
}
