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
    internal class ServerPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.FinishGeneratingLevel))]
        private static void RoundManager_FinishGeneratingLevel_Postfix(RoundManager __instance)
        {
            try
            {
                if (!__instance.IsServer) { return; }
                logger.LogDebug("Started generating diseases for level");
                SteamValveHazard[] valves = GameObject.FindObjectsOfType<SteamValveHazard>();
                foreach (var valve in valves)
                {
                    if (UnityEngine.Random.Range(0f, 1f) < steamDiseaseChance.Value)
                    {
                        Disease disease = Disease.CreateRandomDisease();
                        disease.Infect(valve.fixInteract.NetworkObject);
                    }
                }

                List<GrabbableObject> spawnedScrap = GameObject.FindObjectsOfType<GrabbableObject>().ToList();

                foreach (var scrap in spawnedScrap)
                {
                    if (!scrap.isInFactory) { continue; }
                    if (UnityEngine.Random.Range(0f, 1f) < scrapDiseaseChance.Value)
                    {
                        Disease disease = Disease.CreateRandomDisease();
                        disease.Infect(scrap.NetworkObject);
                    }
                }

                List<InteractTrigger> triggers = GameObject.FindObjectsOfType<InteractTrigger>().ToList();

                foreach (var trigger in triggers)
                {
                    if (trigger.hasTriggered) { continue; }
                    if (UnityEngine.Random.Range(0f, 1f) < interactableDiseaseChance.Value)
                    {
                        Disease disease = Disease.CreateRandomDisease();
                        disease.Infect(trigger.NetworkObject);
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
        private static void EnemyAI_Start_Postfix(EnemyAI __instance)
        {
            try
            {
                if (!__instance.IsServer) { return; }
                if (UnityEngine.Random.Range(0f, 1f) < monsterDiseaseChance.Value)
                {
                    Disease disease = Disease.CreateRandomDisease();
                    disease.Infect(__instance.NetworkObject);
                }
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.OnCollideWithPlayer))]
        private static void EnemyAI_OnCollideWithPlayer_Postfix(EnemyAI __instance, Collider other)
        {
            try
            {
                if (!__instance.IsServer) { return; }
                if (!other.gameObject.TryGetComponent(out NetworkObject networkObject)) { return; }

                List<Disease> diseases = __instance.NetworkObject.GetDiseases();
                foreach (var disease in diseases)
                {
                    disease.TrySpread(networkObject);
                }
                diseases = networkObject.GetDiseases();
                foreach (var disease in diseases)
                {
                    disease.TrySpread(__instance.NetworkObject);
                }
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.HitEnemy))]
        private static void EnemyAI_HitEnemy_Postfix(EnemyAI __instance, PlayerControllerB? playerWhoHit)
        {
            try
            {
                if (!__instance.IsServer || playerWhoHit == null) { return; }

                List<Disease> diseases = playerWhoHit.NetworkObject.GetDiseases();

                foreach (var disease in diseases)
                {
                    disease.TrySpread(__instance.NetworkObject);
                }
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.DamagePlayerFromOtherClientServerRpc))]
        private static void PlayerControllerB_DamagePlayerFromOtherClientServerRpc_Postfix(PlayerControllerB __instance, int playerWhoHit)
        {
            try
            {
                if (!__instance.IsServer) { return; }
                PlayerControllerB? _playerWhoHit = PlayerFromId((ulong)playerWhoHit);
                if (_playerWhoHit == null) { return; }

                List<Disease> diseases = _playerWhoHit.NetworkObject.GetDiseases();
                foreach (var disease in diseases)
                {
                    disease.TrySpread(__instance.NetworkObject);
                }
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.GrabItemOnClient))]
        private static void GrabbableObject_GrabItemOnClient_Postfix(GrabbableObject __instance)
        {
            try
            {
                if (!__instance.IsServer) { return; }
                PlayerControllerB playerWhoGrabbed = __instance.playerHeldBy;

                List<Disease> diseases = __instance.NetworkObject.GetDiseases();
                foreach (var disease in diseases)
                {
                    disease.TrySpread(playerWhoGrabbed.NetworkObject);
                }

                diseases = playerWhoGrabbed.NetworkObject.GetDiseases();
                foreach (var disease in diseases)
                {
                    disease.TrySpread(__instance.NetworkObject);
                }
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }
    }
}
