using GameNetcodeStuff;
using SnowyLib;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Disease;
using static LethalDiseases.Plugin;

namespace LethalDiseases
{
    public static class DiseaseAPI
    {
        public static void Infect(this NetworkObject netObj)
        {
            Disease disease = Disease.CreateRandomDisease();
            netObj.Infect(disease);
        }

        public static void Infect(this NetworkObject netObj, Disease disease)
        {
            netObj.Infect(disease.ToString());
        }

        public static void Infect(this NetworkObject netObj, string diseaseId)
        {
            if (netObj.GetImmuneDiseaseIds().Contains(diseaseId)) { return; }
            netObj.GetHost().InfectRpc(diseaseId);
        }
        // TODO: Make more helper methods like these
        public static void Infect(this PlayerControllerB player) => player.NetworkObject.Infect();
        public static void Infect(this PlayerControllerB player, Disease disease) => player.NetworkObject.Infect(disease);
        public static void Infect(this PlayerControllerB player, string diseaseId) => player.NetworkObject.Infect(diseaseId);
        public static void Infect(this EnemyAI enemy) => enemy.NetworkObject.Infect();
        public static void Infect(this EnemyAI enemy, Disease disease) => enemy.NetworkObject.Infect(disease);
        public static void Infect(this EnemyAI enemy, string diseaseId) => enemy.NetworkObject.Infect(diseaseId);

        public static void TrySpread(this NetworkObject source, NetworkObject netObj, TransmissionType transmissionType, float multiplier = 1f)
        {
            foreach (Disease disease in source.GetDiseases())
            {
                disease.TrySpread(netObj, transmissionType, multiplier);
            }
        }

        public static void TrySpreadBetween(NetworkObject netObj1, NetworkObject netObj2, TransmissionType transmissionType, float multiplier = 1f)
        {
            foreach (Disease disease in netObj1.GetDiseases())
            {
                disease.TrySpread(netObj2, transmissionType, multiplier);
            }
            foreach (Disease disease in netObj2.GetDiseases())
            {
                disease.TrySpread(netObj1, transmissionType, multiplier);
            }
        }

        public static bool HasDisease(this NetworkObject netObj)
        {
            return netObj.GetDiseases().Count > 0;
        }

        public static List<Disease> GetDiseases(this NetworkObject netObj)
        {
            return netObj.GetHost().Diseases;
        }

        public static List<Disease> GetImmuneDiseases(this NetworkObject netObj)
        {
            return netObj.GetHost().ImmuneDiseases;
        }

        public static List<string> GetDiseaseIds(this NetworkObject netObj)
        {
            return netObj.GetHost().DiseaseIds;
        }

        public static List<string> GetImmuneDiseaseIds(this NetworkObject netObj)
        {
            return netObj.GetHost().ImmuneDiseaseIds;
        }

        internal static DiseaseHost GetHost(this NetworkObject netObj)
        {
            if (!netObj.gameObject.TryGetComponent(out DiseaseHost host))
            {
                host = netObj.gameObject.AddComponent<DiseaseHost>();
            }

            return host;
        }

        internal static DiseaseHost GetHost(this PlayerControllerB player)
        {
            return player.NetworkObject.GetHost();
        }

        internal static DiseaseHost GetHost(this EnemyAI enemy)
        {
            return enemy.NetworkObject.GetHost();
        }

        public static void AddDisease(this NetworkObject networkObject, Disease disease)
        {
            networkObject.GetHost().AddDisease(disease);
        }

        public static void AddDisease(this NetworkObject networkObject, string diseaseId)
        {
            Disease? disease = Disease.GetDiseaseFromString(diseaseId);
            if (disease == null) { return; }
            networkObject.AddDisease(disease);
        }

        public static void RemoveDisease(this NetworkObject networkObject, Disease disease)
        {
            networkObject.GetHost().RemoveDisease(disease);
        }

        public static void RemoveDisease(this NetworkObject networkObject, string diseaseId)
        {
            Disease? disease = Disease.GetDiseaseFromString(diseaseId);
            if (disease == null) { return; }
            networkObject.RemoveDisease(disease);
        }

        public static void ClearDiseases(this NetworkObject networkObject, bool clearImmune = false)
        {
            networkObject.GetHost().ClearDiseases(clearImmune);
        }

        public static void LogSpawnedDiseases()
        {
            foreach (var diseaseHost in GameObject.FindObjectsOfType<DiseaseHost>())
            {
                if (!diseaseHost.hasDisease) { continue; }
                logger?.LogInfo(diseaseHost.gameObject.name + ":");
                List<string> diseaseNames = [];
                foreach (Disease disease in diseaseHost.Diseases)
                {
                    string diseaseName = $"{disease.name}:{disease.ToString()}";
                    diseaseNames.Add(diseaseName);
                    logger?.LogInfo("- " + diseaseName);
                    foreach (var symptom in disease.symptoms)
                    {
                        logger?.LogInfo($"-- {Symptom.RegisteredSymptoms[symptom].name}");
                    }
                }
                Utils.Ping(diseaseHost.diseaseScanNode.scanNode.transform.position, diseaseHost.gameObject.name, string.Join("|", diseaseNames), destroyTime: 30f);
            }
        }
    }
}
