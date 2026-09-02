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
        [Symptom("Daltonizm", "You see in black and white", Symptom.SymptomType.Neutral, 50)]
        public static StatusEffect Daltonizm(Disease disease)
        {
            throw new NotImplementedException();
        }
    }
}
