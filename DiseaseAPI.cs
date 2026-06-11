using SnowyLib;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Disease;
using static LethalDiseases.Plugin;

namespace LethalDiseases
{
    public static class DiseaseAPI
    {
        public static void Infect(this NetworkObject target)
        {
            Disease disease = Disease.CreateRandomDisease();
            target.Infect(disease);
        }

        public static void Infect(this NetworkObject target, Disease disease)
        {
            target.Infect(disease.id);
        }

        public static void Infect(this NetworkObject target, string diseaseId)
        {
            if (target.GetImmuneDiseaseIds().Contains(diseaseId)) { return; }
            LethalDiseasesNetworkHandler.Instance.InfectServerRpc(target, diseaseId);
        }

        public static void TrySpread(this NetworkObject source, NetworkObject target, TransmissionType transmissionType)
        {
            foreach (Disease disease in source.GetDiseases())
            {
                disease.TrySpread(target, transmissionType);
            }
        }

        public static void TrySpreadBetween(NetworkObject source1, NetworkObject source2, TransmissionType transmissionType)
        {
            foreach (Disease disease in source1.GetDiseases())
            {
                disease.TrySpread(source2, transmissionType);
            }
            foreach (Disease disease in source2.GetDiseases())
            {
                disease.TrySpread(source1, transmissionType);
            }
        }

        public static bool HasDisease(this NetworkObject target)
        {
            return target.GetDiseases().Count > 0;
        }

        public static List<Disease> GetDiseases(this NetworkObject target)
        {
            return target.GetHost().Diseases;
        }

        public static List<Disease> GetImmuneDiseases(this NetworkObject target)
        {
            return target.GetHost().ImmuneDiseases;
        }

        public static List<string> GetDiseaseIds(this NetworkObject target)
        {
            return target.GetHost().DiseaseIds;
        }

        public static List<string> GetImmuneDiseaseIds(this NetworkObject target)
        {
            return target.GetHost().ImmuneDiseaseIds;
        }

        internal static DiseaseHost GetHost(this NetworkObject target)
        {
            if (!target.TryGetComponent(out DiseaseHost host))
            {
                host = target.gameObject.AddComponent<DiseaseHost>();
            }

            return host;
        }

        public static void AddDisease(this NetworkObject networkObject, Disease disease)
        {
            logger.LogDebug($"Trying to add disease to networkObject with id {networkObject.GetInstanceID()} with disease {disease.name}:{disease.id}");
            if (networkObject.GetImmuneDiseases().Contains(disease)) { return; }

            disease.Init(networkObject);

            logger.LogDebug($"Infected {disease.GetInfectedName()} with disease {disease.name}:{disease.id}");

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
            networkObject.GetDiseases().Clear();
            networkObject.GetDiseaseIds().Clear();

            if (!clearImmune) { return; }

            networkObject.GetImmuneDiseases().Clear();
            networkObject.GetImmuneDiseaseIds().Clear();
        }

        public static void LogSpawnedDiseases()
        {
            foreach (var diseaseHost in GameObject.FindObjectsOfType<DiseaseHost>())
            {
                logger.LogDebug(diseaseHost.gameObject.name + ":");
                foreach (var disease in diseaseHost.Diseases)
                {
                    logger.LogDebug($"- {disease.name}:{disease.id}");
                    foreach (var symptom in disease.symptoms)
                    {
                        logger.LogDebug($"-- {Symptom.symptomList[symptom].name}");
                    }
                }
            }
        }
    }
}
