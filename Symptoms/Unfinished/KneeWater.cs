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
        [Symptom("Knee Water", "Randomly get the quicksand effect", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect KneeWater(Disease disease)
        {
            throw new NotImplementedException();
        }
    }
}
