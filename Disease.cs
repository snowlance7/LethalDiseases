using GameNetcodeStuff;
using SnowyCraftingCore;
using SnowyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Configs;
using static LethalDiseases.Plugin;
using static SnowyLib.Utils;

namespace LethalDiseases
{
    public class Disease : IEquatable<Disease>
    {
        public static Dictionary<string, string> namedDiseases = new Dictionary<string, string>();
        public string name
        {
            get
            {
                if (namedDiseases.TryGetValue(ToString(), out string diseaseName))
                {
                    return diseaseName;
                }
                else
                {
                    return "???";
                }
            }
            set
            {
                namedDiseases[ToString()] = value;
            }
        }

        // How long it lasts
        public float strength { get; internal set; }
        public float strengthTime => Mathf.Lerp(strengthRange.Value.Min, strengthRange.Value.Max, strength);

        // How long it lasts outside the player
        public float stability { get; internal set; }
        public float stabilityTime => Mathf.Lerp(stabilityRange.Value.Min, stabilityRange.Value.Max, stability);

        // How long it takes for the symptoms to show up
        public float latency { get; internal set; }
        public float latencyTime => Mathf.Lerp(latencyRange.Value.Min, latencyRange.Value.Max, latency);

        // Likelyhood it will transmit based on transmission type
        public float transmissibility { get; internal set; }

        public TransmissionType transmissionType { get; private set; }

        [System.Flags]
        public enum TransmissionType
        {
            None = 0,
            Airborne = 1 << 0, // 1
            Contact = 1 << 1, // 2
            Blood = 1 << 2, // 4 // TODO
            Foodborne = 1 << 3 // 8 // TODO
        }

        public NetworkObject? networkObject => host?.networkObject;
        public PlayerControllerB? player => host?.player;
        public EnemyAI? enemy => host?.enemy;

        public bool hasActor => host != null && host.hasActor;

        public int[] symptoms = [];

        public bool isActive { get; private set; }

        public float elapsedTime;
        public float elapsedTimeMultiplier = 1f;
        public float timeLeft => isActive ? strengthTime - elapsedTime : (host != null && host.hasActor ? (strengthTime + latencyTime) - elapsedTime : stabilityTime - elapsedTime);

        float timeSinceSpreadUpdate;

        public float nextPeriodicTriggerTime;

        public bool frozen = true;

        public DiseaseHost? host;

        Collider[] airborneColliders = []; // TODO: Test this

