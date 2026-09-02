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
        [Symptom("Empyema", "Leave a trail of slime when you move", Symptom.SymptomType.Neutral, 50)]
        public static StatusEffect Empyema(Disease disease)
        {
            throw new NotImplementedException();
        }
    }
}
