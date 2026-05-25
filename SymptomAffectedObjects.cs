using GameNetcodeStuff;
using SnowyLib;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;

namespace LethalDiseases
{
    internal static class SymptomAffectedObjects
    {
        public static AutoDictionary<string, HashSet<NetworkObject>> symptomAffectedObjects { get; private set; } = new AutoDictionary<string, HashSet<NetworkObject>>();
        public static AutoDictionary<string, HashSet<PlayerControllerB>> symptomAffectedPlayers => symptomAffectedObjects.Where(x => x.)
        //public static AutoDictionary<string, HashSet<NetworkObject>> symptomAffectedObjects { get; private set; } = new AutoDictionary<string, HashSet<NetworkObject>>();

        public static void AddSymptomAffectedObject(string symptomName, NetworkObjectReference netRef)
        {
            if (!netRef.TryGet(out NetworkObject networkObject) || networkObject == null) { return; }

            if (!SymptomAffectedObjects.symptomAffectedObjects.ContainsKey(symptomName))
                SymptomAffectedObjects.symptomAffectedObjects.Add(symptomName, new HashSet<NetworkObject>());

            SymptomAffectedObjects.symptomAffectedObjects[symptomName].Add(networkObject);
        }

        public static void RemoveSymptomAffectedObject(string symptomName, NetworkObjectReference netRef)
        {
            if (!netRef.TryGet(out NetworkObject networkObject) || networkObject == null) { return; }

            if (!SymptomAffectedObjects.symptomAffectedObjects.ContainsKey(symptomName)) { return; }

            SymptomAffectedObjects.symptomAffectedObjects[symptomName].Remove(networkObject);
        }
    }
}
