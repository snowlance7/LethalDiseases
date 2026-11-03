using Dawn;
using GameNetcodeStuff;
using HarmonyLib;
using SnowyLib;
using System.Linq;
using Unity.Netcode;
using static LethalDiseases.Configs;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Patches
{
    [HarmonyPatch]
    internal static class GenerationPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.Start))]
        static void PlayerControllerB_Start_Postfix(PlayerControllerB __instance)
        {
            try
            {
                var host = __instance.gameObject.GetComponent<DiseaseHost>();
                host ??= __instance.gameObject.AddComponent<DiseaseHost>();
                host.SetHost(__instance);
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.Start))]
        static void EnemyAI_Start_Postfix(EnemyAI __instance)
        {
            try
            {
                var host = __instance.gameObject.GetComponent<DiseaseHost>();
                host ??= __instance.gameObject.AddComponent<DiseaseHost>();
                host.SetHost(__instance);

                if (!__instance.IsServer) { return; }

                if (Utils.testing && TESTING.nextEnemyDisease != null)
                {
                    host.InfectSynced(TESTING.nextEnemyDisease);
                    TESTING.nextEnemyDisease = null;
                    return;
                }

                if (TESTING.disableDiseaseSpawning || Configs.UninfectableEnemies.Contains(__instance.enemyType.GetDawnInfo().TypedKey.ToString())) { return; }

                if (UnityEngine.Random.Range(0f, 1f) < MonsterDiseaseChance)
                    __instance.gameObject.GetComponent<NetworkObject>().InfectSynced();
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
                var host = __instance.gameObject.GetComponent<DiseaseHost>();
                host ??= __instance.gameObject.AddComponent<DiseaseHost>();
                host.SetHost(__instance);

                if (!__instance.fixInteract.IsServer || TESTING.disableDiseaseSpawning) { return; }

                if (UnityEngine.Random.Range(0f, 1f) < SteamDiseaseChance)
                    __instance.fixInteract.gameObject.GetComponent<NetworkObject>().InfectSynced();
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
                var host = __instance.gameObject.GetComponent<DiseaseHost>();
                host ??= __instance.gameObject.AddComponent<DiseaseHost>();
                host.SetHost(__instance);

                if (!__instance.IsServer || TESTING.disableDiseaseSpawning || __instance.isInShipRoom || Configs.UninfectableItems.Contains(__instance.itemProperties.GetDawnInfo().TypedKey.ToString())) { return; }

                if (UnityEngine.Random.Range(0f, 1f) < ScrapDiseaseChance)
                    __instance.gameObject.GetComponent<NetworkObject>().InfectSynced();
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }
    }
}
