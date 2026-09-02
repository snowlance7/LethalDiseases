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
        [Symptom("Sponge Legs", "You make an annoying sound when you walk", Symptom.SymptomType.Neutral, 50)]
        public static StatusEffect SpongeLegs(Disease disease)
        {
            // spongebob walking noises
            throw new NotImplementedException();
        }
    }
}
