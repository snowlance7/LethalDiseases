using GameNetcodeStuff;
using HarmonyLib;
using SnowyLib;
using System;
using System.Linq;
using UnityEngine.Rendering.HighDefinition;
using static LethalDiseases.Plugin;
using static LethalDiseases.SymptomAffectedObjects;

namespace LethalDiseases.Symptoms
{
    internal static partial class Symptoms
    {
        [Symptom("Cursed", "Spawns a ghost girl that targets you", Symptom.SymptomType.Bad, 1)] // Spawn ghost girl
        public static StatusEffect Cursed(Disease disease)
        {
            return new IntervalActionEffect(10f, () =>
            {
                if (disease.player == null || StartOfRound.Instance.inShipPhase) { return; }
                if (RoundManager.Instance.SpawnedEnemies.Any(x => x is DressGirlAI girl && girl.hauntingPlayer == disease.player)) { return; }
                networkHandler.SpawnGhostGirlRpc(disease.player.actualClientId);
            }, disease.ToString(), "Cursed", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }
}
