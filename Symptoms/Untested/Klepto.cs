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
        [Symptom("Klepto", "Randomly and periodically take items from nearby players", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Klepto(Disease disease)
        {
            return new RandomIntervalActionEffect(new BoundedRange(30f, 120f), () =>
            {
                PlayerControllerB? player = StartOfRound.Instance.allPlayerScripts.Where(x => x.isPlayerControlled).GetClosestToPosition(localPlayer.transform.position, (x) => x.transform.position);
                if (player == null) { return; }
                GrabbableObject? grabbable = player.currentlyHeldObjectServer;
                if (grabbable == null || localPlayer.FirstEmptyItemSlot(grabbable) == -1) { return; }
                networkHandler.PlayerDiscardHeldObjectRpc(player.actualClientId);
                localPlayer.GrabGrabbableObject(grabbable);
            }, disease.ToString(), "Klepto", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }
}
