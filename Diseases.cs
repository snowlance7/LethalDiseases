using SnowyLib;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases
{
    internal static class Diseases
    {
        public static void AddDisease(this NetworkObject networkObject, Disease disease)
        {
            logger.LogDebug($"Trying to add disease to networkObject with id {networkObject.GetInstanceID()} with disease {disease.name}:{disease.id}");
            if (networkObject.GetImmuneDiseases().Contains(disease)) { return; }

            disease.Init(networkObject);

            logger.LogDebug($"Infected {disease.GetInfectedName()} with disease {disease.name}:{disease.id}");

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
        }

        public static void RemoveDisease(this NetworkObject networkObject, string diseaseId)
        {
            Disease? disease = Disease.GetDiseaseFromID(diseaseId);
            if (disease == null) { return; }
            networkObject.RemoveDisease(disease);
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
