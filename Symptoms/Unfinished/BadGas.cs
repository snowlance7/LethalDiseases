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
        [Symptom("Bad Gas", "Slowly gain the TZP effect when inside and fart randomly", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect BadGas(Disease disease)
        {
            throw new NotImplementedException();
        }
    }
}
