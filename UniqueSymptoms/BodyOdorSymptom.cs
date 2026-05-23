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
    internal static class BodyOdorSymptom
    {
        public static HashSet<PlayerControllerB> affectedPlayers = SymptomAffectedPlayers.symptomAffectedPlayers["Body Odor"];

        [Symptom("Body Odor", "Enemies prioritize targetting you first", Symptom.SymptomType.Bad, 100)]
        public static StatusEffect BodyOdor(Disease disease)
        {
            if (disease.player != null)
                LethalDiseasesNetworkHandler.Instance.AddSymptomAffectedPlayerServerRpc("Body Odor", disease.player.actualClientId); // TODO CONTINUE
            return new OnRemoveActionEffect(() =>
            {
                if (disease.player == null) { return; }
                LethalDiseasesNetworkHandler.Instance.AddSymptomAffectedPlayerServerRpc("Body Odor", disease.player.actualClientId);
            }, disease.id, "Body Odor", disease.strengthTime, Symptoms.SetHighestDurationAndDeny);
        }
    }

    [HarmonyPatch]
    public class BodyOdorSymptomPatches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.CheckLineOfSightForPlayer))]
        static void CheckLineOfSightForPlayerPostFix(EnemyAI __instance, ref PlayerControllerB __result, float width, int range, int proximityAwareness)
        {
            try
            {
                if (__result == null || !IsServerOrHost || BodyOdorSymptom.affectedPlayers.Count <= 0) { return; }
                PlayerControllerB[] players = __instance.GetAllPlayersInLineOfSight(width, range, __instance.eye, proximityAwareness);

                foreach (var affectedPlayer in BodyOdorSymptom.affectedPlayers)
                {
                    if (affectedPlayer == null) { continue; }
                    if (players.Contains(affectedPlayer))
                    {
                        __result = affectedPlayer;
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.CheckLineOfSightForClosestPlayer))]
        static void CheckLineOfSightForClosestPlayerPostFix(EnemyAI __instance, ref PlayerControllerB __result, float width, int range, int proximityAwareness)
        {
            try
            {
                if (__result == null || !IsServerOrHost || BodyOdorSymptom.affectedPlayers.Count <= 0) { return; }
                PlayerControllerB[] players = __instance.GetAllPlayersInLineOfSight(width, range, __instance.eye, proximityAwareness);

                foreach (var affectedPlayer in BodyOdorSymptom.affectedPlayers)
                {
                    if (affectedPlayer == null) { continue; }
                    if (players.Contains(affectedPlayer))
                    {
                        __result = affectedPlayer;
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e);
                return;
            }
        }
    }
}
