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
        [Symptom("Shroombiosis", "You grow shrooms on your body which heal players when eaten", Symptom.SymptomType.Good, 50)]
        public static StatusEffect Shroombiosis(Disease disease)
        {
            throw new NotImplementedException();
        }
    }
}
