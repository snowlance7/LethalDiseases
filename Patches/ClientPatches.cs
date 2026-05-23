using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Patches
{
    [HarmonyPatch]
    internal class ClientPatches
    {

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InteractTrigger), nameof(InteractTrigger.Interact))]
        private static void InteractTrigger_Interact_Postfix(InteractTrigger __instance, Transform playerTransform)
        {
            try
            {
                if (!playerTransform.TryGetComponent(out PlayerControllerB player)) { return; }

                List<Disease> diseases = __instance.NetworkObject.GetDiseases();
                foreach (var disease in diseases)
                {
                    disease.TrySpread(player.NetworkObject);
                }

                diseases = player.NetworkObject.GetDiseases();
                foreach (var disease in diseases)
                {
                    disease.TrySpread(__instance.NetworkObject);
                }
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }
    }
}