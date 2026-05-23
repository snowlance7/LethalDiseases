using GameNetcodeStuff;
using HarmonyLib;
using SnowyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.UniqueSymptoms
{
    internal static class TerminalRallySymptom
    {
        const string symptomName = "Terminal Rally";
        public static HashSet<EnemyAI> affectedEnemies = new HashSet<EnemyAI>();
        internal static bool localPlayerAffected;

        [Symptom(symptomName, "On death, get 10 seconds of rage and die", Symptom.SymptomType.Good, 50)]
        public static StatusEffect TerminalRally(Disease disease)
        {
            if (disease.player != null)
                localPlayerAffected = true;
            else if (disease.enemy != null)
                affectedEnemies.Add(disease.enemy);

            return new OnRemoveActionEffect(() =>
            {
                if (disease.player != null)
                    localPlayerAffected = false;
                if (disease.enemy != null)
                    affectedEnemies.Remove(disease.enemy);
            }, disease.id, symptomName, disease.strengthTime, Symptoms.SetHighestDurationAndDeny);
        }
    }

    [HarmonyPatch]
    public class TerminalRallySymptomPatches
    {
        [HarmonyPrefix, HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.KillEnemyOnOwnerClient))]
        static bool EnemyAI_KillEnemyOnOwnerClient_Prefix(EnemyAI __instance)
        {
            try
            {
                if (!TerminalRallySymptom.affectedEnemies.Contains(__instance)) { return true; }

                __instance.enemyHP = 10;
                TerminalRallySymptom.affectedEnemies.Remove(__instance);

                __instance.StatusEffectController().ApplyEffect(new OnRemoveActionEffect(() =>
                {
                    __instance.KillEnemyOnOwnerClient();
                }, "Terminal Rally", "Terminal Rally Death", 10f));

                return false;
            }
            catch (Exception e)
            {
                logger.LogError(e);
                return true;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayer))]
        static bool PlayerControllerB_KillPlayer_Prefix(PlayerControllerB __instance)
        {
            try
            {
                if (!TerminalRallySymptom.localPlayerAffected) { return true; }

                __instance.health = 100;
                HUDManager.Instance.UpdateHealthUI(100, false);
                TerminalRallySymptom.localPlayerAffected = false;

                __instance.StatusEffectController().ApplyEffect(new OnRemoveActionEffect(() =>
                {
                    __instance.KillPlayer(Vector3.zero);
                }, "Terminal Rally", "Terminal Rally Death", 10f));

                return false;
            }
            catch (Exception e)
            {
                logger.LogError(e);
                return true;
            }
        }
    }
}