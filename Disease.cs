using Dawn;
using GameNetcodeStuff;
using LethalDiseases.Items;
using SnowyCraftingCore;
using SnowyLib;
using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// How long it lasts
        /// </summary>
        public float strength { get; internal set; }
        public float strengthTime { get { return Mathf.Lerp(StrengthRange.Min, StrengthRange.Max, strength); } set { strength = Mathf.InverseLerp(StrengthRange.Min, StrengthRange.Max, value); } }

        /// <summary>
        /// How long it lasts outside the player
        /// </summary>
        public float stability { get; internal set; }
        public float stabilityTime { get { return Mathf.Lerp(StabilityRange.Min, StabilityRange.Max, stability); } set { stability = Mathf.InverseLerp(StabilityRange.Min, StabilityRange.Max, value); } }

        /// <summary>
        /// How long it takes for the symptoms to show up
        /// </summary>
        public float latency { get; internal set; }
        public float latencyTime { get { return Mathf.Lerp(LatencyRange.Min, LatencyRange.Max, latency); } set { latency = Mathf.InverseLerp(LatencyRange.Min, LatencyRange.Max, value); } }

        /// <summary>
        /// Likelyhood it will transmit based on transmission type
        /// </summary>
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

        public HashSet<int> symptoms = [];

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

            int min = Mathf.Clamp(Mathf.Min(MinSymptoms, MaxSymptoms), 0, Symptom.RegisteredSymptoms.Count);
            int max = Mathf.Clamp(Mathf.Max(MinSymptoms, MaxSymptoms), 0, Symptom.RegisteredSymptoms.Count);

            int symptomCount = UnityEngine.Random.Range(min, max + 1);

            // Shuffle indices and take first N (no duplicates, no loops)
            disease.symptoms = Enumerable.Range(0, Symptom.RegisteredSymptoms.Count)
                .OrderBy(_ => randomLocal.Next())
                .Take(symptomCount)
                .ToHashSet();

            return disease;
        }

        internal static TransmissionType CreateRandomTransmissionType()
        {
            List<(TransmissionType Type, int Weight)> transmissionTypes = new()
            {
                (TransmissionType.Airborne, AirborneTypeWeight),
                (TransmissionType.Contact, ContactTypeWeight),
                (TransmissionType.Blood, BloodTypeWeight),
                (TransmissionType.Foodborne, FoodborneTypeWeight)
            };

            int index = RoundManager.Instance.GetRandomWeightedIndexList(transmissionTypes.Select(x => x.Weight).ToList());

            TransmissionType transmissionType = transmissionTypes[index].Type;
            transmissionTypes.RemoveAt(index);

            string[] extraTypeChances = Configs.ExtraTransmissionTypeChances
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
                transmissionType = (TransmissionType)RoundManager.Instance.GetRandomWeightedIndex(new int[] { AirborneTypeWeight, ContactTypeWeight, BloodTypeWeight, FoodborneTypeWeight })
            };

            disease.symptoms = [symptomIndex];
            return disease;
        }

        internal static Disease CreateRandomDiseaseWithSymptoms(params Symptom[] symptoms)
        {
            float RandomPercent() => MathF.Round(UnityEngine.Random.Range(0f, 1f), 2);

            var disease = new Disease
            {
                strength = RandomPercent(),
                transmissibility = RandomPercent(),
                stability = RandomPercent(),
                latency = RandomPercent(),
                transmissionType = (TransmissionType)RoundManager.Instance.GetRandomWeightedIndex(new int[] { AirborneTypeWeight, ContactTypeWeight, BloodTypeWeight, FoodborneTypeWeight })
            };

            disease.AddSymptoms(symptoms);
            return disease;
        }

        internal static Disease CreateRandomDiseaseWithSymptoms(params string[] symptomNames)
        {
            float RandomPercent() => MathF.Round(UnityEngine.Random.Range(0f, 1f), 2);

            var disease = new Disease
            {
                strength = RandomPercent(),
                transmissibility = RandomPercent(),
                stability = RandomPercent(),
                latency = RandomPercent(),
                transmissionType = (TransmissionType)RoundManager.Instance.GetRandomWeightedIndex(new int[] { AirborneTypeWeight, ContactTypeWeight, BloodTypeWeight, FoodborneTypeWeight })
            };

            disease.AddSymptoms(symptomNames);
            return disease;
        }

        public override string ToString() // Format: 0.00|0.00|0.00|0.00|0|0,0,0,0,0,0
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
                symptoms = string.IsNullOrEmpty(parts[5]) ? new HashSet<int>() : parts[5].Split(',').Select(int.Parse).ToHashSet()
            };

            return disease;
        }

        public List<Symptom> GetSymptoms()
        {
            return symptoms.Select(x => Symptom.RegisteredSymptoms[x]).ToList();
        }

        internal void TrySpread(NetworkObject _networkObject, TransmissionType spreadTransmissionType, float multiplier = 1f)
        {
            if (transmissionType.HasFlag(TransmissionType.Airborne) && spreadTransmissionType.HasFlag(TransmissionType.Airborne))
            {
                if (host != null && host.hasActor)
                {
                    var otherHost = _networkObject.GetHost();

                    if (otherHost.IsAirFilterFunctional)
                    {
                        multiplier += AirFilter.Multiplier;
                        otherHost.UseAirFilterAndSync(multiplier);
                    }

                    if (UnityEngine.Random.Range(0f, 1f) * multiplier < transmissibility)
                    {
                        _networkObject.InfectAndSync(this);
                    }
                }
            }
            if (transmissionType.HasFlag(TransmissionType.Contact) && spreadTransmissionType.HasFlag(TransmissionType.Contact))
            {
                var otherHost = _networkObject.GetHost();

                if (otherHost.IsSanitized)
                {
                    multiplier += Sanitizer.Multiplier;
                }

                if (UnityEngine.Random.Range(0f, 1f) * multiplier < transmissibility)
                    _networkObject.InfectAndSync(this);
            }
            if (transmissionType.HasFlag(TransmissionType.Blood) && spreadTransmissionType.HasFlag(TransmissionType.Blood))
            {
                if (UnityEngine.Random.Range(0f, 1f) * multiplier < transmissibility)
                    _networkObject.InfectAndSync(this);
            }
            if (transmissionType.HasFlag(TransmissionType.Foodborne) && spreadTransmissionType.HasFlag(TransmissionType.Foodborne))
            {
                if (UnityEngine.Random.Range(0f, 1f) * multiplier < transmissibility)
                    _networkObject.InfectAndSync(this);
            }
        }

        internal void TrySpreadAirborne(Vector3 origin, float radius = default, float multiplier = 1)
        {
            if (host == null || !host.hasActor) { return; }

            radius = radius == default ? AirborneSpreadRange : radius;

            Physics.OverlapSphereNonAlloc(origin, radius, airborneColliders);

            foreach (var col in airborneColliders)
            {
                if (col.gameObject.TryGetComponent(out PlayerControllerB p))
                    TrySpread(p.NetworkObject, TransmissionType.Airborne, multiplier);
                else if (col.gameObject.TryGetComponent(out EnemyAI e))
                    TrySpread(e.NetworkObject, TransmissionType.Airborne, multiplier);
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
            if (!isActive && elapsedTime > latencyTime)
            {
                elapsedTime = 0;
                isActive = true;

                foreach (var symptomIndex in symptoms)
                {
                    ActivateSymptom(symptomIndex);
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

                    float radius = host.hasActor ? AirborneSpreadRange : 10f;

                    TrySpreadAirborne(origin, radius, 0.1f);
                }
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

            mergedDisease.symptoms = diseases.SelectMany(disease => disease.symptoms).Distinct().ToHashSet();
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
                var symptom = Symptom.RegisteredSymptoms[symptomIndex];
                text += $"\n- {symptom.DisplayName}: {symptom.description}";
            }

            text += "\n\n";

            return text;
        }

        public static Disease? GetDiseaseFromIngredient(ChemistryIngredient ingredient)
        {
            return GetDiseaseFromString(ingredient.specialInstructions);
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

        public static void LoadNamedDiseases() // TODO: Should only have 1 reference to avoid save scumming
        {
            var contract = DawnLib.GetCurrentContract();
            if (contract == null) { logger.LogError("Failed to get named diseases, current contract does not exist"); return; }

            string namedDiseasesString = contract.GetOrSetDefault(LethalDiseasesKeys.NamedDiseasesKey, "");
            if (string.IsNullOrWhiteSpace(namedDiseasesString)) { logger.LogDebug("NamedDiseasesString is empty"); return; }

            var diseases = namedDiseasesString.Split("/");
            foreach (var namedDisease in diseases)
            {
                var nameId = namedDisease.Split(":");
                string name = nameId[0];
                string id = nameId[1];
                namedDiseases.Add(id, name);
            }
            logger.LogDebug($"LoadNamedDiseases completed, loaded {namedDiseases.Count} diseases");
        }

        public static void SaveNamedDiseases() // TODO: Should only have 1 reference to avoid save scumming
        {
            if (namedDiseases.Count == 0) { logger.LogDebug("No named diseases to save"); return; }

            var contract = DawnLib.GetCurrentContract();
            if (contract == null) { logger.LogError("Failed to get named diseases, current contract does not exist"); return; }

            string saveString = string.Join("/", namedDiseases.Select(x => x.Value + ":" + x.Key));
            contract.Set(LethalDiseasesKeys.NamedDiseasesKey, saveString);
            logger.LogDebug($"SaveNamedDiseases completed: {saveString}");
        }

        public void ActivateSymptom(int symptomIndex)
        {
            if (host == null || !host.hasActor) { return; }

            if (host.player != null)
            {
                if (localPlayer == host.player)
                {
                    logger?.LogDebug($"Activating symptom at index {symptomIndex}");
                    var effect = Symptom.RegisteredSymptoms[symptomIndex].effect(this);
                    host.networkObject.gameObject.StatusEffectController().ApplyEffect(effect);
                }
            }
            else if (IsServerOrHost)
            {
                var effect = Symptom.RegisteredSymptoms[symptomIndex].effect(this);
                host.networkObject.gameObject.StatusEffectController().ApplyEffect(effect);
            }
        }

        public void DeactivateSymptom(int symptomIndex)
        {
            if (host == null || !host.hasActor) { return; }

            if (host.player != null)
            {
                if (localPlayer == host.player)
                {
                    logger?.LogDebug($"Activating symptom at index {symptomIndex}");
                    var effect = Symptom.RegisteredSymptoms[symptomIndex].effect(this);
                    host.networkObject.gameObject.StatusEffectController().RemoveEffect(effect);
                }
            }
            else if (IsServerOrHost)
            {
                var effect = Symptom.RegisteredSymptoms[symptomIndex].effect(this);
                host.networkObject.gameObject.StatusEffectController().RemoveEffect(effect);
            }
        }

        public void AddSymptomAndSync(Symptom symptom)
        {
            AddSymptomAndSync(Symptom.RegisteredSymptoms.IndexOf(symptom));
        }

        public void AddSymptomAndSync(int symptomIndex)
        {
            NetworkHandler.Instance.AddSymptomToDiseaseRpc(networkObject, ToString(), symptomIndex);
        }

        public Disease AddSymptom(Symptom symptom)
        {
            return AddSymptom(Symptom.RegisteredSymptoms.IndexOf(symptom));
        }

        public Disease AddSymptom(string symptomName)
        {
            var symptom = Symptom.GetSymptomByName(symptomName);
            if (symptom == null) { logger.LogError("Couldnt find symptom with name" + symptomName); return this; }
            return AddSymptom(symptom);
        }

        public Disease AddSymptoms(params string[] symptomNames)
        {
            foreach (var symptomName in symptomNames)
            {
                AddSymptom(symptomName);
            }

            return this;
        }

        public Disease AddSymptoms(params Symptom[] symptoms)
        {
            foreach (var symptom in symptoms)
            {
                AddSymptom(symptom);
            }

            return this;
        }

        public Disease AddSymptom(int symptomIndex)
        {
            symptoms.Add(symptomIndex);

            if (elapsedTime > latencyTime)
            {
                ActivateSymptom(symptomIndex);
            }

            return this;
        }

        public void RemoveSymptomAndSync(Symptom symptom)
        {
            RemoveSymptomAndSync(Symptom.RegisteredSymptoms.IndexOf(symptom));
        }

        public void RemoveSymptomAndSync(int symptomIndex)
        {
            NetworkHandler.Instance.RemoveSymptomFromDiseaseRpc(networkObject, ToString(), symptomIndex);
        }

        public Disease RemoveSymptom(int symptomIndex)
        {
            if (!symptoms.Contains(symptomIndex)) { return this; }

            symptoms.Remove(symptomIndex);

            if (elapsedTime > latencyTime)
            {
                DeactivateSymptom(symptomIndex);
            }

            return this;
        }

        public Disease RemoveRandomSymptom()
        {
            return RemoveSymptom(symptoms.GetRandom(randomGlobal));
        }

        public Disease RemoveRandomSymptom(Symptom.SymptomType symptomType)
        {
            var filteredSymptoms = symptoms.Where(x => Symptom.RegisteredSymptoms[x].symptomType == symptomType);
            if (filteredSymptoms == null || filteredSymptoms.Count() == 0) { return this; }
            return RemoveSymptom(filteredSymptoms.GetRandom(randomGlobal));
        }

        public Disease AddRandomSymptom()
        {
            var availableSymptoms = Symptom.RegisteredSymptoms.Where(x => !symptoms.Contains(Symptom.RegisteredSymptoms.IndexOf(x)));
            if (availableSymptoms == null || availableSymptoms.Count() == 0) { return this; }
            var symptomToRemove = availableSymptoms.GetRandom(randomGlobal);
            if (symptomToRemove == null) { return this; }
            return AddSymptom(symptomToRemove);
        }

        public Disease AddRandomSymptom(Symptom.SymptomType symptomType)
        {
            var availableSymptoms = Symptom.RegisteredSymptoms.Where(x => x.symptomType == symptomType && !symptoms.Contains(Symptom.RegisteredSymptoms.IndexOf(x)));
            if (availableSymptoms == null || availableSymptoms.Count() == 0) { return this; }
            var symptomToRemove = availableSymptoms.GetRandom(randomGlobal);
            if (symptomToRemove == null) { return this; }
            return AddSymptom(symptomToRemove);
        }

        public Disease ReplaceRandomSymptom()
        {
            if (symptoms.Count == 0)
                return this;

            var availableSymptomsToAdd = Enumerable.Range(0, Symptom.RegisteredSymptoms.Count).Where(index => !symptoms.Contains(index));

            if (!availableSymptomsToAdd.Any())
                return this;

            var symptomToRemove = symptoms.GetRandom(randomGlobal);
            var symptomToAdd = availableSymptomsToAdd.GetRandom(randomGlobal);

            RemoveSymptom(symptomToRemove);
            AddSymptom(symptomToAdd);

            return this;
        }

        public Disease ReplaceRandomSymptom(Symptom.SymptomType removing, Symptom.SymptomType adding)
        {
            if (symptoms.Count == 0) { return this; }

            var availableSymptomsToAdd = Symptom.RegisteredSymptoms
                .Select((symptom, index) => (symptom, index))
                .Where(x => x.symptom.symptomType == adding && !symptoms.Contains(x.index))
                .Select(x => x.index)
                .ToList();

            if (availableSymptomsToAdd.Count == 0) { return this; }

            var availableSymptomsToRemove = symptoms
                .Where(x => Symptom.RegisteredSymptoms[x].symptomType == removing)
                .ToList();

            if (availableSymptomsToRemove.Count == 0) { return this; }

            var symptomToRemove = availableSymptomsToRemove.GetRandom(randomGlobal);
            var symptomToAdd = availableSymptomsToAdd.GetRandom(randomGlobal);

            RemoveSymptom(symptomToRemove);
            AddSymptom(symptomToAdd);

            return this;
        }

        public Disease AddTransmissionType(TransmissionType type)
        {
            if (type == TransmissionType.None) return this;

            transmissionType |= type & ~transmissionType;
            return this;
        }

        public Disease RemoveTransmissionType(TransmissionType type)
        {
            transmissionType &= ~type;
            return this;
        }

        public Disease ReplaceTransmissionType(TransmissionType type)
        {
            var existingTypes = Enum.GetValues(typeof(TransmissionType)).Cast<TransmissionType>().Where(x => x != TransmissionType.None && (transmissionType & x) == x).ToList();

            var availableTypes = Enum.GetValues(typeof(TransmissionType)).Cast<TransmissionType>().Where(x => x != TransmissionType.None && (transmissionType & x) != x).ToList();

            if (existingTypes.Count == 0 || availableTypes.Count == 0) return this;

            var typeToRemove = existingTypes.GetRandom(Utils.randomGlobal);

            transmissionType &= ~typeToRemove;
            return AddTransmissionType(type);
        }

        public Disease RemoveRandomTransmissionType()
        {
            var existingTypes = Enum.GetValues(typeof(TransmissionType)).Cast<TransmissionType>().Where(x => x != TransmissionType.None && (transmissionType & x) == x).ToList();

            if (existingTypes.Count == 0) return this;

            return RemoveTransmissionType(existingTypes.GetRandom(Utils.randomGlobal));
        }

        public Disease AddRandomTransmissionType()
        {
            var availableTypes = Enum.GetValues(typeof(TransmissionType)).Cast<TransmissionType>().Where(x => x != TransmissionType.None && (transmissionType & x) != x).ToList();

            if (availableTypes.Count == 0) return this;

            return AddTransmissionType(availableTypes.GetRandom(Utils.randomGlobal));
        }

        public Disease ReplaceRandomTransmissionType()
        {
            var existingTypes = Enum.GetValues(typeof(TransmissionType)).Cast<TransmissionType>().Where(x => x != TransmissionType.None && (transmissionType & x) == x).ToList();

            var availableTypes = Enum.GetValues(typeof(TransmissionType)).Cast<TransmissionType>().Where(x => x != TransmissionType.None && (transmissionType & x) != x).ToList();

            if (existingTypes.Count == 0 || availableTypes.Count == 0) return this;

            var typeToRemove = existingTypes.GetRandom(Utils.randomGlobal);
            var typeToAdd = availableTypes.GetRandom(Utils.randomGlobal);

            transmissionType &= ~typeToRemove;
            return AddTransmissionType(typeToAdd);
        }
    }
}
