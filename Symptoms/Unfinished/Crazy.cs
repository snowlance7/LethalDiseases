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
        [Symptom("Crazy", "I was crazy once, they put me in a room", Symptom.SymptomType.Bad, 50)] // Everyone is wearing masks
        public static StatusEffect Crazy(Disease disease)
        {
            throw new NotImplementedException();
        }
    }
}
