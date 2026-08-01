using Dawn;
using GameNetcodeStuff;
using SnowyLib;
using UnityEngine;
using UnityEngine.AI;
using static LethalDiseases.SymptomAffectedObjects;

namespace LethalDiseases.Symptoms
{
    internal static partial class Symptoms // TODO: Test this
    {
        [Symptom("Bountiful", "Turn into a cadaver bloom on death", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Bountiful(Disease disease)
        {
            if (disease.player != null)
            {
                localPlayerAffected["Bountiful"] = true;
                StartOfRound.Instance.LocalPlayerDieEvent.AddListener(OnLocalPlayerDie);
            }
            return new OnRemoveActionEffect((effect) =>
            {
                if (disease.player == null) { return; }
                localPlayerAffected["Bountiful"] = false;
                StartOfRound.Instance.LocalPlayerDieEvent.RemoveListener(OnLocalPlayerDie);
            }, disease.id, "Bountiful", disease.strengthTime, SetHighestDurationAndDeny);
        }

        public static void OnLocalPlayerDie(PlayerControllerB player, int deathAnimation)
        {
            if (!localPlayerAffected["Bountiful"]) { return; }
            CadaverGrowthAI? cadaverGrowth = Object.FindObjectOfType<CadaverGrowthAI>();

            switch (deathAnimation)
            {
                case -1:
                case 7:
                case 9:
                    Debug.Log($"Cadaver growth: Unable to burst from dying player '{player.playerUsername}'. Reason A (incompatible cause of death). Time:{TimeOfDay.Instance.normalizedTimeOfDay} ");
                    return;
                default:
                    if (player.enemyWaitingForBodyRagdoll != null && player.enemyWaitingForBodyRagdoll.enemyType.enemyName == "MouthDog")
                    {
                        Debug.Log($"Cadaver growth: Unable to burst from dying player '{player.playerUsername}'. Reason B (eyeless dog). Time:{TimeOfDay.Instance.normalizedTimeOfDay} ");
                        return;
                    }
                    break;
                case 200:
                    break;
            }
            Vector3 pos = player.transform.position;
            if (Physics.Raycast(player.transform.position, Vector3.down, out var hitInfo, 10f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            {
                Debug.Log($"Cadaver growth: Burst on player death found raycast to ground. player position: {player.transform.position}; rayhit position: {hitInfo.point}; Time:{TimeOfDay.Instance.normalizedTimeOfDay} ");
                pos = hitInfo.point;
            }

            var info = LethalContent.Enemies[EnemyKeys.CadaverGrowths];
            int agentMask = RoundManager.Instance.GetLayermaskForEnemySizeLimit(info.EnemyType, supplyExistingMask: true, info.EnemyType.enemyPrefab.GetComponent<NavMeshAgent>().areaMask);

            pos = RoundManager.Instance.GetNavMeshPosition(pos, default, 10f, agentMask);
            if (!RoundManager.Instance.GotNavMeshPositionResult)
            {
                Debug.Log($"Cadaver growth: Unable to burst from dying player '{player.playerUsername}' Reason D (no navmesh position result). player position: {player.transform.position}; Time:{TimeOfDay.Instance.normalizedTimeOfDay} ");
                return;
            }

            NetworkHandler.Instance.CadaverBurstFromPlayerRpc(player.actualClientId);
        }
    }
}