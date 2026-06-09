using LethalDiseases.UniqueSymptoms;
using System;
using System.Collections.Generic;

namespace LethalDiseases
{
    internal static class UniqueSymptomsRegistry
    {
        internal static readonly List<Action<float>> updates = new();

        internal static void Register()
        {
            updates.Add(InsanitySymptom.Update);
            updates.Add(ItFollows.Update);
        }

        internal static void Update(float deltaTime)
        {
            foreach (var update in updates)
                update(deltaTime);
        }
    }
}
