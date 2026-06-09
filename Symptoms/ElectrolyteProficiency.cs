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
        [Symptom("ElectrolyteProficiency", "You charge items you hold instead of depleting them", Symptom.SymptomType.Good, 50)]
        public static StatusEffect ElectrolyteProficiency(Disease disease)
        {
            if (disease.player != null)
                localPlayerAffected["ElectrolyteProficiency"] = true;
            return new OnRemoveActionEffect(() =>
            {
                if (disease.player == null) { return; }
                localPlayerAffected["ElectrolyteProficiency"] = false;
            }, disease.id, "ElectrolyteProficiency", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }

    [HarmonyPatch]
    internal static class ElectrolyteProficiencySymptomPatches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Update))]
        static void GrabbableObject_UpdatePostfix(GrabbableObject __instance)
        {
            try
            {
                if (!localPlayerAffected["ElectrolyteProficiency"]) { return; }
                if (__instance.IsOwner)
                {
                    if (__instance.isBeingUsed && __instance.itemProperties.requiresBattery)
                    {
                        if (!__instance.itemProperties.itemIsTrigger)
                        {
                            __instance.insertedBattery.charge += 2 * (Time.deltaTime / __instance.itemProperties.batteryUsage);
                            __instance.insertedBattery.empty = false;
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

        [HarmonyPostfix, HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.UseItemBatteries))]
        static void GrabbableObject_UseItemBatteriesPostfix(GrabbableObject __instance, ref bool __result, bool isToggle, bool buttonDown)
        {
            try
            {
                if (!localPlayerAffected["ElectrolyteProficiency"]) { return; }

                if (__instance.itemProperties.requiresBattery && __instance.insertedBattery == null)
                {
                    return;
                }
                if (__instance.itemProperties.itemIsTrigger)
                {
                    __instance.insertedBattery.charge = Mathf.Clamp(__instance.insertedBattery.charge + 2 * (__instance.itemProperties.batteryUsage), 0f, 1f);
                    __instance.insertedBattery.empty = false;
                    __instance.isBeingUsed = false;
                }
                else if (__instance.itemProperties.automaticallySetUsingPower)
                {
                    if (isToggle)
                    {
                        __instance.isBeingUsed = !__instance.isBeingUsed;
                    }
                    else
                    {
                        __instance.isBeingUsed = buttonDown;
                    }
                }
                __result = true;
                return;
            }
            catch (Exception e)
            {
                logger.LogError(e);
                return;
            }
        }
    }
}