        internal static Disease CreateRandomDisease()
        {
            float RandomPercent() => MathF.Round(UnityEngine.Random.Range(0f, 1f), 2);

            var disease = new Disease
            {
                strength = RandomPercent(),
                transmissibility = RandomPercent(),
                stability = RandomPercent(),
                latency = RandomPercent(),
                transmissionType = CreateRandomTransmissionType()
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

        internal static TransmissionType CreateRandomTransmissionType()
        {
            List<(TransmissionType Type, int Weight)> transmissionTypes = new()
            {
                (TransmissionType.Airborne, airborneTypeWeight.Value),
                (TransmissionType.Contact, contactTypeWeight.Value),
                (TransmissionType.Blood, bloodTypeWeight.Value),
                (TransmissionType.Foodborne, foodborneTypeWeight.Value)
            };

            int index = RoundManager.Instance.GetRandomWeightedIndexList(transmissionTypes.Select(x => x.Weight).ToList());

            TransmissionType transmissionType = transmissionTypes[index].Type;
            transmissionTypes.RemoveAt(index);

            string[] extraTypeChances = Configs.extraTransmissionTypeChances.Value
                .Replace(" ", "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (extraTypeChances.Length == 0)
                return transmissionType;

            for (int i = 0; i < extraTypeChances.Length; i++)
            {
                if (!float.TryParse(extraTypeChances[i], out float extraTypeChance))
                    continue;

                if (transmissionTypes.Count == 0)
                    return transmissionType;

                if (UnityEngine.Random.Range(0f, 1f) <= extraTypeChance)
                {
                    index = RoundManager.Instance.GetRandomWeightedIndexList(
                        transmissionTypes.Select(x => x.Weight).ToList());

                    transmissionType |= transmissionTypes[index].Type;
                    transmissionTypes.RemoveAt(index);
                }
            }

            return transmissionType;
        }

        internal static Disease CreateRandomDiseaseWithSymptom(int symptomIndex)
        {
            logger?.LogDebug("Creating random disease with symptom index of " + symptomIndex);
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

        public override string ToString()
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

        public static Disease? GetDiseaseFromString(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) { return null; }

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

        public List<Symptom> GetSymptoms()
        {
            return symptoms.Select(x => Symptom.symptomList[x]).ToList();
        }

        internal void TrySpread(NetworkObject _networkObject, TransmissionType spreadTransmissionType)
        {
            if (transmissionType.HasFlag(TransmissionType.Airborne) && spreadTransmissionType.HasFlag(TransmissionType.Airborne))
            {
                if (host != null && host.hasActor)
                {
                    if (UnityEngine.Random.Range(0f, 1f) < transmissibility)
                        _networkObject.Infect(this);
                }
            }
            if (transmissionType.HasFlag(TransmissionType.Contact) && spreadTransmissionType.HasFlag(TransmissionType.Contact))
            {
                if (UnityEngine.Random.Range(0f, 1f) < transmissibility)
                    _networkObject.Infect(this);
            }
            if (transmissionType.HasFlag(TransmissionType.Blood) && spreadTransmissionType.HasFlag(TransmissionType.Blood))
            {
                if (UnityEngine.Random.Range(0f, 1f) < transmissibility)
                    _networkObject.Infect(this);
            }
            if (transmissionType.HasFlag(TransmissionType.Foodborne) && spreadTransmissionType.HasFlag(TransmissionType.Foodborne))
            {
                if (UnityEngine.Random.Range(0f, 1f) < transmissibility)
                    _networkObject.Infect(this);
            }
        }

        internal void Update(float deltaTime)
        {
            if (frozen) { return; }

            elapsedTime += deltaTime * elapsedTimeMultiplier;
            timeSinceSpreadUpdate += deltaTime;

            if (host == null)
                return;

            float lifeTime = host.hasActor ? strengthTime : stabilityTime;

            // Remove disease if expired
            if ((isActive || !host.hasActor) && elapsedTime > lifeTime) // TODO: Test this
            {
                host.networkObject.RemoveDisease(this);
                return;
            }

            // Activation handling
            if (host.hasActor && !isActive)
            {
                if (elapsedTime > latencyTime)
                {
                    elapsedTime = 0;
                    isActive = true;

                    if (host.player != null)
                    {
                        if (localPlayer == host.player)
                        {
                            foreach (var symptomIndex in symptoms)
                            {
                                logger?.LogDebug($"Activating symptom at index {symptomIndex}");
                                var effect = Symptom.symptomList[symptomIndex].effect(this);
                                host.networkObject.gameObject.StatusEffectController().ApplyEffect(effect);
                            }
                        }
                    }
                    else if (IsServerOrHost)
                    {
                        for (int i = 0; i < symptoms.Length; i++)
                        {
                            var effect = Symptom.symptomList[symptoms[i]].effect(this);
                            host.networkObject.gameObject.StatusEffectController().ApplyEffect(effect);
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
                    Vector3 origin = host.hasActor ? host.networkObject.transform.position : (host.steamValve != null ? host.steamValve.valveAudio.transform.position : Vector3.zero); // TODO: Test this

                    if (origin == Vector3.zero) { return; }

                    float radius = host.hasActor ? airborneSpreadRange.Value : 10f;

                    TrySpreadAirborne(origin, radius, 0.5f);
                }
            }
        }

        internal void TrySpreadAirborne(Vector3 origin, float radius = default, float multiplier = 1)
        {
            if (host == null || !host.hasActor) { return; }

            radius = radius == default ? airborneSpreadRange.Value : radius;

            Physics.OverlapSphereNonAlloc(origin, radius, airborneColliders);

            foreach (var col in airborneColliders)
            {
                if (UnityEngine.Random.Range(0f, 1f) > transmissibility * multiplier) { continue; }
                if (col.gameObject.TryGetComponent(out PlayerControllerB p))
                    p.NetworkObject.Infect(this);
                else if (col.gameObject.TryGetComponent(out EnemyAI e))
                    e.NetworkObject.Infect(this);
            }
        }

        public bool Equals(Disease other)
        {
            return this.ToString() == other.ToString();
        }

        public static Disease MergeDiseases(params IEnumerable<Disease> diseases)
        {
            Disease mergedDisease = new Disease();
            if (diseases == null || diseases.Count() == 0) { return mergedDisease; }

            mergedDisease.strength = diseases.Max(d => d.strength);
            mergedDisease.transmissibility = diseases.Max(d => d.transmissibility);
            mergedDisease.stability = diseases.Max(d => d.stability);
            mergedDisease.latency = diseases.Max(d => d.latency);

            mergedDisease.symptoms = diseases.SelectMany(disease => disease.symptoms).Distinct().ToArray();
            mergedDisease.transmissionType = diseases.Select(disease => disease.transmissionType).Aggregate(TransmissionType.None, (combined, type) => combined | type);

            return mergedDisease;
        }

        public ChemistryLiquidAppearance GetChemistryLiquidAppearance()
        {
            Color color = GetColor();
            return new ChemistryLiquidAppearance(color, Mathf.Lerp(0, 10f, transmissibility));
        }

        public Color GetColor()
        {
            return new Color(r: strength, g: stability, b: latency);
        }

        public string GetTerminalDisplayText()
        {
            string text = $"Name: {name}\n" +
                $"\n" +
                $"Strength: {strength * 100}% | {strengthTime} seconds\n" +
                $"Stability: {stability * 100}% | {stabilityTime} seconds\n" +
                $"Latency: {latency * 100}% | {latencyTime} seconds\n" +
                $"Transmissibility: {transmissibility * 100}%\n" +
                $"\n" +
                $"Transmission Types: {transmissionType.ToString()}\n" +
                $"\n" +
                $"Symptoms:";

            foreach (var symptomIndex in symptoms)
            {
                var symptom = Symptom.symptomList[symptomIndex];
                text += $"\n- {symptom.DisplayName}: {symptom.description}";
            }

            text += "\n\n";

            return text;
        }

        public static Disease? GetDiseaseFromName(string diseaseName)
        {
            try
            {
                var namedDisease = namedDiseases.Where(x => x.Value.ToString().ToLower() == diseaseName.ToLower()).First();
                Disease? disease = GetDiseaseFromString(namedDisease.Key);
                return disease;
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}
