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
        [Symptom("Trigger Finger", "Randomly use whatever youre holding (left mouse click)", Symptom.SymptomType.Bad, 100)]
        public static StatusEffect TriggerFinger(Disease disease)
        {
            return new RandomIntervalActionEffect(new BoundedRange(30, 60), () =>
            {
                if (disease.player == null) { return; }
                if (!disease.player.CanUseItem()) { return; }
                if (UnityEngine.Random.Range(0, 2) == 0) { return; }
                disease.player.currentlyHeldObjectServer?.UseItemOnClient(buttonDown: true);
            }, disease.ToString(), "Trigger Finger", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }
}
