using GameNetcodeStuff;
using HarmonyLib;
using SnowyLib;
using UnityEngine;
using static LethalDiseases.Plugin;
using static LethalDiseases.SymptomAffectedObjects;

namespace LethalDiseases.Symptoms
{
    internal static partial class Symptoms
    {
        [Symptom("Rage", "Double damage but you cant see health", Symptom.SymptomType.Neutral, 50)] // TODO
        public static StatusEffect Rage(Disease disease)
        {
            if (disease.player != null)
                NetworkHandler.Instance.AddSymptomAffectedObjectRpc("Rage", disease.player.NetworkObject);
            return new OnRemoveActionEffect((effect) =>
            {
                if (disease.player == null) { return; }
                NetworkHandler.Instance.AddSymptomAffectedObjectRpc("Rage", disease.player.NetworkObject);
            }, disease.ToString(), "Rage", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }

    [HarmonyPatch]
    internal static class RageSymptomPatches
    {

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.HitEnemy))]
        private static void EnemyAI_HitEnemy_Prefix(EnemyAI __instance, ref int force, PlayerControllerB? playerWhoHit)
        {
            try
            {
                if (playerWhoHit == null || !symptomAffectedPlayers["Rage"].Contains(playerWhoHit)) { return; }
                force = Mathf.Min(force * 2, 100);
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        /*[HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.DamagePlayerFromOtherClientServerRpc))]
        private static void PlayerControllerB_DamagePlayerFromOtherClientServerRpc_Prefix(PlayerControllerB __instance, ref int damageAmount, int playerWhoHit)
        {
            try
            {
                if (!__instance.IsServer) { return; }
                PlayerControllerB? _playerWhoHit = PlayerFromId((ulong)playerWhoHit);
                if (_playerWhoHit == null) { return; }

                if (!RageSymptom.affectedPlayers.Contains(_playerWhoHit)) { return; }
                damageAmount = Mathf.Min(damageAmount * 2, 100);
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }*/
    }
}
