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
        [Symptom("Anarchist", "Randomly summon landmines in front of you", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Anarchist(Disease disease)
        {
            return new RandomIntervalActionEffect(new BoundedRange(120f, 450f), () =>
            {
                if (disease.hasActor)
                    networkHandler.SpawnMapObjectRpc(MapObjectKeys.Landmine, disease.networkObject.gameObject.transform.position + disease.networkObject.gameObject.transform.forward * 2);
            }, disease.ToString(), "Anarchist", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }
}
