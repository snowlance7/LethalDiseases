using GameNetcodeStuff;
using HarmonyLib;
using SnowyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using static LethalDiseases.Plugin;
using static LethalDiseases.SymptomAffectedObjects;

namespace LethalDiseases.Symptoms
{
    internal static partial class Symptoms
    {
        [Symptom("TerminalRally", "On death, get 10 seconds of rage and die", Symptom.SymptomType.Good, 50)]
        public static StatusEffect TerminalRally(Disease disease)
        {
            if (disease.player != null)
                localPlayerAffected["TerminalRally"] = true;
            else if (disease.enemy != null)
                symptomAffectedEnemies["TerminalRally"].Add(disease.enemy);

            return new OnRemoveActionEffect(() =>
            {
                if (disease.player != null)
                    localPlayerAffected["TerminalRally"] = false;
                if (disease.enemy != null)
                    symptomAffectedEnemies["TerminalRally"].Remove(disease.enemy);
            }, disease.id, "TerminalRally", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }

    [HarmonyPatch]
    internal static class TerminalRallySymptomPatches
    {
        [HarmonyPrefix, HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.KillEnemyOnOwnerClient))]
        static bool EnemyAI_KillEnemyOnOwnerClient_Prefix(EnemyAI __instance)
        {
            try
            {
                if (!symptomAffectedEnemies["TerminalRally"].Contains(__instance)) { return true; }

                __instance.enemyHP = 10;
                symptomAffectedEnemies["TerminalRally"].Remove(__instance);

                __instance.StatusEffectController().ApplyEffect(new OnRemoveActionEffect(() =>
                {
                    __instance.KillEnemyOnOwnerClient();
                }, "TerminalRally", "Terminal Rally Death", 10f));

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
                if (!localPlayerAffected["TerminalRally"]) { return true; }

                __instance.health = 100;
                HUDManager.Instance.UpdateHealthUI(100, false);
                localPlayerAffected["TerminalRally"] = false;

                __instance.StatusEffectController().ApplyEffect(new OnRemoveActionEffect(() =>
                {
                    __instance.KillPlayer(Vector3.zero);
                }, "TerminalRally", "Terminal Rally Death", 10f));

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