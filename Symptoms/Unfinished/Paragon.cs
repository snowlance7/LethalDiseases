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
        [Symptom("Paragon", "Gain every other negative effect but cant die", Symptom.SymptomType.Bad, 1)]
        public static StatusEffect Paragon(Disease disease)
        {
            throw new NotImplementedException();
        }
    }
}
