using Dawn;
using GameNetcodeStuff;
using HarmonyLib;
using SnowyLib;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static LethalDiseases.Plugin;

namespace LethalDiseases
{
    internal class LethalDiseasesNetworkHandler : NetworkBehaviour
    {
        public static LethalDiseasesNetworkHandler Instance { get; private set; } = null!;

        public AudioClipGroup[] soundEffects = null!;
        public DecalProjector pukeProjectorPrefab = null!;

        public enum SoundEffect
        {
            Sneeze,
            Cough,
            CoughHeavy,
            Paranoia,
            DuckSounds // TODO: Set up
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
                Instance?.gameObject.GetComponent<NetworkObject>().Despawn(destroy: true);
            Instance = this;
            logger?.LogDebug("NetworkHandler spawned");
            base.OnNetworkSpawn();
        }

        public void Start()
        {
            Symptoms.Symptoms.Load();
            UniqueSymptomsRegistry.Register();
            Configs.Init(Plugin.Instance);
        }

        public void Update()
        {
            UniqueSymptomsRegistry.Update(Time.deltaTime);
        }

        [ServerRpc(RequireOwnership = false)]
        public void MufflePlayerServerRpc(ulong clientId, bool value)
        {
            if (!IsServer) { return; }
            MufflePlayerClientRpc(clientId, value);
        }

        [ClientRpc]
        private void MufflePlayerClientRpc(ulong clientId, bool value)
        {
            PlayerControllerB? player = PlayerFromId(clientId);
            if (player == null) { return; }
            player.MufflePlayer(value);
        }

        [ServerRpc(RequireOwnership = false)]
        public void InfectServerRpc(NetworkObjectReference netRef, string diseaseId) // TODO: Should only have 1 reference
        {
            if (!IsServer) { return; }
            InfectClientRpc(netRef, diseaseId);
        }

