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
        [Symptom("Burnt Lungs", "1.5x stamina usage", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect BurntLungs(Disease disease)
        {
            return new TickActionEffect(() =>
            {
                if (disease.player == null) { return; }
                disease.player.sprintMeter = Mathf.Clamp(disease.player.sprintMeter, 0, 0.75f);
            }, disease.ToString(), "Burnt Lungs", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }
}
