using GameNetcodeStuff;
using HarmonyLib;
using SnowyLib;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static LethalDiseases.Plugin;
using static LethalDiseases.SymptomAffectedObjects;

namespace LethalDiseases.Symptoms
{
    internal static partial class Symptoms
    {
        [Symptom("Aquaphobia", "You fear water", Symptom.SymptomType.Neutral, 50)]
        public static StatusEffect Aquaphobia(Disease disease)
        {
            LayerMask mask = LayerMask.GetMask("Water");

            return new IntervalActionEffect(0.2f, () =>
            {
                PlayerControllerB? player = disease.player;
                if (player == null) { return; }
                if (player.isInsideFactory || !Physics.Raycast(player.gameplayCamera.transform.position, player.gameplayCamera.transform.forward, 100f, mask)) { return; }
                player.JumpToFearLevel(1f);
            }, disease.ToString(), "Aquaphobia", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }
}
