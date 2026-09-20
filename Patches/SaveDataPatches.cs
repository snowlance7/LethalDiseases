using HarmonyLib;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Patches
{
    [HarmonyPatch]
    internal static class SaveDataPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
        static void StartOfRound_Start_Postfix(StartOfRound __instance)
        {
            try
            {
                Disease.LoadNamedDiseases();
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.SaveGame))]
        static void GameNetworkManager_SaveGame_Postfix(GameNetworkManager __instance)
        {
            try
            {
                Disease.SaveNamedDiseases();
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }
    }
}
