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
        [Symptom("Lightheaded", "Get dizzy when running too much", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Lightheaded(Disease disease)
        {
            return new ConditionalActionEffect(() => disease.player?.sprintMeter < 0.5f, () =>
            {
                if (disease.player == null) { return; }
                disease.player.drunkness = Mathf.Max(disease.player.drunkness, 0.4f);
            }, false, disease.ToString(), 0, 0, "Lightheaded", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }
}
