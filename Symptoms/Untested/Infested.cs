using Dawn;
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
        [Symptom("Infested", "Periodically spawn hoarderbugs around you", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Infested(Disease disease)
        {
            return new RandomIntervalActionEffect(new BoundedRange(30, 300), () =>
            {
                if (disease.player == null) { return; }
                if (!disease.player.isInsideFactory || UnityEngine.Random.Range(0, 2) == 1) { return; }
                SnowyLib.NetworkHandler.Instance.SpawnEnemyRpc(EnemyKeys.Hoardingbug, disease.player.transform.position - disease.player.transform.forward);
            }, disease.ToString(), "Infested", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }
}
