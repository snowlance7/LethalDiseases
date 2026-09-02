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
        [Symptom("Clumsiness", "Randomly trip", Symptom.SymptomType.Bad, 100)]
        public static StatusEffect Clumsiness(Disease disease)
        {
            return new RandomIntervalActionEffect(new BoundedRange(15, 120), () =>
            {
                if (disease.player == null) { return; }
                disease.player.PlayQuickSpecialAnimation(1.5f);
                disease.player.playerBodyAnimator.SetTrigger("SA_PushLeverBack");
            }, disease.ToString(), "Clumsiness", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }
}
