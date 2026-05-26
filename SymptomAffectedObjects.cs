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
        public static AutoDictionary<string, HashSet<PlayerControllerB>> symptomAffectedPlayers { get; private set; } = new AutoDictionary<string, HashSet<PlayerControllerB>>();
        public static AutoDictionary<string, HashSet<EnemyAI>> symptomAffectedEnemies { get; private set; } = new AutoDictionary<string, HashSet<EnemyAI>>();

        public static void AddSymptomAffectedObject(string symptomName, NetworkObjectReference netRef)
        {
            if (!netRef.TryGet(out NetworkObject networkObject) || networkObject == null) { return; }

            if (!symptomAffectedObjects.ContainsKey(symptomName))
                symptomAffectedObjects.Add(symptomName, new HashSet<NetworkObject>());

            symptomAffectedObjects[symptomName].Add(networkObject);

            if (networkObject.gameObject.TryGetComponent(out PlayerControllerB player))
            {
                if (!symptomAffectedPlayers.ContainsKey(symptomName))
                    symptomAffectedPlayers.Add(symptomName, new HashSet<PlayerControllerB>());

                symptomAffectedPlayers[symptomName].Add(player);
            }
            else if (networkObject.gameObject.TryGetComponent(out EnemyAI enemy))
            {
                if (!symptomAffectedEnemies.ContainsKey(symptomName))
                    symptomAffectedEnemies.Add(symptomName, new HashSet<EnemyAI>());

                symptomAffectedEnemies[symptomName].Add(enemy);
            }
        }

        public static void RemoveSymptomAffectedObject(string symptomName, NetworkObjectReference netRef)
        {
            if (!netRef.TryGet(out NetworkObject networkObject) || networkObject == null) { return; }

            if (symptomAffectedObjects.ContainsKey(symptomName))
                symptomAffectedObjects[symptomName].Remove(networkObject);

            if (networkObject.gameObject.TryGetComponent(out PlayerControllerB player))
            {
                if (symptomAffectedPlayers.ContainsKey(symptomName))
                    symptomAffectedPlayers[symptomName].Remove(player);
            }
            else if (networkObject.gameObject.TryGetComponent(out EnemyAI enemy))
            {
                if (symptomAffectedEnemies.ContainsKey(symptomName))
                    symptomAffectedEnemies[symptomName].Remove(enemy);
            }
        }
    }
}
