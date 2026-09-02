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
        [Symptom("Mood Swings", "Randomly increases/decreases random stats every 2 ingame hours", Symptom.SymptomType.Neutral, 50)]
        public static StatusEffect MoodSwings(Disease disease)
        {
            /*Mood Swings -Randomly increases and/ or decreases each of your movement factors every 2 ingame hours.
            This includes:
            Sprint Stamina
            Jump Stamina
            Walk Speed
            Critical Speed
            Crouch Speed
            Sprint Speed
            Water Speed
            Jump Height
            Item Weight Influence*/
            throw new NotImplementedException();
        }
    }
}
