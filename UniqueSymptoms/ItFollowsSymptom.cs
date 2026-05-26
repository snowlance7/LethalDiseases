using GameNetcodeStuff;
using HarmonyLib;
using LethalDiseases.Enemies;
using SnowyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.UniqueSymptoms
{
    internal static class ItFollowsSymptom
    {
        public const string symptomName = "It Follows";

        [Symptom(symptomName, "???", Symptom.SymptomType.Bad, 10)]
        public static StatusEffect ItFollows(Disease disease)
        {
            if (disease.player != null || disease.enemy != null)
                LethalDiseasesNetworkHandler.Instance.AddSymptomAffectedObjectServerRpc(symptomName, disease.networkObject);
            return new OnRemoveActionEffect(() =>
            {
                if (disease.player == null && disease.enemy == null) { return; }
                LethalDiseasesNetworkHandler.Instance.RemoveSymptomAffectedObjectServerRpc(symptomName, disease.networkObject);
            }, disease.id, symptomName, disease.strengthTime, Symptoms.SetHighestDurationAndDeny);
        }

        public static void Update(float deltaTime)
        {
            if (!IsServerOrHost || ItFollowsEntity.Instance != null || StartOfRound.Instance.inShipPhase || StartOfRound.Instance.shipIsLeaving || SymptomAffectedObjects.symptomAffectedObjects[symptomName].Count <= 0) { return; }
            Vector3 mainEntrancePosition = RoundManager.FindMainEntrancePosition(getTeleportPosition: true, getOutsideEntrance: false);
            GameObject? spawnNode = Utils.insideAINodes.GetFarthestFromPosition(mainEntrancePosition, (x) => x.transform.position);
            if (spawnNode == null) { return; }
            logger.LogDebug("Spawning ItFollowsEntity");
            Utils.SpawnEnemy(LethalDiseasesKeys.ItFollows, spawnNode.transform.position);
        }
    }
}