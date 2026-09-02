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
        [Symptom("Last Stand", "You kill the last thing that kills you", Symptom.SymptomType.Good, 50)]
        public static StatusEffect LastStand(Disease disease)
        {
            throw new NotImplementedException();
        }
    }
}
