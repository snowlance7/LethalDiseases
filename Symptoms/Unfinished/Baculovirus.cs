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
        [Symptom("Baculovirus", "", Symptom.SymptomType.Bad, 1)]
        public static StatusEffect Baculovirus(Disease disease)
        {
            // baculovirus. It makes caterpillars explode to infect other caterpillars. (By making them go high to the light but going to ship might be easier for coding reasons.) Not sure how hard/bad that would be but it'd be a fun zombie option
            throw new NotImplementedException();
        }
    }
}