        [ClientRpc]
        private void InfectClientRpc(NetworkObjectReference netRef, string diseaseId)
        {
            Disease? disease = Disease.GetDiseaseFromID(diseaseId);
            if (disease == null || !netRef.TryGet(out NetworkObject netObj) || netObj == null) { return; }
            logger?.LogDebug($"Infecting {netObj.gameObject.name} with disease ID: {diseaseId}");
            netObj.AddDisease(disease);
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlaySoundEffectServerRpc(ulong clientId, SoundEffect soundEffect, int bodyPartIndex = 0, float volume = 1f, float min3DDistance = 1f, float max3DDistance = 10f, float cutoffFrequency = 22000, int audibleNoiseID = 0)
        {
            if (!IsServer) { return; }
            PlaySoundEffectClientRpc(clientId, soundEffect, bodyPartIndex, volume, min3DDistance, max3DDistance, cutoffFrequency, audibleNoiseID);
        }

        [ClientRpc]
        private void PlaySoundEffectClientRpc(ulong clientId, SoundEffect soundEffect, int bodyPartIndex = 0, float volume = 1f, float min3DDistance = 1f, float max3DDistance = 10f, float cutoffFrequency = 22000, int audibleNoiseID = 0)
        {
            PlayerControllerB? player = PlayerFromId(clientId);
            if (player == null) { return; }
            Transform position = player.bodyParts[bodyPartIndex];

            Utils.PlaySoundAtPosition(position, soundEffects[(int)soundEffect].clips, volume, true, true, min3DDistance, max3DDistance, cutoffFrequency, audibleNoiseID);
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlaySoundEffectServerRpc(Vector3 position, SoundEffect soundEffect, int bodyPartIndex = 0, float volume = 1f, float min3DDistance = 1f, float max3DDistance = 10f, float cutoffFrequency = 22000, int audibleNoiseID = 0)
        {
            if (!IsServer) { return; }
            PlaySoundEffectClientRpc(position, soundEffect, bodyPartIndex, volume, min3DDistance, max3DDistance, cutoffFrequency, audibleNoiseID);
        }

        [ClientRpc]
        private void PlaySoundEffectClientRpc(Vector3 position, SoundEffect soundEffect, int bodyPartIndex = 0, float volume = 1f, float min3DDistance = 1f, float max3DDistance = 10f, float cutoffFrequency = 22000, int audibleNoiseID = 0)
        {
            Utils.PlaySoundAtPosition(position, soundEffects[(int)soundEffect].clips, volume, true, true, min3DDistance, max3DDistance, cutoffFrequency, audibleNoiseID);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnEnemyServerRpc(Dawn.NamespacedKey<Dawn.DawnEnemyInfo> key, Vector3 position, Quaternion rotation = default)
        {
            if (!IsServer) { return; }
            Utils.SpawnEnemy(key, position, rotation);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SlimePlayersServerRpc(ulong[] clientIds, string diseaseId)
        {
            if (!IsServer) { return; }
            SlimePlayersClientRpc(clientIds, diseaseId);
        }

        [ClientRpc]
        private void SlimePlayersClientRpc(ulong[] clientIds, string diseaseId)
        {
            foreach (var clientId in clientIds)
            {
                PlayerControllerB? player = PlayerFromId(clientId);
                if (player == null) { continue; }

                player.NetworkObject.AddDisease(diseaseId);

                if (player == localPlayer)
                {
                    localPlayer.slimeOnFace = 10f;
                    localPlayer.slimeOnFaceDecals[0].gameObject.SetActive(true);
                    localPlayer.slimeOnFaceDecals[1].gameObject.SetActive(true);
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlacePukeDecalServerRpc(Vector3 position, Vector3 direction, float despawnTime = 0)
        {
            if (!IsServer) { return; }
            PlacePukeDecalClientRpc(position, direction, despawnTime);
        }

        [ClientRpc]
        private void PlacePukeDecalClientRpc(Vector3 position, Vector3 direction, float despawnTime)
        {
            DecalProjector projector = Instantiate(pukeProjectorPrefab, position, Quaternion.LookRotation(direction)).GetComponent<DecalProjector>();
            if (despawnTime > 0)
                Destroy(projector.gameObject, despawnTime);
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlayerDiscardHeldObjectServerRpc(ulong clientId)
        {
            if (!IsServer) { return; }
            PlayerDiscardHeldObjectClientRpc(clientId);
        }

        [ClientRpc]
        private void PlayerDiscardHeldObjectClientRpc(ulong clientId)
        {
            PlayerControllerB? player = PlayerFromId(clientId);
            player?.DiscardHeldObject();
        }

        [ServerRpc(RequireOwnership = false)]
        public void AddSymptomAffectedObjectServerRpc(string symptomName, NetworkObjectReference netRef)
        {
            if (!IsServer) { return; }
            AddSymptomAffectedObjectClientRpc(symptomName, netRef);
        }

        [ClientRpc]
        private void AddSymptomAffectedObjectClientRpc(string symptomName, NetworkObjectReference netRef)
        {
            SymptomAffectedObjects.AddSymptomAffectedObject(symptomName, netRef);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RemoveSymptomAffectedObjectServerRpc(string symptomName, NetworkObjectReference netRef)
        {
            if (!IsServer) { return; }
            RemoveSymptomAffectedObjectClientRpc(symptomName, netRef);
        }

        [ClientRpc]
        private void RemoveSymptomAffectedObjectClientRpc(string symptomName, NetworkObjectReference netRef)
        {
            SymptomAffectedObjects.RemoveSymptomAffectedObject(symptomName, netRef);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnGhostGirlServerRpc(ulong targetPlayerId)
        {
            if (!IsServer) { return; }
            DressGirlAI child = (DressGirlAI)Utils.SpawnEnemy(EnemyKeys.Girl, Vector3.zero)!;
            SpawnGhostGirlClientRpc(targetPlayerId, child.NetworkObject);
        }

        [ClientRpc]
        private void SpawnGhostGirlClientRpc(ulong targetPlayerId, NetworkObjectReference netRef)
        {
            if (!netRef.TryGet(out NetworkObject netObj)) { return; }
            PlayerControllerB? player = PlayerFromId(targetPlayerId);
            if (player == null) { return; }
            DressGirlAI child = netObj.GetComponent<DressGirlAI>();
            child.hauntingPlayer = player;
            child.ChangeOwnershipOfEnemy(child.hauntingPlayer.actualClientId);
            child.hauntingLocalPlayer = localPlayer == child.hauntingPlayer;
            if (child.switchHauntedPlayerCoroutine != null)
            {
                child.StopCoroutine(child.switchHauntedPlayerCoroutine);
            }
            child.switchHauntedPlayerCoroutine = child.StartCoroutine(child.setSwitchingHauntingPlayer());
        }

        [ServerRpc(RequireOwnership = false)]
        public void TeleportPlayerServerRpc(ulong clientId, Vector3 position)
        {
            if (!IsServer) { return; }
            TeleportPlayerClientRpc(clientId, position);
        }

        [ClientRpc]
        private void TeleportPlayerClientRpc(ulong clientId, Vector3 position)
        {
            if (localPlayer.actualClientId != clientId) { return; }
            localPlayer.TeleportPlayer(position);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnExplosionServerRpc(Vector3 explosionPosition, bool spawnExplosionEffect = false, float killRange = 1f, float damageRange = 1f, int nonLethalDamage = 50, float physicsForce = 0f, bool goThroughCar = false)
        {
            if (!IsServer) { return; }
            SpawnExplosionClientRpc(explosionPosition, spawnExplosionEffect, killRange, damageRange, nonLethalDamage, physicsForce, goThroughCar);
        }

        [ClientRpc]
        private void SpawnExplosionClientRpc(Vector3 explosionPosition, bool spawnExplosionEffect = false, float killRange = 1f, float damageRange = 1f, int nonLethalDamage = 50, float physicsForce = 0f, bool goThroughCar = false)
        {
            Landmine.SpawnExplosion(explosionPosition, spawnExplosionEffect, killRange, damageRange, nonLethalDamage, physicsForce, null, goThroughCar);
        }

        [ServerRpc(RequireOwnership = false)]
        public void CadaverBurstFromPlayerServerRpc(ulong clientId)
        {
            if (!IsServer) { return; }
            PlayerControllerB? player = PlayerFromId(clientId);
            if (player == null) { return; }

            CadaverBloomAI cadaverBloom = (CadaverBloomAI)Utils.SpawnEnemy(EnemyKeys.CadaverBloom, new Vector3(1200f, -300f, 0f))!;
            CadaverBurstFromPlayerClientRpc(clientId, cadaverBloom.NetworkObject);
        }

        [ClientRpc]
        private void CadaverBurstFromPlayerClientRpc(ulong clientId, NetworkObjectReference netRef)
        {
            PlayerControllerB? player = PlayerFromId(clientId);
            if (player == null) { return; }
            if (!netRef.TryGet(out NetworkObject netObj)) { return; }
            if (!netObj.TryGetComponent(out CadaverBloomAI cadaverBloom)) { return; }
            cadaverBloom.BurstForth(player, true, player.transform.position, player.transform.eulerAngles);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnMapObjectServerRpc(NamespacedKey<DawnMapObjectInfo> key, Vector3 position, Quaternion rotation = default)
        {
            if (!IsServer) { return; }
            Utils.SpawnMapObject(key, position, rotation);
        }
    }

    [System.Serializable]
    public class AudioClipGroup
    {
        public AudioClip[] clips = null!;
    }

    [HarmonyPatch]
    internal class NetworkHandlerPatches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
        static void StartOfRound_Awake_PostFix()
        {
            if (!IsServerOrHost) { return; }
            var networkHandlerHost = UnityEngine.Object.Instantiate(LethalDiseasesContentHandler.Instance.NetworkHandler?.NetworkHandlerPrefab, Vector3.zero, Quaternion.identity);
            networkHandlerHost?.GetComponent<NetworkObject>().Spawn();
        }
    }
}