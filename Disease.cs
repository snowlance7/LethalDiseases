using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Configs;
using static LethalDiseases.Plugin;
using static SnowyLib.Utils;
using SnowyLib;

namespace LethalDiseases
{
    public class Disease : IEquatable<Disease>
    {
        public static Dictionary<string, string> namedDiseases = new Dictionary<string, string>();
        public string id { get { return GetID(); } }
        public string name { get { return namedDiseases.TryGetValue(id, out string _name) ? _name : "???"; } }

        // How long it lasts
        public float strength;
        public float strengthTime => Mathf.Lerp(strengthRange.Value.Min, strengthRange.Value.Max, strength);

        // Likelyhood it will transmit based on transmission type
        public float transmissibility;

        // How long it lasts outside the player
        public float stability;
        public float stabilityTime => Mathf.Lerp(stabilityRange.Value.Min, stabilityRange.Value.Max, stability);

        // How long it takes for the symptoms to show up
        public float latency;
        public float latencyTime => Mathf.Lerp(latencyRange.Value.Min, latencyRange.Value.Max, latency);

        public TransmissionType transmissionType;

        [System.Flags]
        public enum TransmissionType
        {
            Airborne = 1 << 0, // 1
            Contact = 1 << 1, // 2
            Blood = 1 << 2, // 4 // TODO
            Foodborne = 1 << 3 // 8 // TODO
        }

        public int[] symptoms = [];

        bool isActive;
        public float elapsedTime;

        float timeSinceSpreadUpdate;

        public float nextPeriodicTriggerTime;

        public NetworkObject networkObject { get; private set; } = null!;
        public PlayerControllerB? player { get; private set; }
        public EnemyAI? enemy { get; private set; }
        internal SteamValveHazard? steamValve { get; private set; }

        Collider[] airborneColliders = []; // TODO: Test this

        public bool hasActor => enemy != null || player != null;

