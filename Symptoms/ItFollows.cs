using GameNetcodeStuff;
using HarmonyLib;
using LethalDiseases.Enemies;
using SnowyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using static LethalDiseases.Plugin;
using static LethalDiseases.SymptomAffectedObjects;

namespace LethalDiseases.Symptoms
{
    internal static partial class Symptoms // TODO: Needs more testing
    {
        [Symptom("ItFollows", "???", Symptom.SymptomType.Bad, 10)]
        public static StatusEffect ItFollows(Disease disease)
        {
            logger.LogDebug("Init symptom ItFollows");
            if (disease.hasActor)
            {

                LethalDiseasesNetworkHandler.Instance.AddSymptomAffectedObjectServerRpc("ItFollows", disease.networkObject);
            }
            return new OnRemoveActionEffect(() =>
            {
                if (!disease.hasActor) { return; }
                LethalDiseasesNetworkHandler.Instance.RemoveSymptomAffectedObjectServerRpc("ItFollows", disease.networkObject);
            }, disease.id, "ItFollows", disease.strengthTime, SetHighestDurationAndDeny);
        }

        public static void ItFollowsUpdate(float deltaTime)
        {
            if (!IsServerOrHost || ItFollowsEntity.Instance != null || StartOfRound.Instance.inShipPhase || StartOfRound.Instance.shipIsLeaving || symptomAffectedObjects["ItFollows"].Count <= 0) { return; }
            logger.LogDebug("Spawning ItFollowsEntity");
            ItFollowsEntity.Init();
        }
    }
}