using Dawn;
using GameNetcodeStuff;
using HarmonyLib;
using LethalDiseases.Items;
using SnowyCraftingCore;
using SnowyCraftingCore.TerminalAdditions;
using SnowyLib;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static LethalDiseases.Plugin;

namespace LethalDiseases
{
    internal class NetworkHandler : NetworkBehaviour
    {
        public static NetworkHandler Instance { get; private set; } = null!;

        public enum SoundEffect
        {
            Sneeze,
            Cough,
            CoughHeavy,
            Paranoia,
            Duck,
            Sponge
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer) { Instance?.gameObject.GetComponent<NetworkObject>().Despawn(destroy: true); }
            Instance = this;
            logger?.LogDebug("NetworkHandler spawned");
            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn()
        {
            if (Instance != null && Instance == this) { Instance = null!; }
            base.OnNetworkDespawn();
        }

        public void Start()
        {
            Symptoms.Symptoms.Load();

            ChemistryIngredient testTubeIngredient = new ChemistryIngredient(LethalDiseasesKeys.TestTube);
            CraftingAPI.RegisterIngredient(testTubeIngredient);

            ChemistryIngredient cottonSwabIngredient = new ChemistryIngredient(LethalDiseasesKeys.CottonSwab);
            CraftingAPI.RegisterIngredient(cottonSwabIngredient);

            CraftingAPI.RegisterRecipe(new ChemistryRecipe(testTubeIngredient, testTubeIngredient, (ingredientA, ingredientB) =>
            {
                Disease? disease1 = Disease.GetDiseaseFromString(ingredientA.specialInstructions);
                Disease? disease2 = Disease.GetDiseaseFromString(ingredientB.specialInstructions);

                if (disease1 != null && disease2 != null)
                {
                    var array1 = disease1.symptoms;
                    var array2 = disease2.symptoms;

                    var result = array1.Except(array2).Concat(array2.Except(array1)).ToArray();
                    Disease disease3 = Disease.MergeDiseases(disease1, disease2);
                    disease3.symptoms = result;
                    return new ChemistryIngredient(LethalDiseasesKeys.TestTube, disease3.GetChemistryLiquidAppearance(), disease3.ToString());
                }
                else if (disease1 != null)
                {
                    return ingredientA;
                }
                else if (disease2 != null)
                {
                    return ingredientB;
                }
                else
                {
                    return null; // TODO
                }
            }));
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void MufflePlayerRpc(ulong clientId, bool value)
        {
            PlayerControllerB? player = PlayerFromId(clientId);
            if (player == null) { return; }
            player.MufflePlayer(value);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void InfectRpc(NetworkObjectReference netRef, string diseaseId) // TODO: Should only have 1 reference
        {
            Disease? disease = Disease.GetDiseaseFromString(diseaseId);
            if (disease == null || !netRef.TryGet(out NetworkObject netObj) || netObj == null) { return; }
            logger?.LogDebug($"Infecting {netObj.gameObject.name} with disease ID: {diseaseId}");
            netObj.AddDisease(disease);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void PlaySoundAtPositionRpc(ulong clientId, SoundEffect soundEffect, int bodyPartIndex = 0, float volume = 1f, float min3DDistance = 1f, float max3DDistance = 10f, float cutoffFrequency = 22000, int audibleNoiseID = 0)
        {
            PlayerControllerB? player = PlayerFromId(clientId);
            if (player == null) { return; }
            Transform position = player.bodyParts[bodyPartIndex];

            var clips = LethalDiseasesContentHandler.Instance.DiseaseAssets!.AudioLibrary.GetClips(soundEffect.ToString());
            if (clips == null) { logger.LogError($"Couldnt find audio clips for {soundEffect} sound effect"); return; }
            Utils.PlaySoundAtPosition(position, clips, volume, true, true, min3DDistance, max3DDistance, cutoffFrequency, audibleNoiseID);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void PlaySoundAtPositionRpc(Vector3 position, SoundEffect soundEffect, float volume = 1f, float min3DDistance = 1f, float max3DDistance = 10f, float cutoffFrequency = 22000, int audibleNoiseID = 0)
        {
            var clips = LethalDiseasesContentHandler.Instance.DiseaseAssets!.AudioLibrary.GetClips(soundEffect.ToString());
            if (clips == null) { logger.LogError($"Couldnt find audio clips for {soundEffect} sound effect"); return; }
            Utils.PlaySoundAtPosition(position, clips, volume, true, true, min3DDistance, max3DDistance, cutoffFrequency, audibleNoiseID);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void PlaySoundAtPositionRpc(ulong clientId, string clipName, int bodyPartIndex = 0, float volume = 1f, float min3DDistance = 1f, float max3DDistance = 10f, float cutoffFrequency = 22000, int audibleNoiseID = 0)
        {
            PlayerControllerB? player = PlayerFromId(clientId);
            if (player == null) { return; }
            Transform position = player.bodyParts[bodyPartIndex];

            var clip = LethalDiseasesContentHandler.Instance.DiseaseAssets!.AudioLibrary.GetClip(clipName);
            if (clip == null) { logger.LogError($"Couldnt find audio clip for {clipName}"); return; }
            Utils.PlaySoundAtPosition(position, clip, volume, true, true, min3DDistance, max3DDistance, cutoffFrequency, audibleNoiseID);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void PlaySoundAtPositionRpc(Vector3 position, string clipName, float volume = 1f, float min3DDistance = 1f, float max3DDistance = 10f, float cutoffFrequency = 22000, int audibleNoiseID = 0)
        {
            var clip = LethalDiseasesContentHandler.Instance.DiseaseAssets!.AudioLibrary.GetClip(clipName);
            if (clip == null) { logger.LogError($"Couldnt find audio clip for {clipName}"); return; }
            Utils.PlaySoundAtPosition(position, clip, volume, true, true, min3DDistance, max3DDistance, cutoffFrequency, audibleNoiseID);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void SlimePlayersRpc(ulong[] clientIds, string diseaseId)
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

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void PlacePukeDecalRpc(Vector3 position, Vector3 direction, float despawnTime = 0)
        {
            DecalProjector projector = Instantiate(LethalDiseasesContentHandler.Instance.DiseaseAssets!.PukeProjectorPrefab, position, Quaternion.LookRotation(direction)).GetComponent<DecalProjector>();
            if (despawnTime > 0)
                Destroy(projector.gameObject, despawnTime);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void PlayerDiscardHeldObjectRpc(ulong clientId)
        {
            PlayerControllerB? player = PlayerFromId(clientId);
            player?.DiscardHeldObject();
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void AddSymptomAffectedObjectRpc(string symptomName, NetworkObjectReference netRef)
        {
            SymptomAffectedObjects.AddSymptomAffectedObject(symptomName, netRef);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void RemoveSymptomAffectedObjectRpc(string symptomName, NetworkObjectReference netRef)
        {
            SymptomAffectedObjects.RemoveSymptomAffectedObject(symptomName, netRef);
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        public void SpawnGhostGirlRpc(ulong targetPlayerId)
        {
            if (!IsServer) { return; }
            DressGirlAI child = (DressGirlAI)Utils.SpawnEnemy(EnemyKeys.Girl, Vector3.zero)!;
            SpawnGhostGirlRpc(targetPlayerId, child.NetworkObject);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        void SpawnGhostGirlRpc(ulong targetPlayerId, NetworkObjectReference netRef)
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

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void TeleportPlayerRpc(ulong clientId, Vector3 position)
        {
            if (localPlayer.actualClientId != clientId) { return; }
            localPlayer.TeleportPlayer(position);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void SpawnExplosionRpc(Vector3 explosionPosition, bool spawnExplosionEffect = false, float killRange = 1f, float damageRange = 1f, int nonLethalDamage = 50, float physicsForce = 0f, bool goThroughCar = false)
        {
            Landmine.SpawnExplosion(explosionPosition, spawnExplosionEffect, killRange, damageRange, nonLethalDamage, physicsForce, null, goThroughCar);
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        public void CadaverBurstFromPlayerRpc(ulong clientId)
        {
            if (!IsServer) { return; }
            PlayerControllerB? player = PlayerFromId(clientId);
            if (player == null) { return; }

            CadaverBloomAI cadaverBloom = (CadaverBloomAI)Utils.SpawnEnemy(EnemyKeys.CadaverBloom, new Vector3(1200f, -300f, 0f))!;
            CadaverBurstFromPlayerRpc(clientId, cadaverBloom.NetworkObject);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        private void CadaverBurstFromPlayerRpc(ulong clientId, NetworkObjectReference netRef)
        {
            PlayerControllerB? player = PlayerFromId(clientId);
            if (player == null) { return; }
            if (!netRef.TryGet(out NetworkObject netObj)) { return; }
            if (!netObj.TryGetComponent(out CadaverBloomAI cadaverBloom)) { return; }
            cadaverBloom.BurstForth(player, true, player.transform.position, player.transform.eulerAngles);
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        public void SpawnMapObjectRpc(NamespacedKey<DawnMapObjectInfo> key, Vector3 position, Quaternion rotation = default)
        {
            if (!IsServer) { return; }
            Utils.SpawnMapObject(key, position, rotation);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void RenameDiseaseRpc(string id, string name)
        {
            Disease? disease = Disease.GetDiseaseFromString(id);
            if (disease == null) { logger.LogError($"Failed to rename disease with id: {id}, disease not found"); return; }
            string previousName = disease.name;
            disease.name = name;

            Utils.LogChat($"Disease {previousName} has been renamed to {name}");
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void SynthesizeDiseaseRpc(string id)
        {
            SmallItemDispenser.Instance!.ItemDispenseOperation(LethalDiseasesTerminalAPI.testTubeDispensable, (item) => ((TestTubeBehavior)item).OnChemicalOutput(new ChemistryIngredient(LethalDiseasesKeys.TestTube, specialInstructions: id)), 30f);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void SynthesizeDiseaseSyringeRpc(string id)
        {
            SmallItemDispenser.Instance!.ItemModificationOperation(LethalDiseasesTerminalAPI.syringeDispensable, (item) => ((SyringeBehavior)item).SetDiseaseRpc(id, false), 30f, 10f, 30f);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void AnalyzeDiseaseRpc(ulong clientId)
        {
            SmallItemDispenser.Instance!.ItemModificationOperation([LethalDiseasesTerminalAPI.testTubeDispensable, LethalDiseasesTerminalAPI.cottonSwabDispensable], (inputItem) =>
            {
                Disease? disease = null;

                if (inputItem is CottonSwabBehavior cottonSwab)
                {
                    disease = Disease.GetDiseaseFromString(cottonSwab.storedDisease);
                }
                else if (inputItem is TestTubeBehavior testTube)
                {
                    disease = Disease.GetDiseaseFromString(testTube.storedDisease);
                }

                if (disease != null)
                {
                    disease.name = $"disease{Disease.namedDiseases.Count}";

                    if (localPlayer.actualClientId == clientId)
                    {
                        HUDManager.Instance.DisplayTip("Analysis succeeded", $"Use 'DGET {disease.name}' in the terminal to see the results");
                    }
                }
                else
                {
                    if (localPlayer.actualClientId == clientId)
                    {
                        HUDManager.Instance.DisplayTip("Analysis failed", "No disease detected on sample");
                    }
                }
            }, 30f, 20f, 30f);
        }
    }

    [HarmonyPatch]
    internal class NetworkHandlerPatches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
        static void StartOfRound_Awake_PostFix()
        {
            if (!IsServerOrHost) { return; }
            var networkHandlerHost = UnityEngine.Object.Instantiate(LethalDiseasesContentHandler.Instance.DiseaseAssets?.NetworkHandlerPrefab, Vector3.zero, Quaternion.identity);
            networkHandlerHost?.GetComponent<NetworkObject>().Spawn();
        }
    }
}