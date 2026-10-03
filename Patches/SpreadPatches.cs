using Dawn;
using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.DiseaseAPI;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Patches;

[HarmonyPatch]
internal static class SpreadPatches
{
    static PlayerControllerB? collidedWith;
    static EnemyAI? enemyColliding;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.OnCollideWithPlayer))]
    static void EnemyAI_OnCollideWithPlayer_Prefix(EnemyAI __instance, Collider other) // TODO: Test this
    {
        try
        {
            if (!__instance.IsServer) { return; }

            if (!other.gameObject.TryGetComponent(out PlayerControllerB player)) { return; }
            collidedWith = player;
            enemyColliding = __instance;
        }
        catch (System.Exception e)
        {
            logger.LogError(e);
            return;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.DamagePlayerServerRpc))]
    static void PlayerControllerB_DamagePlayerServerRpc_Postfix(PlayerControllerB __instance) // TODO: Test this
    {
        try
        {
            if (!__instance.IsServer) { return; }

            if (collidedWith == null || enemyColliding == null) { return; }
            if (__instance != collidedWith) { return; }
            enemyColliding.NetworkObject.TrySpread(collidedWith.NetworkObject, Disease.TransmissionType.Blood);
        }
        catch (System.Exception e)
        {
            logger.LogError(e);
            return;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.OnCollideWithPlayer))]
    static void EnemyAI_OnCollideWithPlayer_Postfix(EnemyAI __instance, Collider other) // TODO: Test this
    {
        try
        {
            if (!__instance.IsServer) { return; }

            collidedWith = null;
            enemyColliding = null;

            if (!other.gameObject.TryGetComponent(out PlayerControllerB player)) { return; }
            TrySpreadBetween(__instance.NetworkObject, player.NetworkObject, Disease.TransmissionType.Contact);

            if (player.bleedingHeavily)
            {
                int enemyMaxHp = __instance.enemyType.enemyPrefab.GetComponent<EnemyAI>().enemyHP;
                if (__instance.enemyHP <= Mathf.CeilToInt(enemyMaxHp * 0.1f))
                {
                    TrySpreadBetween(__instance.NetworkObject, player.NetworkObject, Disease.TransmissionType.Blood);
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
    [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.OnCollideWithEnemy))]
    static void EnemyAI_OnCollideWithEnemy_Postfix(EnemyAI __instance, Collider other, EnemyAI collidedEnemy) // TODO: Test this
    {
        try
        {
            if (!__instance.IsServer) { return; }

            if (collidedEnemy != null)
                TrySpreadBetween(__instance.NetworkObject, collidedEnemy.NetworkObject, Disease.TransmissionType.Contact);
            else if (other.gameObject.TryGetComponent(out NetworkObject networkObject))
                TrySpreadBetween(__instance.NetworkObject, networkObject, Disease.TransmissionType.Contact);
        }
        catch (System.Exception e)
        {
            logger.LogError(e);
            return;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.HitEnemy))]
    static void EnemyAI_HitEnemy_Postfix(EnemyAI __instance, PlayerControllerB? playerWhoHit) // TODO: Test this
    {
        try
        {
            if (!__instance.IsServer || playerWhoHit == null) { return; }

            playerWhoHit.NetworkObject.TrySpread(__instance.NetworkObject, Disease.TransmissionType.Blood | Disease.TransmissionType.Contact);
        }
        catch (System.Exception e)
        {
            logger.LogError(e);
            return;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.DamagePlayerFromOtherClientServerRpc))]
    static void PlayerControllerB_DamagePlayerFromOtherClientServerRpc_Postfix(PlayerControllerB __instance, int playerWhoHit) // TODO: Test this
    {
        try
        {
            if (!__instance.IsServer) { return; }

            PlayerControllerB? _playerWhoHit = PlayerFromId((ulong)playerWhoHit);
            if (_playerWhoHit == null) { return; }

            _playerWhoHit.NetworkObject.TrySpread(__instance.NetworkObject, Disease.TransmissionType.Blood | Disease.TransmissionType.Contact);
        }
        catch (System.Exception e)
        {
            logger.LogError(e);
            return;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.GrabItemOnClient))]
    static void GrabbableObject_GrabItemOnClient_Postfix(GrabbableObject __instance) // TODO: Test this
    {
        try
        {
            if (!__instance.IsServer) { return; }

            PlayerControllerB? playerWhoGrabbed = __instance.playerHeldBy;
            if (playerWhoGrabbed == null) { return; }

            TrySpreadBetween(playerWhoGrabbed.NetworkObject, __instance.NetworkObject, Disease.TransmissionType.Contact);
        }
        catch (System.Exception e)
        {
            logger.LogError(e);
            return;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CadaverGrowthAI), nameof(CadaverGrowthAI.InfectPlayers))]
    static void CadaverGrowthAI_InfectPlayers_Postfix(CadaverGrowthAI __instance) // TODO: Test this
    {
        try
        {
            if (!__instance.stoodInWeedsLastCheck) { return; }

            __instance.NetworkObject.TrySpread(localPlayer.NetworkObject, Disease.TransmissionType.Airborne, 0.1f);
        }
        catch (System.Exception e)
        {
            logger.LogError(e);
            return;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.ItemActivate))]
    static void GrabbableObject_ItemActivate_Prefix(GrabbableObject __instance) // TODO: Test this
    {
        try
        {
            var info = __instance.itemProperties.GetDawnInfo();
            if (info == null || !(info.AllTags().Any(x => Configs.TransmissableFoodborneTags.Contains(x.ToString())) || Configs.TransmissableFoodborneItems.Contains(info.TypedKey.ToString()))) { return; }
            if (__instance.playerHeldBy == null || __instance.playerHeldBy != localPlayer) { return; }
            __instance.NetworkObject.TrySpread(localPlayer.NetworkObject, Disease.TransmissionType.Foodborne);
        }
        catch (System.Exception e)
        {
            logger.LogError(e);
            return;
        }
    }
}