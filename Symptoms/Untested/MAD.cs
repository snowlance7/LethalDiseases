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
        [Symptom("MAD", "You kill the nearest living thing on death", Symptom.SymptomType.Neutral, 50)]
        public static StatusEffect MAD(Disease disease)
        {
            return new OnRemoveActionEffect((effect) =>
            {
                if ((disease.player != null && disease.player.isPlayerDead) || (disease.enemy != null && disease.enemy.isEnemyDead)) // TODO: Simplify this
                {
                    GameObject? closestObj = GetClosestActorToPosition(disease.networkObject.transform.position);
                    if (closestObj == null) { return; }
                    if (closestObj.TryGetComponent(out EnemyAI enemy))
                    {
                        enemy.KillEnemyServerRpc(false); // TODO: Test
                    }
                    if (closestObj.TryGetComponent(out PlayerControllerB player))
                    {
                        player.KillPlayerServerRpc(-1, true, Vector3.zero, 0, 0, Vector3.zero, false); // TODO: Test
                    }
                }
            }, disease.ToString(), "IBS", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }
}
