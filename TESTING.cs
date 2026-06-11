using Dawn.Utils;
using HarmonyLib;
using SnowyLib;
using System.Linq;
using UnityEngine;
using static LethalDiseases.Plugin;
using Dawn;

/* bodyparts
 * 0 head
 * 1 right arm
 * 2 left arm
 * 3 right leg
 * 4 left leg
 * 5 chest
 * 6 feet
 * 7 right hip
 * 8 crotch
 * 9 left shoulder
 * 10 right shoulder */

namespace LethalDiseases
{
    [HarmonyPatch]
    public static class TESTING
    {
        public static bool disableDiseaseSpawning = false;

        [HarmonyPostfix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.PingScan_performed))]
        public static void PingScan_performedPostFix()
        {
            try
            {
                if (!Utils.testing) { return; }
                //RoundManager.Instance.ShowStaticElectricityWarningServerRpc(localPlayer.NetworkObject, 10f);
            }
            catch
            {
                return;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.SubmitChat_performed))]
        public static void SubmitChat_performedPrefix(HUDManager __instance)
        {
            try
            {
                if (!Utils.testing) { return; }
                string msg = __instance.chatTextField.text;
                string[] args = msg.Split(" ");
                Plugin.logger?.LogDebug(msg);

                switch (args[0])
                {
                    case "/infect":
                        HUDManager.Instance.DisplayTip("Server", "Infecting local player");
                        if (args.Length == 1)
                        {
                            localPlayer.NetworkObject.Infect();
                        }
                        else
                        {
                            string symptomName = args[1].Replace("_", " ");
                            Disease disease = Disease.CreateRandomDiseaseWithSymptom(Symptom.GetSymptomIndexByName(symptomName));
                            localPlayer.NetworkObject.Infect(disease);
                        }
                        break;
                    case "/infectnow":
                        HUDManager.Instance.DisplayTip("Server", "Infecting local player now");
                        if (args.Length == 1)
                        {
                            Disease disease = Disease.CreateRandomDisease();
                            disease.latency = 0;
                            localPlayer.NetworkObject.Infect(disease);
                        }
                        else
                        {
                            string symptomName = args[1].Replace("_", " ");
                            Disease disease = Disease.CreateRandomDiseaseWithSymptom(Symptom.GetSymptomIndexByName(symptomName));
                            disease.latency = 0;
                            localPlayer.NetworkObject.Infect(disease);
                        }
                        break;
                    case "/symptoms":
                        HUDManager.Instance.DisplayTip("Server", "Logging symptoms");
                        foreach (var symptom in Symptom.symptomList)
                        {
                            logger?.LogDebug($"{symptom.name}: {symptom.description}");
                        }
                        break;
                    case "/diseases":
                        if (args.Length == 1)
                        {
                            HUDManager.Instance.DisplayTip("Server", "Logging diseases");
                            DiseaseAPI.LogSpawnedDiseases();
                            return;
                        }

                        switch (args[1])
                        {
                            case "spawning":
                                disableDiseaseSpawning = !disableDiseaseSpawning;
                                HUDManager.Instance.DisplayTip("Server", "disableDiseaseSpawning = " + disableDiseaseSpawning);
                                break;
                            case "clear":
                                HUDManager.Instance.DisplayTip("Server", "Clearing diseases on local player");
                                localPlayer.NetworkObject.ClearDiseases(clearImmune: true);
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }
            }
            catch
            {
                return;
            }
        }
    }
}