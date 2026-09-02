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
        [Symptom("Broken Legs", "You are always limping", Symptom.SymptomType.Bad, 100)]
        public static StatusEffect BrokenLegs(Disease disease)
        {
            return new TickActionEffect(() =>
            {
                if (disease.player == null) { return; }
                disease.player.criticallyInjured = true;
                disease.player.playerBodyAnimator.SetBool("Limp", value: true);
            }, disease.ToString(), "Broken Legs", disease.strengthTime, SetHighestDurationAndDeny, onRemove: (effect) =>
            {
                if (disease.player == null) { return; }
                disease.player.criticallyInjured = false;
                disease.player.playerBodyAnimator.SetBool("Limp", value: false);
            });
        }
    }
}
