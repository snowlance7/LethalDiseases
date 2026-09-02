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
        [Symptom("Vampirism", "Leech health from other living things", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Vampirism(Disease disease)
        {
            throw new NotImplementedException();
            return new IntervalActionEffect(2.5f, () =>
            {
                if (disease.player != null)
                {
                    if (disease.player.health >= 100) { return; }
                    GameObject? leechObj = GetClosestActorToPosition(disease.player.transform.position, 5f);
                    if (leechObj == null) { return; }
                    if (leechObj.TryGetComponent(out PlayerControllerB player))
                    {
                        //player.DamagePlayerServerRpc(1, player.health - 1)
                    }
                }
                if (disease.enemy != null)
                {

                }
            }, disease.ToString(), "", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }
}
