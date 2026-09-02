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
        [Symptom("Bald", "Looking at the infected player for too long will trigger a flashbang", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Bald(Disease disease)
        {
            throw new NotImplementedException();
        }
    }
}
