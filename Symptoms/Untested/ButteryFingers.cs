using Dawn.Utils;
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
        [Symptom("Buttery Fingers", "Periodically drop what you're holding", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect ButteryFingers(Disease disease)
        {
            return new RandomIntervalActionEffect(new BoundedRange(60, 120), () =>
            {
                if (disease.player == null) { return; }
                disease.player.DiscardHeldObject();
            }, disease.ToString(), "Buttery Fingers", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }
}
