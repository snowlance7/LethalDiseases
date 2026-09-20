//using HarmonyLib;
//using System;
//using System.Collections.Generic;
//using System.Text;
//using static LethalDiseases.Plugin;

//namespace LethalDiseases.Patches
//{
//    [HarmonyPatch]
//    internal static class SaveDataPatches
//    {
//        [HarmonyPostfix]
//        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.LoadShipGrabbableItems))]
//        static void StartOfRound_LoadShipGrabbableItems_Postfix(StartOfRound __instance)
//        {
//            try
//            {

//            }
//            catch (System.Exception e)
//            {
//                logger.LogError(e);
//                return;
//            }
//        }

//        [HarmonyPostfix]
//        [HarmonyPatch(typeof(StartOfRound), nameof(GameNetworkManager.SaveGame))]
//        static void StartOfRound_LoadShipGrabbableItems_Postfix(StartOfRound __instance)
//        {
//            try
//            {

//            }
//            catch (System.Exception e)
//            {
//                logger.LogError(e);
//                return;
//            }
//        }
//    }
//}
