using GameNetcodeStuff;
using HarmonyLib;
using SnowyLib;
using System;
using static LethalDiseases.Plugin;
using static LethalDiseases.SymptomAffectedObjects;

namespace LethalDiseases.Symptoms
{
    internal static partial class Symptoms
    {
        [Symptom("Scopophobia", "They are watching", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Scopophobia(Disease disease)
        {
            if (disease.player != null)
                localPlayerAffected["Scopophobia"] = true;
            return new OnRemoveActionEffect(() =>
            {
                if (disease.player == null) { return; }
                localPlayerAffected["Scopophobia"] = false;
            }, disease.id, "Scopophobia", disease.strengthTime, SetHighestDurationAndDeny);
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
