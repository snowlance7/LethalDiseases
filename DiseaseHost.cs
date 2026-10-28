using GameNetcodeStuff;
using LethalDiseases.Items;
using SnowyLib;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases
{
    public class DiseaseHost : MonoBehaviour
    {
        public static HashSet<DiseaseHost> Instances { get; private set; } = new HashSet<DiseaseHost>();

        public List<Disease> Diseases = [];
        public List<Disease> ImmuneDiseases = [];
        public List<string> DiseaseIds = [];
        public List<string> ImmuneDiseaseIds = [];

        public NetworkObject? networkObject
        {
            get
            {
                if (player != null) { return player.NetworkObject; }
                if (enemy != null) { return enemy.NetworkObject; }
                if (steamValve != null) { return steamValve.fixInteract.NetworkObject; }
                if (grabbableObject != null) { return grabbableObject.NetworkObject; }
                return null;
            }
        }
        public PlayerControllerB? player { get; private set; }
        public EnemyAI? enemy { get; private set; }
        internal SteamValveHazard? steamValve { get; private set; }
        internal GrabbableObject? grabbableObject { get; private set; }

        public DiseaseScanNode? diseaseScanNode;

        public bool hasActor => enemy != null || player != null;

        public bool hasDisease => Diseases.Count > 0;

        public float sanitizedTimeLeft;
        public bool IsSanitized => sanitizedTimeLeft > 0;

        public float airFilterQuality = 1f;
        public bool IsAirFilterFunctional => airFilterQuality > 0f;

        void Start()
        {
            Instances.Add(this);
            player = gameObject.GetComponent<PlayerControllerB>();
            enemy = gameObject.GetComponent<EnemyAI>();
            steamValve = gameObject.GetComponent<SteamValveHazard>();
            grabbableObject = gameObject.GetComponent<GrabbableObject>();

            CreateDiseaseScanNode();
        }

        public void OnDestroy()
        {
            Instances.Remove(this);
        }

        void Update()
        {
            if (IsSanitized)
            {
                sanitizedTimeLeft -= Time.deltaTime;

                if (sanitizedTimeLeft <= 0)
                    Utils.DisplayStatusEffect("Hands are no longer sanitized");
            }

            foreach (var disease in Diseases.ToList())
            {
                disease.Update(Time.deltaTime);
            }
        }

        public void AddDisease(Disease disease)
        {
            if (ImmuneDiseases.Contains(disease)) { return; }
            var id = disease.ToString();
            disease.host = this;
            disease.frozen = false;
            DiseaseIds.Add(id);
            ImmuneDiseaseIds.Add(id);
            Diseases.Add(disease);
            ImmuneDiseases.Add(disease);

            logger?.LogDebug($"Infected {GetInfectedName()} with disease {disease.name}:{disease.ToString()}");
        }

        public void RemoveDisease(Disease disease, bool removeImmune = false)
        {
            foreach (var symptomIndex in disease.symptoms) networkObject.gameObject.StatusEffectController().RemoveEffect(e => e.source == disease.ToString());

            Diseases.Remove(disease);
            DiseaseIds.Remove(disease.ToString());

            if (!removeImmune) { return; }
            ImmuneDiseases.Remove(disease);
            ImmuneDiseaseIds.Remove(disease.ToString());
        }

        public void ClearDiseases(bool clearImmune = false)
        {
            foreach (var disease in Diseases.ToList()) RemoveDisease(disease, clearImmune);
        }

        void CreateDiseaseScanNode()
        {
            //logger.LogDebug("Creating disease scannode");

            Collider? collider = null;

            if (enemy != null)
            {
                collider = enemy.gameObject.GetComponentInChildren<EnemyAICollisionDetect>()?.GetComponent<Collider>(); // TODO: Test this
            }
            else
            {
                collider = networkObject.gameObject.GetComponent<Collider>();
            }
            //if (collider == null) { logger.LogError($"Couldn't create disease scan node, no collider found on {networkObject.name}"); return; }
            //if (collider != null) { logger.LogDebug($"Got Collider {collider.name}"); }

            GameObject scanNodeObj = Instantiate(LethalDiseasesContentHandler.Instance.DiseaseAssets!.DiseaseScanNodePrefab, collider != null ? collider.transform : networkObject.transform);
            diseaseScanNode = scanNodeObj.GetComponent<DiseaseScanNode>();
            diseaseScanNode.diseaseHost = this;
            diseaseScanNode.parentCollider = collider;
        }

        public string GetInfectedName()
        {
            if (player != null)
                return player.playerUsername;
            else if (enemy != null)
                return enemy.enemyType.enemyName;
            else if (networkObject != null)
                return networkObject.name;

            return "";
        }

        public void Sanitize(float sanitizeTime)
        {
            sanitizedTimeLeft = Mathf.Max(sanitizedTimeLeft, sanitizeTime);
        }

        public void SanitizeAndSync(float sanitizeTime)
        {
            NetworkHandler.Instance.SanitizeRpc(networkObject, sanitizeTime);
        }

        public void ReplaceAirFilter()
        {
            airFilterQuality = 1f;

            if (player != null && localPlayer == player)
                Utils.DisplayStatusEffect($"Air Filter replaced. Filter quality: {airFilterQuality * 100}%");
        }

        public void ReplaceAirFilterAndSync()
        {
            NetworkHandler.Instance.ReplaceAirFilterRpc(networkObject);
        }

        public void UseAirFilter(float multiplier = 1f)
        {
            airFilterQuality -= AirFilter.Usage * multiplier;

            if (player != null && localPlayer == player)
                Utils.DisplayStatusEffect($"Airborne diseases detected! Filter quality: {airFilterQuality * 100}%");
        }

        public void UseAirFilterAndSync(float multiplier = 1f)
        {
            NetworkHandler.Instance.UseAirFilterRpc(networkObject, multiplier);
        }

        public void Infect(string diseaseId) // Should only have 1 reference
        {
            Disease? disease = Disease.GetDiseaseFromString(diseaseId);
            if (disease == null) { return; }
            logger?.LogDebug($"Infecting {gameObject.name} with disease ID: {diseaseId}");
            AddDisease(disease);
        }

        public void InfectAndSync(string diseaseId)
        {
            NetworkHandler.Instance.InfectRpc(networkObject, diseaseId);
        }
    }
}
