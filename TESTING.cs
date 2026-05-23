using HarmonyLib;
using SnowyLib;
using System.Linq;
using UnityEngine;
using static LethalDiseases.Plugin;

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
    public class TESTING : MonoBehaviour
    {
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
                Plugin.logger.LogDebug(msg);

                switch (args[0])
                {
                    case "/infect":
                        if (args.Length == 1)
                        {
                            Disease disease = Disease.CreateRandomDisease();
                            //LethalDiseasesNetworkHandler.Instance.InfectServerRpc(localPlayer.NetworkObject, disease.id);
                        }
                        else
                        {
                            Disease disease = Disease.CreateRandomDiseaseWithSymptom(Symptom.GetSymptomIndexByName(args[1]));
                            //LethalDiseasesNetworkHandler.Instance.InfectServerRpc(localPlayer.NetworkObject, disease.id);
                        }
                        break;
                    case "/symptoms":
                        foreach (var symptom in Symptom.symptomList)
                        {
                            logger.LogDebug($"{symptom.name}: {symptom.description}");
                        }
                        break;
                    default:
                        Utils.ChatCommand(args);
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