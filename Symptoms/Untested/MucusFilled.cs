using Dawn.Utils;
using GameNetcodeStuff;
using HarmonyLib;
using SnowyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static LethalDiseases.Plugin;
using static LethalDiseases.SymptomAffectedObjects;

namespace LethalDiseases.Symptoms
{
    internal static partial class Symptoms
    {
        [Symptom("Mucus Filled", "Periodically spit mucus on other player, which can spread the disease", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect MucusFilled(Disease disease)
        {
            return new RandomIntervalActionEffect(new BoundedRange(30, 120), () =>
            {
                if (disease.player == null) { return; }
                List<ulong> clientIds = new List<ulong>();
                Collider[] collided = [];
                int collidersHitCount = Physics.OverlapCapsuleNonAlloc(disease.player.gameplayCamera.transform.position, disease.player.gameplayCamera.transform.position + disease.player.gameplayCamera.transform.forward * 5f, 2.5f, collided, StartOfRound.Instance.playersMask, QueryTriggerInteraction.Ignore);
                if (collidersHitCount <= 0) { return; }
                foreach (var playerCollider in collided)
                {
                    PlayerControllerB player = playerCollider.gameObject.GetComponent<PlayerControllerB>();
                    if (player == null) continue;
                    clientIds.Add(player.actualClientId);
                }
                networkHandler.SlimePlayersRpc(clientIds.ToArray(), disease.ToString()); // TODO: Test this
            }, disease.ToString(), "Mucus Filled", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }
}
