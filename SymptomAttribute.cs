using System;

namespace LethalDiseases
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SymptomAttribute : Attribute
    {
        public string Name;
        public string Description;
        public Symptom.SymptomType Type;
        public float Rarity;

        public SymptomAttribute(string name, string description, Symptom.SymptomType type, float rarity)
        {
            Name = name;
            Description = description;
            Type = type;
            Rarity = rarity;
        }
    }
}
