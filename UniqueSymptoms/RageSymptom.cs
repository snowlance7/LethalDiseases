using GameNetcodeStuff;
using HarmonyLib;
using SnowyLib;
using System.Collections.Generic;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.UniqueSymptoms
{
    internal static class RageSymptom
    {
        public const string symptomName = "Rage";
        public static HashSet<PlayerControllerB> affectedPlayers = SymptomAffectedObjects.symptomAffectedPlayers[symptomName];

        [Symptom(symptomName, "Double damage but you cant see health", Symptom.SymptomType.Neutral, 50)]
        public static StatusEffect Rage(Disease disease)
        {
            if (disease.player != null)
                LethalDiseasesNetworkHandler.Instance.AddSymptomAffectedObjectServerRpc(symptomName, disease.player.NetworkObject);
            return new OnRemoveActionEffect(() =>
            {
                if (disease.player == null) { return; }
                LethalDiseasesNetworkHandler.Instance.AddSymptomAffectedObjectServerRpc(symptomName, disease.player.NetworkObject);
            }, disease.id, symptomName, disease.strengthTime, Symptoms.SetHighestDurationAndDeny);
        }
    }

    [HarmonyPatch]
    public class RageSymptomPatches
    {

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.HitEnemy))]
        private static void EnemyAI_HitEnemy_Prefix(EnemyAI __instance, ref int force, PlayerControllerB? playerWhoHit)
        {
            try
            {
                if (playerWhoHit == null || !RageSymptom.affectedPlayers.Contains(playerWhoHit)) { return; }
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
