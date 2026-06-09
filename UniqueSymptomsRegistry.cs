using System;
using System.Collections.Generic;
using static LethalDiseases.Symptoms.Symptoms;

namespace LethalDiseases
{
    internal static class UniqueSymptomsRegistry
    {
        internal static readonly List<Action<float>> updates = new();

        internal static void Register()
        {
            updates.Add(InsanityUpdate);
            updates.Add(ItFollowsUpdate);
        }

        internal static void Update(float deltaTime)
        {
            foreach (var update in updates)
                update(deltaTime);
        }
    }
}
