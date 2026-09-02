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
        [Symptom("Ionizing", "You periodically do damage to nearby players", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Ionizing(Disease disease)
        {
            throw new NotImplementedException();
        }
    }
}
