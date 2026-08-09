using GameNetcodeStuff;
using SnowyLib;
using System.Collections.Generic;
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
        internal GrabbableObject? grabbableObject { get; private set; }

        public DiseaseScanNode? diseaseScanNode;

        public bool hasActor => enemy != null || player != null;

        public bool hasDisease => Diseases.Count > 0;

        void Start()
        {
            Instances.Add(this);
            player = NetworkObject.gameObject.GetComponent<PlayerControllerB>();
            enemy = NetworkObject.gameObject.GetComponent<EnemyAI>();
            steamValve = NetworkObject.gameObject.GetComponent<SteamValveHazard>();
            grabbableObject = NetworkObject.gameObject.GetComponent<GrabbableObject>();

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

            Collider? collider = null;

            if (enemy != null)
            {
                collider = enemy.gameObject.GetComponentInChildren<EnemyAICollisionDetect>()?.GetComponent<Collider>(); // TODO: Test this
            }
            else
            {
                collider = networkObject.gameObject.GetComponent<Collider>();
            }
            if (collider == null) { logger.LogError($"Couldn't create disease scan node, no collider found on {networkObject.name}"); return; }

            logger.LogDebug($"Got Collider {collider.name}");
            GameObject scanNodeObj = Instantiate(LethalDiseasesContentHandler.Instance.DiseaseAssets!.DiseaseScanNodePrefab, collider.transform);
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
    }
}
