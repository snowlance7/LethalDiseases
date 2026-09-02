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
        [Symptom("Selective Hearing", "You cant hear certain players", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect SelectiveHearing(Disease disease)
        {
            throw new NotImplementedException();
        }
    }
}
