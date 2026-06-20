using GameNetcodeStuff;
using SnowyLib;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using Unity.Services.Authentication.Generated;
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
            netObj.Infect(disease.id);
        }

        public static void Infect(this NetworkObject netObj, string diseaseId)
        {
            if (netObj.GetImmuneDiseaseIds().Contains(diseaseId)) { return; }
            LethalDiseasesNetworkHandler.Instance.InfectServerRpc(netObj, diseaseId);
        }
        // TODO: Make more helper methods like these
        public static void Infect(this PlayerControllerB player) => player.NetworkObject.Infect();
        public static void Infect(this PlayerControllerB player, Disease disease) => player.NetworkObject.Infect(disease);
        public static void Infect(this PlayerControllerB player, string diseaseId) => player.NetworkObject.Infect(diseaseId);
        public static void Infect(this EnemyAI enemy) => enemy.NetworkObject.Infect();
        public static void Infect(this EnemyAI enemy, Disease disease) => enemy.NetworkObject.Infect(disease);
        public static void Infect(this EnemyAI enemy, string diseaseId) => enemy.NetworkObject.Infect(diseaseId);

        public static void TrySpread(this NetworkObject source, NetworkObject netObj, TransmissionType transmissionType)
        {
            foreach (Disease disease in source.GetDiseases())
            {
                disease.TrySpread(netObj, transmissionType);
            }
        }

        public static void TrySpreadBetween(NetworkObject netObj1, NetworkObject netObj2, TransmissionType transmissionType)
        {
            foreach (Disease disease in netObj1.GetDiseases())
            {
                disease.TrySpread(netObj2, transmissionType);
            }
            foreach (Disease disease in netObj2.GetDiseases())
            {
                disease.TrySpread(netObj1, transmissionType);
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
            if (!netObj.TryGetComponent(out DiseaseHost host))
            {
                host = netObj.gameObject.AddComponent<DiseaseHost>();
            }

            return host;
        }

        public static void AddDisease(this NetworkObject networkObject, Disease disease)
        {
            logger?.LogDebug($"Trying to add disease to networkObject with id {networkObject.GetInstanceID()} with disease {disease.name}:{disease.id}");
            if (networkObject.GetImmuneDiseases().Contains(disease)) { return; }

            disease.Init(networkObject);

            logger?.LogDebug($"Infected {disease.GetInfectedName()} with disease {disease.name}:{disease.id}");

            string id = disease.id;

            networkObject.GetDiseaseIds().Add(id);
            networkObject.GetImmuneDiseaseIds().Add(id);
            networkObject.GetDiseases().Add(disease);
            networkObject.GetImmuneDiseases().Add(disease);
        }

        public static void AddDisease(this NetworkObject networkObject, string diseaseId)
        {
            Disease? disease = Disease.GetDiseaseFromID(diseaseId);
            if (disease == null) { return; }
            networkObject.AddDisease(disease);
        }

        public static void RemoveDisease(this NetworkObject networkObject, Disease disease)
        {
            foreach (var symptomIndex in disease.symptoms)
            {
                networkObject.gameObject.StatusEffectController().RemoveEffect(e => e.source == disease.id);
            }
            networkObject.GetDiseases().Remove(disease);
            networkObject.GetDiseaseIds().Remove(disease.id);
        }

        public static void RemoveDisease(this NetworkObject networkObject, string diseaseId)
        {
            Disease? disease = Disease.GetDiseaseFromID(diseaseId);
            if (disease == null) { return; }
            networkObject.RemoveDisease(disease);
        }

        public static void ClearDiseases(this NetworkObject networkObject, bool clearImmune = false)
        {
            foreach (var disease in networkObject.GetDiseases()) // TODO: Make sure this is correct when im not high
                networkObject.RemoveDisease(disease);
            //networkObject.GetDiseases().Clear();
            networkObject.GetDiseaseIds().Clear();

            if (!clearImmune) { return; }

            networkObject.GetImmuneDiseases().Clear();
            networkObject.GetImmuneDiseaseIds().Clear();
        }

        public static void LogSpawnedDiseases()
        {
            foreach (var diseaseHost in GameObject.FindObjectsOfType<DiseaseHost>())
            {
                logger?.LogDebug(diseaseHost.gameObject.name + ":");
                foreach (Disease disease in diseaseHost.Diseases)
                {
                    logger?.LogDebug($"- {disease.name}:{disease.id}");
                    foreach (var symptom in disease.symptoms)
                    {
                        logger?.LogDebug($"-- {Symptom.symptomList[symptom].name}");
                    }
                }
            }
        }
    }
}
