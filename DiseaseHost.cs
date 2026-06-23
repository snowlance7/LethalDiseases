using GameNetcodeStuff;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases
{
    public class DiseaseHost : NetworkBehaviour
    {
        public static HashSet<DiseaseHost> Instances { get; private set; } = new HashSet<DiseaseHost>();

        public List<Disease> Diseases = [];
        public List<Disease> ImmuneDiseases = [];
        public List<string> DiseaseIds = [];
        public List<string> ImmuneDiseaseIds = [];

        public NetworkObject networkObject => NetworkObject;
        public PlayerControllerB? player { get; private set; }
        public EnemyAI? enemy { get; private set; }
        internal SteamValveHazard? steamValve { get; private set; }

        public DiseaseScanNode? diseaseScanNode;

        public bool hasActor => enemy != null || player != null;

        public bool hasDisease => Diseases.Count > 0;

        void Start()
        {
            Instances.Add(this);
            player = NetworkObject.gameObject.GetComponent<PlayerControllerB>();
            enemy = NetworkObject.gameObject.GetComponent<EnemyAI>();
            steamValve = NetworkObject.gameObject.GetComponent<SteamValveHazard>();

            CreateDiseaseScanNode();
        }

        public override void OnDestroy()
        {
            Instances.Remove(this);
            base.OnDestroy();
        }

        void Update()
        {
            foreach (var disease in Diseases.ToList())
            {
                disease.Update(Time.deltaTime);
            }
        }

        public void AddDisease(Disease disease)
        {
            if (ImmuneDiseases.Contains(disease)) { return; }
            var id = disease.id;
            disease.host = this;
            DiseaseIds.Add(id);
            ImmuneDiseaseIds.Add(id);
            Diseases.Add(disease);
            ImmuneDiseases.Add(disease);

            logger?.LogDebug($"Infected {GetInfectedName()} with disease {disease.name}:{disease.id}");
        }

        void CreateDiseaseScanNode()
        {
            logger.LogDebug("Creating disease scannode");
            Collider? collider = GetLargestCollider();
            if (collider == null) { logger.LogDebug("Collider is null"); return; }

            logger.LogDebug("Got Collider");
            GameObject scanNodeObj = Instantiate(LethalDiseasesContentHandler.Instance.DiseaseAssets!.DiseaseScanNodePrefab, collider.transform);
            diseaseScanNode = scanNodeObj.GetComponent<DiseaseScanNode>();
            diseaseScanNode.diseaseHost = this;
            diseaseScanNode.parentCollider = collider;
        }

        Collider? GetLargestCollider()
        {
            Collider[] colliders = networkObject.gameObject.GetComponentsInChildren<Collider>(); // TODO:  get this working

            Collider? largest = null;
            float largestVolume = 0f;

            foreach (Collider col in colliders)
            {
                Bounds bounds = col.bounds;

                float volume =
                    bounds.size.x *
                    bounds.size.y *
                    bounds.size.z;

                if (volume > largestVolume)
                {
                    largestVolume = volume;
                    largest = col;
                }
            }

            return largest;
        }

        public string GetInfectedName()
        {
            if (player != null)
                return player.playerUsername;
            else if (enemy != null)
                return enemy.enemyType.enemyName;
            else if (networkObject != null)
                return networkObject.gameObject.name;

            return "";
        }
    }
}
