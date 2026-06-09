using GameNetcodeStuff;
using HarmonyLib;
using SnowyLib;
using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static LethalDiseases.Plugin;
using static LethalDiseases.SymptomAffectedObjects;

namespace LethalDiseases.Symptoms
{
    internal static partial class Symptoms
    {
        [Symptom("Green Needle", "You are never targeted by lightning strikes", Symptom.SymptomType.Good, 50)]
        public static StatusEffect GreenNeedle(Disease disease)
        {
            networkHandler.AddSymptomAffectedObjectServerRpc("Green Needle", disease.networkObject);
            return new OnRemoveActionEffect(() =>
            {
                networkHandler.RemoveSymptomAffectedObjectServerRpc("Green Needle", disease.networkObject);
            }, disease.id, "Green Needle", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }

    [HarmonyPatch]
    internal static class GreenNeedleSymptomPatches
    {
        [HarmonyPrefix, HarmonyPatch(typeof(StormyWeather), nameof(StormyWeather.LightningStrike))]
        static bool StormyWeather_LightningStrike_ServerRpc_PreFix(StormyWeather __instance, Vector3 strikePosition, bool useTargetedObject) // TODO: Test this
        {
            try
            {
                if (!IsServerOrHost) { return true; }
                if (!useTargetedObject) { return true; }
                if (symptomAffectedObjects["Green Needle"].Count <= 0) { return true; }
                foreach (var symptomAffectedObject in symptomAffectedObjects["Green Needle"])
                {
                    if (symptomAffectedObject.TryGetComponent(out PlayerControllerB player))
                    {
                        if (__instance.setStaticGrabbableObject.playerHeldBy == player)
                        {
                            // TODO
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e);
                return;
            }
        }
    }
}
