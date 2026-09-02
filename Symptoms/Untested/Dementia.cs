using Dawn.Utils;
using GameNetcodeStuff;
using HarmonyLib;
using SnowyLib;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static LethalDiseases.Plugin;
using static LethalDiseases.SymptomAffectedObjects;

namespace LethalDiseases.Symptoms
{
    internal static partial class Symptoms
    {
        [Symptom("Dementia", "Randomly teleport to a location periodically", Symptom.SymptomType.Bad, 50)] // TODO: Make them drop an item before teleporting
        public static StatusEffect Dementia(Disease disease)
        {
            return new RandomIntervalActionEffect(new BoundedRange(60, 500), () =>
            {
                if (disease.player == null) { return; }
                GameObject? node = Utils.allAINodes.GetRandom(Utils.randomLocal);
                if (node == null) { return; }
                disease.player.TeleportPlayer(node.transform.position);
            }, disease.ToString(), "Dementia", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }
}
