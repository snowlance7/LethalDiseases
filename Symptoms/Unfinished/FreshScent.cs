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
        [Symptom("Fresh Scent", "If circuit bees are triggered, they will hunt you down", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect FreshScent(Disease disease)
        {
            throw new NotImplementedException();
        }
    }
}
