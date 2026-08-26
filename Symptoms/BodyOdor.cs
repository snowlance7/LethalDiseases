using GameNetcodeStuff;
using HarmonyLib;
using SnowyLib;
using System;
using System.Linq;
using UnityEngine.Rendering.HighDefinition;
using static LethalDiseases.Plugin;
using static LethalDiseases.SymptomAffectedObjects;

namespace LethalDiseases.Symptoms
{
    internal static partial class Symptoms
    {
        [Symptom("BodyOdor", "Enemies prioritize targetting you first", Symptom.SymptomType.Bad, 100)]
        public static StatusEffect BodyOdor(Disease disease)
        {
            if (disease.player != null)
                networkHandler.AddSymptomAffectedObjectRpc("BodyOdor", disease.player.NetworkObject); // TODO CONTINUE
            return new OnRemoveActionEffect((effect) =>
            {
                if (disease.player == null) { return; }
                networkHandler.RemoveSymptomAffectedObjectRpc("BodyOdor", disease.player.NetworkObject);
            }, disease.ToString(), "BodyOdor", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }

    [HarmonyPatch]
    internal static class BodyOdorSymptomPatches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.CheckLineOfSightForPlayer))]
        static void CheckLineOfSightForPlayerPostFix(EnemyAI __instance, ref PlayerControllerB __result, float width, int range, int proximityAwareness)
        {
            try
            {
                if (__result == null || !IsServerOrHost || symptomAffectedPlayers["BodyOdor"].Count <= 0) { return; }
                PlayerControllerB[] players = __instance.GetAllPlayersInLineOfSight(width, range, __instance.eye, proximityAwareness);

                foreach (var affectedPlayer in symptomAffectedPlayers["BodyOdor"])
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
                if (__result == null || !IsServerOrHost || symptomAffectedPlayers["BodyOdor"].Count <= 0) { return; }
                PlayerControllerB[] players = __instance.GetAllPlayersInLineOfSight(width, range, __instance.eye, proximityAwareness);

                foreach (var affectedPlayer in symptomAffectedPlayers["BodyOdor"])
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