        internal void Init(NetworkObject netObj)
        {
            networkObject = netObj;
            player = netObj.gameObject.GetComponent<PlayerControllerB>();
            enemy = netObj.gameObject.GetComponent<EnemyAI>();
            steamValve = netObj.gameObject.GetComponent<SteamValveHazard>();
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

        internal static Disease CreateRandomDisease()
        {
            float RandomPercent() => MathF.Round(UnityEngine.Random.Range(0f, 1f), 2);

            var disease = new Disease
            {
                strength = RandomPercent(),
                transmissibility = RandomPercent(),
                stability = RandomPercent(),
                latency = RandomPercent(),
                transmissionType = (TransmissionType)RoundManager.Instance.GetRandomWeightedIndex(
                    new int[] { airborneTypeWeight.Value, contactTypeWeight.Value, bloodTypeWeight.Value, foodborneTypeWeight.Value })
            };

            int min = Mathf.Clamp(Mathf.Min(minSymptoms.Value, maxSymptoms.Value), 0, Symptom.symptomList.Count);
            int max = Mathf.Clamp(Mathf.Max(minSymptoms.Value, maxSymptoms.Value), 0, Symptom.symptomList.Count);

            int symptomCount = UnityEngine.Random.Range(min, max + 1);

            // Shuffle indices and take first N (no duplicates, no loops)
            disease.symptoms = Enumerable.Range(0, Symptom.symptomList.Count)
                .OrderBy(_ => randomLocal.Next())
                .Take(symptomCount)
                .ToArray();

            return disease;
        }

        internal static Disease CreateRandomDiseaseWithSymptom(int symptomIndex)
        {
            float RandomPercent() => MathF.Round(UnityEngine.Random.Range(0f, 1f), 2);

            var disease = new Disease
            {
                strength = RandomPercent(),
                transmissibility = RandomPercent(),
                stability = RandomPercent(),
                latency = RandomPercent(),
                transmissionType = (TransmissionType)RoundManager.Instance.GetRandomWeightedIndex(
                    new int[] { airborneTypeWeight.Value, contactTypeWeight.Value, bloodTypeWeight.Value, foodborneTypeWeight.Value })
            };

            disease.symptoms = [symptomIndex];
            return disease;
        }

        string GetID()
        {
            // Ensure consistent float formatting
            string Format(float f) => f.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

            // Sort symptoms so order doesn't affect ID
            var sortedSymptoms = symptoms.OrderBy(x => x);

            string symptomPart = string.Join(",", sortedSymptoms);

            return $"{Format(strength)}|" +
                   $"{Format(transmissibility)}|" +
                   $"{Format(stability)}|" +
                   $"{Format(latency)}|" +
                   $"{(int)transmissionType}|" +
                   $"{symptomPart}";
        }

        public static Disease? GetDiseaseFromID(string id)
        {
            var parts = id.Split('|');

            if (parts.Length != 6)
                return null;

            float ParseFloat(string s) =>
                float.Parse(s, System.Globalization.CultureInfo.InvariantCulture);

            var disease = new Disease
            {
                strength = ParseFloat(parts[0]),
                transmissibility = ParseFloat(parts[1]),
                stability = ParseFloat(parts[2]),
                latency = ParseFloat(parts[3]),
                transmissionType = (TransmissionType)int.Parse(parts[4]),
                symptoms = string.IsNullOrEmpty(parts[5])
                    ? new int[0]
                    : parts[5].Split(',').Select(int.Parse).ToArray()
            };

            return disease;
        }

        internal void TrySpread(NetworkObject _networkObject, TransmissionType spreadTransmissionType)
        {
            if (!transmissionType.HasFlag(spreadTransmissionType)) { return; }

            switch (spreadTransmissionType)
            {
                case TransmissionType.Airborne:
                    if (hasActor)
                    {
                        if (UnityEngine.Random.Range(0f, 1f) < transmissibility)
                        {
                            Infect(_networkObject);
                        }
                    }
                    break;
                default:
                    if (UnityEngine.Random.Range(0f, 1f) < transmissibility)
                    {
                        Infect(_networkObject);
                    }
                    break;
            }
        }

        internal void Infect(NetworkObject _networkObject)
        {
            LethalDiseasesNetworkHandler.Instance.InfectServerRpc(_networkObject, id);
        }

        internal void Update(float deltaTime)
        {
            elapsedTime += deltaTime;
            timeSinceSpreadUpdate += deltaTime;

            if (networkObject == null)
                return;

            float lifeTime = hasActor ? strengthTime : stabilityTime;

            // Remove disease if expired
            if (elapsedTime > lifeTime)
            {
                networkObject.RemoveDisease(this);
                return;
            }

            // Activation handling
            if (hasActor && !isActive)
            {
                if (elapsedTime > latencyTime)
                {
                    elapsedTime = 0;
                    isActive = true;

                    if (player != null)
                    {
                        if (localPlayer == player)
                        {
                            for (int i = 0; i < symptoms.Length; i++)
                            {
                                var effect = Symptom.symptomList[symptoms[i]].effect(this); // TODO: Giving arguementOutOfRange Exception
                                networkObject.gameObject.StatusEffectController().ApplyEffect(effect);
                            }
                        }
                    }
                    else if (IsServerOrHost)
                    {
                        for (int i = 0; i < symptoms.Length; i++)
                        {
                            var effect = Symptom.symptomList[symptoms[i]].effect(this);
                            networkObject.gameObject.StatusEffectController().ApplyEffect(effect);
                        }
                    }
                }
            }

            // Spread handling
            if (timeSinceSpreadUpdate > 2f)
            {
                timeSinceSpreadUpdate = 0f;

                if (transmissionType.HasFlag(TransmissionType.Airborne))
                {
                    Vector3 origin = hasActor ? networkObject.transform.position : (steamValve != null ? steamValve.valveAudio.transform.position : Vector3.zero); // TODO: Test this

                    if (origin == Vector3.zero) { return; }

                    float radius = hasActor ? airborneSpreadRange.Value : 10f;

                    TrySpreadAirborne(origin, radius, 0.5f);
                }
            }
        }

        internal void TrySpreadAirborne(Vector3 origin, float radius = default, float multiplier = 1)
        {
            if (!hasActor) { return; }

            radius = radius == default ? airborneSpreadRange.Value : radius;

            Physics.OverlapSphereNonAlloc(origin, radius, airborneColliders);

            foreach (var col in airborneColliders)
            {
                if (UnityEngine.Random.Range(0f, 1f) > transmissibility * multiplier) { continue; }
                if (col.gameObject.TryGetComponent(out PlayerControllerB p))
                    LethalDiseasesNetworkHandler.Instance.InfectServerRpc(p.NetworkObject, id);
                else if (col.gameObject.TryGetComponent(out EnemyAI e))
                    LethalDiseasesNetworkHandler.Instance.InfectServerRpc(e.NetworkObject, id);
            }
        }

        public bool Equals(Disease other)
        {
            return this.id == other.id;
        }
    }
}
