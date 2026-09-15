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
        [Symptom("Haunted", "If a ghost girl spawns, it targets you. If multiple people have this, chooses a random player", Symptom.SymptomType.Bad, 5)]
        public static StatusEffect Haunted(Disease disease)
        {
            if (disease.player != null)
                AddSymptomAffectedObject("Haunted", disease.player.NetworkObject);
            return new OnRemoveActionEffect((effect) =>
            {
                if (disease.player == null) { return; }
                RemoveSymptomAffectedObject("Haunted", disease.player.NetworkObject);
            }, disease.ToString(), "Haunted", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }

    [HarmonyPatch]
    internal static class HauntedSymptomPatches
    {
        [HarmonyPrefix, HarmonyPatch(typeof(DressGirlAI), nameof(DressGirlAI.ChoosePlayerToHaunt))]
        static bool DressGirlAI_ChoosePlayerToHauntPrefix(DressGirlAI __instance)
        {
            try
            {
                if (symptomAffectedObjects["Haunted"].Count <= 0) { return true; }

                __instance.timesChoosingAPlayer++;
                if (__instance.timesChoosingAPlayer > 1)
                {
                    __instance.timer = __instance.hauntInterval - 1f;
                }
                __instance.SFXVolumeLerpTo = 0f;
                __instance.creatureVoice.Stop();
                __instance.heartbeatMusic.volume = 0f;
                if (!__instance.initializedRandomSeed)
                {
                    __instance.ghostGirlRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 158);
                }

                __instance.hauntingPlayer = symptomAffectedPlayers["Haunted"].GetRandom(__instance.ghostGirlRandom);
                Debug.Log($"Little girl: Haunting player with playerClientId: {__instance.hauntingPlayer!.playerClientId}; actualClientId: {__instance.hauntingPlayer.actualClientId}");
                __instance.ChangeOwnershipOfEnemy(__instance.hauntingPlayer.actualClientId);
                __instance.hauntingLocalPlayer = GameNetworkManager.Instance.localPlayerController == __instance.hauntingPlayer;
                if (__instance.switchHauntedPlayerCoroutine != null)
                {
                    __instance.StopCoroutine(__instance.switchHauntedPlayerCoroutine);
                }
                __instance.switchHauntedPlayerCoroutine = __instance.StartCoroutine(__instance.setSwitchingHauntingPlayer());
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
