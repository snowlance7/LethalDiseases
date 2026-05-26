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
            logger.LogDebug("Init symptom ItFollows");
            if (disease.hasActor)
            {

                LethalDiseasesNetworkHandler.Instance.AddSymptomAffectedObjectServerRpc(symptomName, disease.networkObject);
            }
            return new OnRemoveActionEffect(() =>
            {
                if (!disease.hasActor) { return; }
                LethalDiseasesNetworkHandler.Instance.RemoveSymptomAffectedObjectServerRpc(symptomName, disease.networkObject);
            }, disease.id, symptomName, disease.strengthTime, Symptoms.SetHighestDurationAndDeny);
        }

        public static void Update(float deltaTime)
        {
            if (!IsServerOrHost || ItFollowsEntity.Instance != null || StartOfRound.Instance.inShipPhase || StartOfRound.Instance.shipIsLeaving || SymptomAffectedObjects.symptomAffectedObjects[symptomName].Count <= 0) { return; }
            logger.LogDebug("Spawning ItFollowsEntity");
            ItFollowsEntity.Init();
        }
    }
}