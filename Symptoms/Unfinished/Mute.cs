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
        [Symptom("Mute", "Cant use comms to speak", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Mute(Disease disease)
        {
            throw new NotImplementedException();
        }
    }
}
