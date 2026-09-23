using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Configs;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Patches
{
    [HarmonyPatch]
    internal static class GenerationPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.Start))]
        static void EnemyAI_Start_Postfix(EnemyAI __instance)
        {
            try
            {
                if (!__instance.IsServer || TESTING.disableDiseaseSpawning) { return; }

                if (UnityEngine.Random.Range(0f, 1f) < MonsterDiseaseChance.Value)
                    __instance.NetworkObject.Infect();
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SteamValveHazard), nameof(SteamValveHazard.Start))]
        static void SteamValveHazard_Start_Postfix(SteamValveHazard __instance)
        {
            try
            {
                if (!__instance.fixInteract.IsServer || TESTING.disableDiseaseSpawning) { return; }

                if (UnityEngine.Random.Range(0f, 1f) < SteamDiseaseChance.Value)
                    __instance.fixInteract.NetworkObject.Infect();
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
        static void GrabbableObject_Start_Postfix(GrabbableObject __instance)
        {
            try
            {
                if (!__instance.IsServer || TESTING.disableDiseaseSpawning) { return; }

                if (__instance.isInShipRoom) { return; }

                if (UnityEngine.Random.Range(0f, 1f) < ScrapDiseaseChance.Value)
                    __instance.NetworkObject.Infect();
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }
    }
}
