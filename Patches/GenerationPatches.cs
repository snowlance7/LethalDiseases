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
        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.FinishGeneratingLevel))]
        static void RoundManager_FinishGeneratingLevel_Postfix(RoundManager __instance) // TODO: Not finding objects? use on ship landed?
        {
            try
            {
                if (!__instance.IsServer) { return; }
                if (TESTING.disableDiseaseSpawning) { return; }

                logger.LogDebug("Started generating diseases for level");

                // SteamValveHazard
                logger.LogDebug("Generating diseases for SteamValveHazard");
                SteamValveHazard[] valves = GameObject.FindObjectsOfType<SteamValveHazard>();
                foreach (var valve in valves)
                {
                    if (UnityEngine.Random.Range(0f, 1f) < steamDiseaseChance.Value)
                    {
                        valve.fixInteract.NetworkObject.Infect();
                    }
                }

                // Grabbable Objects
                logger.LogDebug("Generating diseases for spawned scrap");
                List<GrabbableObject> spawnedScrap = GameObject.FindObjectsOfType<GrabbableObject>().ToList();
                foreach (var scrap in spawnedScrap)
                {
                    if (!scrap.isInFactory || scrap.isInShipRoom) { continue; }
                    if (UnityEngine.Random.Range(0f, 1f) < scrapDiseaseChance.Value)
                    {
                        scrap.NetworkObject.Infect();
                    }
                }
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.Start))]
        static void EnemyAI_Start_Postfix(EnemyAI __instance)
        {
            try
            {
                if (!__instance.IsServer) { return; }
                if (TESTING.disableDiseaseSpawning) { return; }

                if (UnityEngine.Random.Range(0f, 1f) < monsterDiseaseChance.Value)
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
