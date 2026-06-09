using BepInEx.Logging;
using GameNetcodeStuff;
using SnowyLib;
using System.Collections.Generic;
using System.Linq;
using static LethalDiseases.Plugin;
using Unity.Netcode;
using System;

namespace LethalDiseases
{
    internal static class SymptomAffectedObjects
    {
        public static AutoDictionary<string, bool> localPlayerAffected { get; set; } = new AutoDictionary<string, bool>((x) => false);
        public static AutoDictionary<string, HashSet<NetworkObject>> symptomAffectedObjects { get; private set; } = new AutoDictionary<string, HashSet<NetworkObject>>((x) => new HashSet<NetworkObject>());
        public static AutoDictionary<string, HashSet<PlayerControllerB>> symptomAffectedPlayers { get; private set; } = new AutoDictionary<string, HashSet<PlayerControllerB>>((x) => new HashSet<PlayerControllerB>());
        public static AutoDictionary<string, HashSet<EnemyAI>> symptomAffectedEnemies { get; private set; } = new AutoDictionary<string, HashSet<EnemyAI>>((x) => new HashSet<EnemyAI>());
        // steamValve = netObj.gameObject.GetComponent<SteamValveHazard>();
        public static void AddSymptomAffectedObject(string symptomName, NetworkObjectReference netRef)
        {
            if (!netRef.TryGet(out NetworkObject networkObject) || networkObject == null) { return; }

            if (networkObject.gameObject.GetComponent<SteamValveHazard>() != null) { return; } // TODO: Test this

            logger.LogDebug($"Adding symptomAffectedObject {symptomName}:{networkObject.name}");
            symptomAffectedObjects[symptomName].Add(networkObject);

            if (networkObject.gameObject.TryGetComponent(out PlayerControllerB player))
            {
                symptomAffectedPlayers[symptomName].Add(player);
            }
            else if (networkObject.gameObject.TryGetComponent(out EnemyAI enemy))
            {
                symptomAffectedEnemies[symptomName].Add(enemy);
            }
        }

        public static void RemoveSymptomAffectedObject(string symptomName, NetworkObjectReference netRef)
        {
            if (!netRef.TryGet(out NetworkObject networkObject) || networkObject == null) { return; }

            symptomAffectedObjects[symptomName].Remove(networkObject);

            if (networkObject.gameObject.TryGetComponent(out PlayerControllerB player))
            {
                symptomAffectedPlayers[symptomName].Remove(player);
            }
            else if (networkObject.gameObject.TryGetComponent(out EnemyAI enemy))
            {
                symptomAffectedEnemies[symptomName].Remove(enemy);
            }
        }
    }
}
