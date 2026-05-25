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

        public enum TransmissionType
        {
            Airborne,
            Contact,
            Blood, // TODO
            Foodborne // TODO
        }

        public int[] symptoms = [];

        bool isActive;
        public float elapsedTime;
        int nextSpreadUpdateInterval;

        public float nextPeriodicTriggerTime;

        public NetworkObject networkObject { get; set; } = null!;
        public PlayerControllerB? player { get; private set; }
        public EnemyAI? enemy { get; private set; }
        public SteamValveHazard? steamValve { get; private set; }

        Collider[] nearby = []; // TODO: Test this

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

        public static Disease CreateRandomDisease()
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

        public static Disease CreateRandomDiseaseWithSymptom(int symptomIndex)
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

        public string GetID()
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

        public void TrySpread(NetworkObject _networkObject)
        {
            switch (transmissionType)
            {
                case TransmissionType.Airborne:
                    if (player != null || enemy != null)
                    {
                        if (UnityEngine.Random.Range(0f, 1f) < transmissibility)
                        {
                            Infect(_networkObject);
                        }
                    }
                    break;
                case TransmissionType.Contact:
                    if (UnityEngine.Random.Range(0f, 1f) < transmissibility)
                    {
                        Infect(_networkObject);
                    }
                    break;
                case TransmissionType.Blood: // TODO
                    break;
                case TransmissionType.Foodborne: // TODO
                    break;
                default:
                    break;
            }
        }

        public static bool HasActor(NetworkObject _networkObject)
        {
            return _networkObject.GetComponent<EnemyAI>() != null || _networkObject.GetComponent<PlayerControllerB>() != null;
        }

        public void Infect(NetworkObject _networkObject)
        {
            //LethalDiseasesNetworkHandler.Instance.InfectServerRpc(_networkObject, GetID());
        }

        /*public void Update(float deltaTime) // TODO: Simplify this
        {
            elapsedTime += deltaTime;

            if (networkObject != null)
            {
                if (player != null || enemy != null)
                {
                    if (diseaseIsActive)
                    {
                        if (elapsedTime > strengthTime)
                        {
                            networkObject.RemoveDisease(this);
                            return;
                        }

                        if ((player != null && localPlayer == player) || (player == null && IsServerOrHost))
                        {
                            foreach (var symptomIndex in symptoms)
                            {
                                Symptom.symptoms[symptomIndex].effect.Invoke(this, elapsedTime / strengthTime);
                            }
                        }
                    }
                    else
                    {
                        if (elapsedTime > latencyTime)
                        {
                            elapsedTime = 0;
                            nextSpreadUpdateInterval = 0;
                            diseaseIsActive = true;
                        }
                    }

                    if (elapsedTime > nextSpreadUpdateInterval)
                    {
                        nextSpreadUpdateInterval++;

                        if (IsServerOrHost && UnityEngine.Random.Range(0f, 1f) < transmissibility)
                        {
                            switch (transmissionType)
                            {
                                case TransmissionType.Airborne:
                                    Physics.OverlapSphereNonAlloc(networkObject.transform.position, airborneSpreadRange, nearbyPlayers);
                                    foreach (var playerCollider in nearbyPlayers)
                                    {
                                        PlayerControllerB nearbyPlayer = playerCollider.gameObject.GetComponent<PlayerControllerB>();
                                        LethalDiseasesNetworkHandler.Instance.InfectServerRpc(nearbyPlayer.NetworkObject, GetID());
                                    }
                                    break;
                                case TransmissionType.Contact:
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    if (elapsedTime > stabilityTime)
                    {
                        networkObject.RemoveDisease(this);
                        return;
                    }

                    foreach (var symptomIndex in symptoms)
                    {
                        Symptom.symptoms[symptomIndex].effect.Invoke(this, elapsedTime / stabilityTime);
                    }

                    if (elapsedTime > nextSpreadUpdateInterval)
                    {
                        nextSpreadUpdateInterval++;

                        if (IsServerOrHost && UnityEngine.Random.Range(0f, 1f) < transmissibility)
                        {
                            switch (transmissionType)
                            {
                                case TransmissionType.Airborne:
                                    if (steamValve != null)
                                    {
                                        Physics.OverlapSphereNonAlloc(steamValve.valveAudio.transform.position, 10f, nearbyPlayers);
                                        foreach (var playerCollider in nearbyPlayers)
                                        {
                                            PlayerControllerB nearbyPlayer = playerCollider.gameObject.GetComponent<PlayerControllerB>();
                                            LethalDiseasesNetworkHandler.Instance.InfectServerRpc(nearbyPlayer.NetworkObject, GetID());
                                        }
                                    }
                                    break;
                                case TransmissionType.Contact:
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
        }*/

        public void Update(float deltaTime)
        {
            elapsedTime += deltaTime;

            if (networkObject == null)
                return;

            bool hasActor = (player != null || enemy != null);

            float lifeTime = hasActor ? strengthTime : stabilityTime;

            // Remove disease if expired
            if (elapsedTime > lifeTime)
            {
                networkObject.RemoveDisease(this);
                return;
            }

            // Activation
            if (hasActor && !isActive)
            {
                if (elapsedTime > latencyTime)
                {
                    elapsedTime = 0;
                    nextSpreadUpdateInterval = 0;
                    isActive = true;

                    if (player != null)
                    {
                        if (player != null && localPlayer == player)
                        {
                            for (int i = 0; i < symptoms.Length; i++)
                            {
                                var effect = Symptom.symptomList[symptoms[i]].effect(this);
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
                //return;
            }

            // Spread handling
            if (elapsedTime <= nextSpreadUpdateInterval)
                return;

            nextSpreadUpdateInterval++;

            if (!IsServerOrHost)
                return;

            switch (transmissionType)
            {
                case TransmissionType.Airborne:
                    {
                        Vector3 origin = hasActor
                            ? networkObject.transform.position
                            : (steamValve != null ? steamValve.valveAudio.transform.position : Vector3.zero); // TODO: Test this

                        if (origin == Vector3.zero) { return; }

                        float radius = hasActor ? airborneSpreadRange.Value : 10f;

                        Physics.OverlapSphereNonAlloc(origin, radius, nearby);

                        foreach (var col in nearby)
                        {
                            if (UnityEngine.Random.Range(0f, 1f) > transmissibility / 2) { continue; }
                            /*if (col.gameObject.TryGetComponent(out PlayerControllerB p))
                                LethalDiseasesNetworkHandler.Instance.InfectServerRpc(p.NetworkObject, id);
                            else if (col.gameObject.TryGetComponent(out EnemyAI e))
                                LethalDiseasesNetworkHandler.Instance.InfectServerRpc(e.NetworkObject, id);*/
                        }
                        break;
                    }

                case TransmissionType.Contact:
                    break;

                case TransmissionType.Blood:
                    break;

                case TransmissionType.Foodborne:
                    break;
            }
        }

        public void TrySpreadAirborne(Vector3 origin, float range)
        {
            if (player == null && enemy == null) { return; }

            Physics.OverlapSphereNonAlloc(origin, range, nearby);

            foreach (var col in nearby)
            {
                if (UnityEngine.Random.Range(0f, 1f) > transmissibility) { continue; }
                /*if (col.gameObject.TryGetComponent(out PlayerControllerB p))
                    LethalDiseasesNetworkHandler.Instance.InfectServerRpc(p.NetworkObject, id);
                else if (col.gameObject.TryGetComponent(out EnemyAI e))
                    LethalDiseasesNetworkHandler.Instance.InfectServerRpc(e.NetworkObject, id);*/
            }
        }

        public bool Equals(Disease other)
        {
            return this.id == other.id;
        }
    }
}
