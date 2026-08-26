using HarmonyLib;
using LethalDiseases.Items;
using SnowyLib;
using System;
using System.Collections;
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
    public static class TESTING
    {
        public static bool disableDiseaseSpawning = false;
        static Disease? nextEnemyDisease;

        static bool goToNextSymptomInTest;
        static bool symptomsTestRunning;

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

                switch (args[0])
                {
                    case "/infect":
                        Debug_Infect(args);
                        break;
                    case "/infectnow":
                        Debug_Infect(args, true);
                        break;
                    case "/symptoms":
                        HUDManager.Instance.DisplayTip("Server", "Logging symptoms");
                        foreach (var symptom in Symptom.symptomList)
                        {
                            logger?.LogDebug($"{symptom.name}: {symptom.description}");
                        }
                        break;
                    case "/next":
                        goToNextSymptomInTest = true;
                        break;
                    case "/symptomstest": // TODO: Use this for testing

                        RunSymptomsTest();
                        
                        break;
                    case "/symptomstestall": // TODO: Use this for testing

                        RunSymptomsTestAll();

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

        public static void RunSymptomsTest()
        {
            logger.LogDebug("RunSymptomsTest");
            HUDManager.Instance.DisplayTip("Server", "Starting symptoms test in 5 seconds, use /next for the next disease");

            IEnumerator symptomsTest()
            {
                yield return null;

                symptomsTestRunning = true;

                yield return new WaitForSeconds(5f);

                foreach (var symptom in Symptom.symptomList)
                {
                    HUDManager.Instance.DisplayTip(symptom.name, symptom.description);
                    Disease disease = Disease.CreateRandomDiseaseWithSymptom(Symptom.symptomList.IndexOf(symptom));
                    disease.latency = 0;
                    localPlayer.Infect(disease);
                    yield return new WaitUntil(() => goToNextSymptomInTest);
                    goToNextSymptomInTest = false;
                }

                symptomsTestRunning = false;
            }

            NetworkHandler.Instance.StartCoroutine(symptomsTest());
        }

        public static void RunSymptomsTestAll()
        {
            HUDManager.Instance.DisplayTip("Server", "Starting symptoms test all in 5 seconds, good luck");

            IEnumerator symptomsTestAll()
            {
                yield return null;

                symptomsTestRunning = true;

                yield return new WaitForSeconds(5f);

                foreach (var symptom in Symptom.symptomList)
                {
                    Disease disease = Disease.CreateRandomDiseaseWithSymptom(Symptom.symptomList.IndexOf(symptom));
                    disease.latency = 0;
                    disease.strength = 1;
                    disease.stability = 1;
                    disease.transmissibility = 0;
                    localPlayer.Infect(disease);
                    yield return new WaitForSeconds(0.2f);
                }

                symptomsTestRunning = false;
            }

            NetworkHandler.Instance.StartCoroutine(symptomsTestAll());
        }

        [HarmonyPostfix, HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.Start))]
        public static void EnemyAI_Start_PostFix(EnemyAI __instance)
        {
            try
            {
                if (!Utils.testing || nextEnemyDisease == null) { return; }
                __instance.Infect(nextEnemyDisease);
            }
            catch
            {
                return;
            }
            finally
            {
                nextEnemyDisease = null;
            }
        }

        static void Debug_Infect(string[] args, bool now = false)
        {
            Disease disease;
            if (args.Length == 1 && localPlayer.currentlyHeldObjectServer == null)
            {
                disease = Disease.CreateRandomDisease();
                if (now) { disease.latency = 0; }
                localPlayer.NetworkObject.Infect(disease);
                HUDManager.Instance.DisplayTip("LethalDiseases", $"Infected local player with disease {disease.ToString()}");
                return;
            }

            string symptomName;
            int symptomIndex;

            switch (args[1])
            {
                case "enemy":
                    if (args.Length == 2)
                    {
                        disease = Disease.CreateRandomDisease();
                        if (now) { disease.latency = 0; }
                        nextEnemyDisease = disease;
                        HUDManager.Instance.DisplayTip("LethalDiseases", $"Infecting next enemy spawn with disease {disease.ToString()}");
                        return;
                    }

                    symptomName = args[1].Replace("_", " ");
                    symptomIndex = Symptom.GetSymptomIndexByName(symptomName);
                    if (symptomIndex == -1) { HUDManager.Instance.DisplayTip("LethalDiseases", $"Couldn't find symptom with name '{symptomName}'"); return; }
                    disease = Disease.CreateRandomDiseaseWithSymptom(symptomIndex);

                    if (now) { disease.latency = 0; }
                    nextEnemyDisease = disease;
                    HUDManager.Instance.DisplayTip("LethalDiseases", $"Infecting next enemy spawn with disease {disease.ToString()}"); break;
                default:
                    if (localPlayer.currentlyHeldObjectServer != null)
                    {
                        if (args.Length == 2)
                        {
                            disease = Disease.CreateRandomDisease();
                            if (now) { disease.latency = 0; }
                            localPlayer.currentlyHeldObjectServer.NetworkObject.Infect(disease);
                            HUDManager.Instance.DisplayTip("LethalDiseases", $"Infected {localPlayer.currentlyHeldObjectServer.itemProperties.itemName} with disease {disease.ToString()}");
                            return;
                        }

                        symptomName = args[1].Replace("_", " ");
                        symptomIndex = Symptom.GetSymptomIndexByName(symptomName);
                        if (symptomIndex == -1) { HUDManager.Instance.DisplayTip("LethalDiseases", $"Couldn't find symptom with name '{symptomName}'"); return; }
                        disease = Disease.CreateRandomDiseaseWithSymptom(symptomIndex);

                        if (now) { disease.latency = 0; }
                        localPlayer.currentlyHeldObjectServer.NetworkObject.Infect(disease);
                        HUDManager.Instance.DisplayTip("LethalDiseases", $"Infected {localPlayer.currentlyHeldObjectServer.itemProperties.itemName} with disease {disease.ToString()}");
                    }
                    else
                    {
                        symptomName = args[1].Replace("_", " ");
                        symptomIndex = Symptom.GetSymptomIndexByName(symptomName);
                        if (symptomIndex == -1) { HUDManager.Instance.DisplayTip("LethalDiseases", $"Couldn't find symptom with name '{symptomName}'"); return; }
                        disease = Disease.CreateRandomDiseaseWithSymptom(symptomIndex);

                        if (now) { disease.latency = 0; }
                        localPlayer.NetworkObject.Infect(disease);
                        HUDManager.Instance.DisplayTip("LethalDiseases", $"Infected local player with disease {disease.ToString()}");
                    }
                    break;
            }
        }
    }
}