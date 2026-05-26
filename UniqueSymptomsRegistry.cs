using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using LethalDiseases.UniqueSymptoms;
using UnityEngine;

namespace LethalDiseases
{
    internal static class UniqueSymptomsRegistry
    {
        internal static readonly List<Action<float>> updates = new();

        internal static void Register()
        {
            updates.Add(InsanitySymptom.Update);
            updates.Add(ItFollowsSymptom.Update);
        }

        internal static void Update(float deltaTime)
        {
            foreach (var update in updates)
                update(deltaTime);
        }
    }
}
