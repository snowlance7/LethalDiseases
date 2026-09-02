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
        [Symptom("IBS", "You explode on death", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect IBS(Disease disease)
        {
            return new OnRemoveActionEffect((effect) =>
            {
                if (disease.player != null && disease.player.isPlayerDead)
                    networkHandler.SpawnExplosionRpc(disease.player.transform.position, true);
                if (disease.enemy != null && disease.enemy.isEnemyDead)
                    networkHandler.SpawnExplosionRpc(disease.enemy.transform.position, true);
            }, disease.ToString(), "IBS", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }
}
