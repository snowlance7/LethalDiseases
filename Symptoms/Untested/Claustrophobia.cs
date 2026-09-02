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
        [Symptom("Claustrophobia", "You fear being inside", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Claustrophobia(Disease disease)
        {
            return new TickActionEffect(() =>
            {
                if (localPlayer.isInsideFactory)
                    localPlayer.JumpToFearLevel(1f);
            }, disease.ToString(), "Claustrophobia", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }
}
