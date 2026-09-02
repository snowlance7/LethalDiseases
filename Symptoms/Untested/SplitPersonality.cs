using Dawn.Utils;
using GameNetcodeStuff;
using SnowyLib;
using System.Linq;
using UnityEngine;

namespace LethalDiseases.Symptoms
{
    internal static partial class Symptoms
    {
        [Symptom("Split Personality", "Randomly swap positions with a random player periodically", Symptom.SymptomType.Bad, 10)]
        public static StatusEffect SplitPersonality(Disease disease)
        {
            return new RandomIntervalActionEffect(new BoundedRange(60, 500), () =>
            {
                if (disease.player != null)
                {
                    PlayerControllerB? randomPlayer = StartOfRound.Instance.allPlayerScripts.Where(x => x.isPlayerControlled).GetRandom();
                    if (randomPlayer == null) { return; }
                    Vector3 playerPosition = disease.player.transform.position;
                    disease.player.TeleportPlayer(randomPlayer.transform.position);
                    networkHandler.TeleportPlayerRpc(randomPlayer.actualClientId, playerPosition); // TODO: Test
                }
            }, disease.ToString(), "Split Personality", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }
}
