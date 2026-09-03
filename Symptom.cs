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
                        return $"<color=green>{name}</color>";
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
            symptomList.Add(symptom);
            symptomList.Sort((a, b) => a.name.CompareTo(b.name));
        }

        public static int GetSymptomIndexByName(string name)
        {
            Symptom? symptom = GetSymptomByName(name);
            return symptom != null ? symptomList.IndexOf(symptom) : -1;
        }

        public static Symptom? GetSymptomByName(string name)
        {
            return symptomList.Where(x => x.name.ToLower() == name.ToLower()).FirstOrDefault();
        }

        internal static List<Symptom> symptomList = new List<Symptom>();
    }
}
