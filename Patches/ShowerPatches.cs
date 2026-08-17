using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Configs;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Patches
{
    [HarmonyPatch]
    internal static class ShowerPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ShowerTrigger), nameof(ShowerTrigger.AddPlayerToShower))]
        static void ShowerTrigger_AddPlayerToShower_Postfix(PlayerControllerB playerScript)
        {
            try
            {
                foreach (var disease in playerScript.NetworkObject.GetDiseases())
                {
                    if (disease.isActive)
                    {
                        disease.elapsedTimeMultiplier = 2;
                    }
                    else
                    {
                        playerScript.NetworkObject.RemoveDisease(disease);
                    }
                }
            }
            catch
            {
                return;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ShowerTrigger), nameof(ShowerTrigger.RemovePlayerFromShower))]
        static void ShowerTrigger_RemovePlayerFromShower_Postfix(PlayerControllerB playerScript)
        {
            try
            {
                foreach (var disease in playerScript.NetworkObject.GetDiseases())
                {
                    disease.elapsedTimeMultiplier = 1;
                }
            }
            catch
            {
                return;
            }
        }
    }
}
