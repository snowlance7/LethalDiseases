using BepInEx.Configuration;
using SnowyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using static LethalDiseases.Symptom;

namespace LethalDiseases
{
    public class Symptom(string name, string description, SymptomType symptomType, float rarity, Func<Disease, StatusEffect> effect)
    {
        internal static List<Symptom> RegisteredSymptoms { get; set; } = new List<Symptom>();

        public string name = name;
        public string description = description;
        public SymptomType symptomType = symptomType;
        public float rarity = rarity;
        public Func<Disease, StatusEffect> effect = effect;

        public string DisplayName
        {
            get
            {
                switch (symptomType)
                {
                    case SymptomType.Good:
                        return $"<color=blue>{name}</color>";
                    case SymptomType.Neutral:
                        return $"<color=yellow>{name}</color>";
                    case SymptomType.Bad:
                        return $"<color=red>{name}</color>";
                    default:
                        return $"{name}";
                }
            }
        }

        public enum SymptomType
        {
            Good,
            Neutral,
            Bad
        }

        public static void RegisterSymptom(Symptom symptom)
        {
            ConfigEntry<bool> cfgEnabled = Plugin.PluginInstance.Config.Bind<bool>($"{symptom.symptomType.ToString()} Symptom Options", symptom.name, true, $"Whether the symptom '{symptom.name}' can appear in diseases\n\n{symptom.description}");
            if (!cfgEnabled.Value) { return; }
            RegisteredSymptoms.Add(symptom);
            RegisteredSymptoms.Sort((a, b) => a.name.CompareTo(b.name));
        }

        public static void UnregisterSymptom(Symptom symptom)
        {
            int index = RegisteredSymptoms.IndexOf(symptom);

            foreach (var host in DiseaseHost.Instances)
            {
                foreach (var disease in host.Diseases)
                {
                    disease.RemoveSymptom(index);
                }
            }

            RegisteredSymptoms.Remove(symptom);
        }

        public static void UnregisterSymptomAndSync(Symptom symptom)
        {
            NetworkHandler.Instance.UnregisterSymptomRpc(RegisteredSymptoms.IndexOf(symptom));
        }

        public static int GetSymptomIndexByName(string name)
        {
            Symptom? symptom = GetSymptomByName(name);
            return symptom != null ? RegisteredSymptoms.IndexOf(symptom) : -1;
        }

        public static Symptom? GetSymptomByName(string name)
        {
            return RegisteredSymptoms.Where(x => x.name.ToLower() == name.ToLower()).FirstOrDefault();
        }
    }
}
