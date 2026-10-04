using Dawn;
using HarmonyLib;
using System.Linq;
using static LethalDiseases.Configs;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Patches
{
    [HarmonyPatch]
    internal static class GenerationPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.Start))]
        static void EnemyAI_Start_Postfix(EnemyAI __instance)
        {
            try
            {
                if (!__instance.IsServer || TESTING.disableDiseaseSpawning || Configs.UninfectableEnemies.Contains(__instance.enemyType.GetDawnInfo().TypedKey.ToString())) { return; }

                if (UnityEngine.Random.Range(0f, 1f) < MonsterDiseaseChance)
                    __instance.NetworkObject.Infect();
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SteamValveHazard), nameof(SteamValveHazard.Start))]
        static void SteamValveHazard_Start_Postfix(SteamValveHazard __instance)
        {
            try
            {
                if (!__instance.fixInteract.IsServer || TESTING.disableDiseaseSpawning) { return; }

                if (UnityEngine.Random.Range(0f, 1f) < SteamDiseaseChance)
                    __instance.fixInteract.NetworkObject.Infect();
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
        static void GrabbableObject_Start_Postfix(GrabbableObject __instance)
        {
            try
            {
                if (!__instance.IsServer || TESTING.disableDiseaseSpawning || __instance.isInShipRoom || Configs.UninfectableItems.Contains(__instance.itemProperties.GetDawnInfo().TypedKey.ToString())) { return; }

                if (UnityEngine.Random.Range(0f, 1f) < ScrapDiseaseChance)
                    __instance.NetworkObject.Infect();
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }
    }
}
