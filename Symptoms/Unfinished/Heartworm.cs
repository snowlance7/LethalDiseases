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
        [Symptom("Heartworm", "Spawns a earth leviathon on your location on death", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Heartworm(Disease disease)
        {
            throw new NotImplementedException();
        }
    }
}
