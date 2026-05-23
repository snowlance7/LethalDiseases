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

namespace LethalDiseases.UniqueSymptoms
{
    internal static class ScalySymptom
    {
        public static HashSet<PlayerControllerB> affectedPlayers = SymptomAffectedPlayers.symptomAffectedPlayers["Scaly"];

        [Symptom("Scaly", "Lizards follow you", Symptom.SymptomType.Neutral, 50)]
        public static StatusEffect Scaly(Disease disease)
        {
            if (disease.player != null)
                LethalDiseasesNetworkHandler.Instance.AddSymptomAffectedPlayerServerRpc("Scaly", disease.player.actualClientId);
            return new OnRemoveActionEffect(() =>
            {
                if (disease.player == null) { return; }
                LethalDiseasesNetworkHandler.Instance.RemoveSymptomAffectedPlayerServerRpc("Scaly", disease.player.actualClientId);
            }, disease.id, "Scaly", disease.strengthTime, Symptoms.SetHighestDurationAndDeny);
        }
    }

    [HarmonyPatch]
    public class ScalySymptomPatches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(FlowerSnakeEnemy), nameof(FlowerSnakeEnemy.DoAIInterval))]
        static void FlowerSnakeEnemy_DoAIIntervalPostFix(FlowerSnakeEnemy __instance)
        {
            try
            {
                if (!IsServerOrHost || ScalySymptom.affectedPlayers.Count == 0 || __instance.clingingToPlayer != null || __instance.leaping) { return; }
                PlayerControllerB? closestPlayer = ScalySymptom.affectedPlayers.GetClosestToPosition(__instance.transform.position, (x) => x.transform.position);
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
