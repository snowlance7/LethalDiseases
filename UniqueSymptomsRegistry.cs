using GameNetcodeStuff;
//using LethalDiseases.UniqueSymptoms;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalDiseases
{
    internal static class UniqueSymptomsRegistry
    {
        internal static readonly List<Action<float>> updates = new();

        internal static void Register()
        {
            //updates.Add(InsanitySymptom.Update);
        }

        internal static void Update(float deltaTime)
        {
            foreach (var update in updates)
                update(deltaTime);
        }
    }
}
