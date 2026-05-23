using HarmonyLib;
using SnowyLib;
using System;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.UniqueSymptoms
{
    internal static class ShopaholicSymptom
    {
        public static bool localPlayerAffected;

        [Symptom("Shopaholic", "25% chance to buy a random item when using the terminal", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Shopaholic(Disease disease)
        {
            if (disease.player != null)
                localPlayerAffected = true;
            return new OnRemoveActionEffect(() =>
            {
                if (disease.player == null) { return; }
                localPlayerAffected = false;
            }, disease.id, "Shopaholic", disease.strengthTime, Symptoms.SetHighestDurationAndDeny);
        }
    }

    [HarmonyPatch]
    public class ShopaholicSymptomPatches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(Terminal), nameof(Terminal.BeginUsingTerminal))]
        static void Terminal_BeginUsingTerminalPostFix(Terminal __instance)
        {
            try
            {
                if (ShopaholicSymptom.localPlayerAffected && UnityEngine.Random.Range(0, 4) == 0) // TODO: Test
                {
                    int index = UnityEngine.Random.Range(0, __instance.buyableItemsList.Length);
                    __instance.groupCredits -= __instance.buyableItemsList[index].creditsWorth;
                    __instance.groupCredits = Mathf.Max(__instance.groupCredits, 0);
                    __instance.orderedItemsFromTerminal.Add(index);
                    __instance.numberOfItemsInDropship++;
                    __instance.BuyItemsServerRpc(__instance.orderedItemsFromTerminal.ToArray(), __instance.groupCredits, __instance.numberOfItemsInDropship);
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
