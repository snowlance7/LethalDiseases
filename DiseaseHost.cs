using GameNetcodeStuff;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace LethalDiseases
{
    public class DiseaseHost : NetworkBehaviour
    {
        public List<Disease> Diseases = [];
        public List<Disease> ImmuneDiseases = [];
        public List<string> DiseaseIds = [];
        public List<string> ImmuneDiseaseIds = [];

        public PlayerControllerB? player { get; private set; }
        public EnemyAI? enemy { get; private set; }
        internal SteamValveHazard? steamValve { get; private set; }

        public bool isActor => enemy != null || player != null;

        public bool hasDisease => Diseases.Count > 0;

        void Start()
        {
            player = NetworkObject.gameObject.GetComponent<PlayerControllerB>();
            enemy = NetworkObject.gameObject.GetComponent<EnemyAI>();
            steamValve = NetworkObject.gameObject.GetComponent<SteamValveHazard>();
        }

        void Update()
        {
            foreach (var disease in Diseases.ToList())
            {
                disease.Update(Time.deltaTime);
            }
        }
    }
}
