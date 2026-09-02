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
        [Symptom("Narcissism", "Everyone is invisible to you", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Narcissism(Disease disease)
        {
            if (disease.player != null && disease.player == localPlayer)
            {
                logger?.LogDebug("Narcissism: Making players invisible");
                foreach (var player in StartOfRound.Instance.allPlayerScripts)
                {
                    if (player == null || !player.isPlayerControlled || player == localPlayer) { continue; }
                    player.MakePlayerInvisible(true);
                }
            }
            return new OnRemoveActionEffect((effect) =>
            {
                if (disease.player != null && disease.player == localPlayer)
                {
                    foreach (var player in StartOfRound.Instance.allPlayerScripts)
                    {
                        if (player == null || player == localPlayer) { continue; }
                        player.MakePlayerInvisible(false);
                    }
                }
            }, disease.ToString(), "Narcissism", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }
}
