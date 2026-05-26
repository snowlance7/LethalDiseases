using GameNetcodeStuff;
using SnowyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Netcode;
using Unity.Services.Authentication.Generated;
using UnityEngine.Serialization;
using static LethalDiseases.Plugin;

namespace LethalDiseases
{
    public static class Diseases
    {
        private static readonly Dictionary<NetworkObject, List<Disease>> diseaseLookup = new();
        private static readonly Dictionary<NetworkObject, List<Disease>> immuneDiseases = new();

        public static List<Disease> GetDiseases(this NetworkObject networkObject)
        {
            if (!diseaseLookup.TryGetValue(networkObject, out var list))
            {
                list = new List<Disease>();
                diseaseLookup[networkObject] = list;
            }

            return list;
        }

        public static List<Disease> GetDiseases(this PlayerControllerB player)
        {
            return player.NetworkObject.GetDiseases();
        }

        public static List<Disease> GetDiseases(this EnemyAI enemy)
        {
            return enemy.NetworkObject.GetDiseases();
        }

        public static List<Disease> GetImmuneDiseases(this NetworkObject networkObject)
        {
            if (!immuneDiseases.TryGetValue(networkObject, out var list))
            {
                list = new List<Disease>();
                immuneDiseases[networkObject] = list;
            }

            return list;
        }

        public static List<Disease> GetImmuneDiseases(this PlayerControllerB player)
        {
            return player.NetworkObject.GetImmuneDiseases();
        }

        public static List<Disease> GetImmuneDiseases(this EnemyAI enemy)
        {
            return enemy.NetworkObject.GetImmuneDiseases();
        }

        public static void AddDisease(this NetworkObject networkObject, Disease disease)
        {
            logger.LogDebug($"Trying to add disease to networkObject with id {networkObject.GetInstanceID()} with disease {disease.name}:{disease.id}");
            if (networkObject.GetImmuneDiseases().Contains(disease)) { return; }

            disease.Init(networkObject);

            logger.LogDebug($"Infected {disease.GetInfectedName()} with disease {disease.name}:{disease.id}");

            networkObject.GetDiseases().Add(disease);
            networkObject.GetImmuneDiseases().Add(disease);
        }

        public static void AddDisease(this PlayerControllerB player, Disease disease)
        {
            player.NetworkObject.AddDisease(disease);
        }

        public static void AddDisease(this EnemyAI enemy, Disease disease)
        {
            enemy.NetworkObject.AddDisease(disease);
        }

        public static void AddDisease(this NetworkObject networkObject, string diseaseId)
        {
            Disease? disease = Disease.GetDiseaseFromID(diseaseId);
            if (disease == null) { return; }
            networkObject.AddDisease(disease);
        }

        public static void AddDisease(this PlayerControllerB player, string diseaseId)
        {
            player.NetworkObject.AddDisease(diseaseId);
        }

        public static void AddDisease(this EnemyAI enemy, string diseaseId)
        {
            enemy.NetworkObject.AddDisease(diseaseId);
        }

        public static void RemoveDisease(this NetworkObject networkObject, Disease disease)
        {
            foreach (var symptomIndex in disease.symptoms)
            {
                networkObject.gameObject.StatusEffectController().RemoveEffect(e => e.source == disease.id);
            }
            networkObject.GetDiseases().Remove(disease);
        }

        public static void RemoveDisease(this PlayerControllerB player, Disease disease)
        {
            player.NetworkObject.RemoveDisease(disease);
        }

        public static void RemoveDisease(this EnemyAI enemy, Disease disease)
        {
            enemy.NetworkObject.RemoveDisease(disease);
        }

        public static void RemoveDisease(this NetworkObject networkObject, string diseaseId)
        {
            Disease? disease = Disease.GetDiseaseFromID(diseaseId);
            if (disease == null) { return; }
            networkObject.RemoveDisease(disease);
        }

        public static void RemoveDisease(this PlayerControllerB player, string diseaseId)
        {
            player.NetworkObject.RemoveDisease(diseaseId);
        }

        public static void RemoveDisease(this EnemyAI enemy, string diseaseId)
        {
            enemy.NetworkObject.RemoveDisease(diseaseId);
        }

        public static void UpdateDiseases(float deltaTime)
        {
            foreach (var diseaseObject in diseaseLookup.ToList())
            {
                var networkObject = diseaseObject.Key;
                var diseases = diseaseObject.Value;
                if (networkObject == null || !networkObject.IsSpawned)
                {
                    diseaseLookup.Remove(diseaseObject.Key);
                    continue;
                }

                foreach (var disease in diseases.ToList())
                {
                    disease.Update(deltaTime);
                }
            }
        }

        public static void LogSpawnedDiseases()
        {
            foreach (var diseaseObject in diseaseLookup)
            {
                logger.LogDebug(diseaseObject.Key.gameObject.name + ":");
                foreach (var disease in diseaseObject.Value)
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
