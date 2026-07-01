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
        [Symptom("Scaly", "Lizards follow you", Symptom.SymptomType.Neutral, 50)]
        public static StatusEffect Scaly(Disease disease)
        {
            if (disease.player != null)
                LethalDiseasesNetworkHandler.Instance.AddSymptomAffectedObjectRpc("Scaly", disease.player.NetworkObject);
            return new OnRemoveActionEffect(() =>
            {
                if (disease.player == null) { return; }
                LethalDiseasesNetworkHandler.Instance.RemoveSymptomAffectedObjectRpc("Scaly", disease.player.NetworkObject);
            }, disease.id, "Scaly", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }

    [HarmonyPatch]
    internal static class ScalySymptomPatches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(FlowerSnakeEnemy), nameof(FlowerSnakeEnemy.DoAIInterval))]
        static void FlowerSnakeEnemy_DoAIIntervalPostFix(FlowerSnakeEnemy __instance)
        {
            try
            {
                if (!IsServerOrHost || symptomAffectedPlayers["Scaly"].Count == 0 || __instance.clingingToPlayer != null || __instance.leaping) { return; }
                PlayerControllerB? closestPlayer = symptomAffectedPlayers["Scaly"].GetClosestToPosition(__instance.transform.position, (x) => x.transform.position);
                if (closestPlayer == null) { return; }
                __instance.SetMovingTowardsTargetPlayer(closestPlayer);
            }
            catch (Exception e)
            {
                logger.LogError(e);
                return;
            }
        }
    }
}
