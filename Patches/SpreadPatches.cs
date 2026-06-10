using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.DiseaseAPI;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Patches
{
    [HarmonyPatch]
    internal static class SpreadPatches
    {

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InteractTrigger), nameof(InteractTrigger.Interact))]
        static void InteractTrigger_Interact_Postfix(InteractTrigger __instance, Transform playerTransform)
        {
            try
            {
                if (!playerTransform.TryGetComponent(out PlayerControllerB player)) { return; }
                TrySpreadBetween(player.NetworkObject, __instance.NetworkObject);
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.OnCollideWithPlayer))]
        static void EnemyAI_OnCollideWithPlayer_Postfix(EnemyAI __instance, Collider other)
        {
            try
            {
                if (!__instance.IsServer) { return; }

                if (!other.gameObject.TryGetComponent(out NetworkObject networkObject)) { return; }
                TrySpreadBetween(__instance.NetworkObject, networkObject);
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.OnCollideWithEnemy))]
        static void EnemyAI_OnCollideWithEnemy_Postfix(EnemyAI __instance, Collider other, EnemyAI collidedEnemy)
        {
            try
            {
                if (!__instance.IsServer) { return; }

                if (collidedEnemy != null)
                    TrySpreadBetween(__instance.NetworkObject, collidedEnemy.NetworkObject);
                else if (other.gameObject.TryGetComponent(out NetworkObject networkObject))
                    TrySpreadBetween(__instance.NetworkObject, networkObject);
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.HitEnemy))]
        static void EnemyAI_HitEnemy_Postfix(EnemyAI __instance, PlayerControllerB? playerWhoHit)
        {
            try
            {
                if (!__instance.IsServer || playerWhoHit == null) { return; }

                playerWhoHit.NetworkObject.TrySpread(__instance.NetworkObject);
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.DamagePlayerFromOtherClientServerRpc))]
        static void PlayerControllerB_DamagePlayerFromOtherClientServerRpc_Postfix(PlayerControllerB __instance, int playerWhoHit)
        {
            try
            {
                if (!__instance.IsServer) { return; }

                PlayerControllerB? _playerWhoHit = PlayerFromId((ulong)playerWhoHit);
                if (_playerWhoHit == null) { return; }

                _playerWhoHit.NetworkObject.TrySpread(__instance.NetworkObject);
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.GrabItemOnClient))]
        static void GrabbableObject_GrabItemOnClient_Postfix(GrabbableObject __instance)
        {
            try
            {
                if (!__instance.IsServer) { return; }

                PlayerControllerB? playerWhoGrabbed = __instance.playerHeldBy;
                if (playerWhoGrabbed == null) { return; }

                TrySpreadBetween(playerWhoGrabbed.NetworkObject, __instance.NetworkObject);
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }
    }
}