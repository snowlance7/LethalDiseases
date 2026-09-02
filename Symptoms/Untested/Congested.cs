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
        [Symptom("Congested", "Your voice is muffled", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Congested(Disease disease)
        {
            if (disease.player != null)
                SnowyLib.NetworkHandler.Instance.MufflePlayerRpc(disease.player.actualClientId, true);

            return new OnRemoveActionEffect((effect) =>
            {
                if (disease.player == null) { return; }
                SnowyLib.NetworkHandler.Instance.MufflePlayerRpc(disease.player.actualClientId, false);
            }, disease.ToString(), "Congested", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }
}
