using GameNetcodeStuff;
using HarmonyLib;
using SnowyLib;
using System;
using UnityEngine;
using static LethalDiseases.Plugin;
using static LethalDiseases.SymptomAffectedObjects;

namespace LethalDiseases.Symptoms
{
    internal static partial class Symptoms
    {
        [Symptom("Arachnophobia", "Spiders instakill you", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Arachnophobia(Disease disease)
        {
            if (disease.player != null)
                localPlayerAffected["Arachnophobia"] = true;
            return new OnRemoveActionEffect((effect) =>
            {
                if (disease.player == null) { return; }
                localPlayerAffected["Arachnophobia"] = false;
            }, disease.ToString(), "Arachnophobia", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }

    [HarmonyPatch]
    internal static class ArachnophobiaSymptomPatches
    {
        [HarmonyPrefix, HarmonyPatch(typeof(SandSpiderAI), nameof(SandSpiderAI.OnCollideWithPlayer))]
        static bool SandSpiderAI_OnCollideWithPlayer_PostFix(SandSpiderAI __instance, Collider other)
        {
            try
            {
                if (!localPlayerAffected["Arachnophobia"]) { return true; }

                if (!__instance.isEnemyDead && !__instance.onWall)
                {
                    PlayerControllerB playerControllerB = __instance.MeetsStandardPlayerCollisionConditions(other, __instance.spoolingPlayerBody);
                    if (playerControllerB != null && __instance.timeSinceHittingPlayer > 1f)
                    {
                        __instance.timeSinceHittingPlayer = 0f;
                        playerControllerB.KillPlayer(Vector3.zero, causeOfDeath: CauseOfDeath.Mauling);
                        __instance.HitPlayerServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
                    }
                }

                return false;
            }
            catch (Exception e)
            {
                logger.LogError(e);
                return true;
            }
        }
    }
}
