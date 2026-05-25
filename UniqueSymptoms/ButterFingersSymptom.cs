using HarmonyLib;
using SnowyLib;
using System;
using static LethalDiseases.Plugin;

namespace LethalDiseases.UniqueSymptoms
{
    internal static class ButterFingersSymptom
    {
        public const string symptomName = "Butter Fingers";

        public static bool localPlayerAffected;

        [Symptom(symptomName, "50/50 chance to drop anything you pick up", Symptom.SymptomType.Bad, 100)]
        public static StatusEffect ButterFingers(Disease disease)
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
    public class ButterFingersSymptomPatches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.GrabItemOnClient))]
        static void GrabbableObject_GrabItemOnClientPostFix(GrabbableObject __instance)
        {
            try
            {
                if (ButterFingersSymptom.localPlayerAffected && UnityEngine.Random.Range(0, 2) == 1)
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
