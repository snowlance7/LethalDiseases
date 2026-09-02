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
        [Symptom("Egg Face", "Sapsuckers attack on sight", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect EggFace(Disease disease)
        {
            throw new NotImplementedException();
        }
    }
}
