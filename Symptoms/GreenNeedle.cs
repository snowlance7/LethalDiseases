using DigitalRuby.ThunderAndLightning;
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
            networkHandler.AddSymptomAffectedObjectRpc("Green Needle", disease.networkObject);
            return new OnRemoveActionEffect(() =>
            {
                networkHandler.RemoveSymptomAffectedObjectRpc("Green Needle", disease.networkObject);
            }, disease.id, "Green Needle", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }

    [HarmonyPatch]
    internal static class GreenNeedleSymptomPatches
    {
        [HarmonyPrefix, HarmonyPatch(typeof(StormyWeather), nameof(StormyWeather.LightningStrike))]
        static bool StormyWeather_LightningStrike_PreFix(StormyWeather __instance, Vector3 strikePosition, bool useTargetedObject) // TODO: Test this
        {
            try
            {
                if (symptomAffectedObjects["Green Needle"].Count <= 0) { return true; }
                foreach (var symptomAffectedObject in symptomAffectedObjects["Green Needle"])
                {
                    if (symptomAffectedObject == null || !symptomAffectedObject.IsSpawned) { continue; }
                    if ((symptomAffectedObject.gameObject.transform.position - strikePosition).sqrMagnitude > 4) { continue; }
                    if (useTargetedObject)
                    {
                        __instance.staticElectricityParticle.Stop();
                        __instance.staticElectricityParticle.GetComponent<AudioSource>().Stop();
                        __instance.setStaticToObject = null;
                    }
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                logger.LogError(e);
                return true;
            }
        }
    }
}
