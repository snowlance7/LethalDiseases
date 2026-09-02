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
        [Symptom("Charlie Horse", "You randomly make horse noises", Symptom.SymptomType.Neutral, 50)]
        public static StatusEffect CharlieHorse(Disease disease)
        {
            throw new NotImplementedException();
        }
    }
}
