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
        [Symptom("Alarm", "You randomly make alarm sounds and attract enemies", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Alarm(Disease disease)
        {
            throw new NotImplementedException();
        }
    }
}
