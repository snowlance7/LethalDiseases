using Dawn.Utils;
using GameNetcodeStuff;
using LethalDiseases.Enemies;
using SnowyLib;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;
using static LethalDiseases.SymptomAffectedObjects;

namespace LethalDiseases.Symptoms
{
    internal static partial class Symptoms // TODO: Needs more testing
    {
        [Symptom("ItFollows", "???", Symptom.SymptomType.Bad, 10)]
        public static StatusEffect ItFollows(Disease disease)
        {
            logger?.LogDebug("Init symptom ItFollows");
            if (disease.hasActor)
            {

                NetworkHandler.Instance.AddSymptomAffectedObjectRpc("ItFollows", disease.networkObject);
            }
            return new OnRemoveActionEffect((effect) =>
            {
                if (!disease.hasActor) { return; }
                NetworkHandler.Instance.RemoveSymptomAffectedObjectRpc("ItFollows", disease.networkObject);
            }, disease.ToString(), "ItFollows", disease.strengthTime, SetHighestDurationAndDeny);
        }

        [StaticUpdate]
        public static void ItFollowsUpdate()
        {
            if (!IsServerOrHost || ItFollowsEntity.Instance != null || StartOfRound.Instance.inShipPhase || StartOfRound.Instance.shipIsLeaving || symptomAffectedObjects["ItFollows"].Count <= 0) { return; }
            logger?.LogDebug("Spawning ItFollowsEntity");
            ItFollowsEntity.Init();
        }
    }

    internal class ItFollowsEntity : NetworkBehaviour
    {
        public static ItFollowsEntity? Instance { get; private set; }

        public Transform turnCompass = null!;
        public Collider collider = null!;
        public SmartAgentNavigator nav = null!;
        public ScanNodeProperties scanNode = null!;
        public Animator animator = null!;
        public AudioSource audioSource = null!;

        PlayerControllerB? targetPlayer => symptomAffectedPlayers["ItFollows"].LastOrDefault();

        public bool isOutside => nav.IsAgentOutside();
        public bool isInsideFactory => !isOutside;

        bool meshEnabled;

        int currentFootstepSurfaceIndex;
        private int previousFootstepClip;

        float timeSinceAIInterval;

        public static void Init()
        {
            Vector3 mainEntrancePosition = RoundManager.FindMainEntrancePosition(getTeleportPosition: true, getOutsideEntrance: false);
            GameObject? spawnNode = Utils.insideAINodes.GetFarthestFromPosition(mainEntrancePosition, (x) => x.transform.position);
            if (spawnNode == null) { return; }
            Instance = (ItFollowsEntity)Utils.SpawnEnemy(LethalDiseasesKeys.ItFollowsEntity, spawnNode.transform.position);
        }

        public void Start()
        {
            nav.SetAllValues(isOutside: false);
            logger?.LogDebug("ItFollowsEntity spawned");
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance != null && Instance == this)
            {
                Instance = null;
            }
        }

        public void Update()
        {
            SetVisibility();

            if (!IsServer) { return; }

            timeSinceAIInterval += Time.deltaTime;
            if (timeSinceAIInterval > 0.2f)
            {
                timeSinceAIInterval = 0f;
                DoAIInterval();
            }
        }

        public void DoAIInterval()
        {
            if (targetPlayer == null)
            {
                NetworkObject.Despawn(destroy: true);
                return;
            }

            nav.DoPathingToDestination(targetPlayer.transform.position);
        }

        void GetCurrentMaterialStandingOn()
        {
            Ray interactRay = new Ray(transform.position + Vector3.up, -Vector3.up);
            if (!Physics.Raycast(interactRay, out RaycastHit hit, 6f, StartOfRound.Instance.walkableSurfacesMask, QueryTriggerInteraction.Ignore) || hit.collider.CompareTag(StartOfRound.Instance.footstepSurfaces[currentFootstepSurfaceIndex].surfaceTag))
            {
                return;
            }
            for (int i = 0; i < StartOfRound.Instance.footstepSurfaces.Length; i++)
            {
                if (hit.collider.CompareTag(StartOfRound.Instance.footstepSurfaces[i].surfaceTag))
                {
                    currentFootstepSurfaceIndex = i;
                    break;
                }
            }
        }

        public void PlayFootstepSFX() // Animation
        {
            int index;

            GetCurrentMaterialStandingOn();
            index = Random.Range(0, StartOfRound.Instance.footstepSurfaces[currentFootstepSurfaceIndex].clips.Length);
            if (index == previousFootstepClip)
            {
                index = (index + 1) % StartOfRound.Instance.footstepSurfaces[currentFootstepSurfaceIndex].clips.Length;
            }
            audioSource.pitch = Random.Range(0.93f, 1.07f);
            audioSource.PlayOneShot(StartOfRound.Instance.footstepSurfaces[currentFootstepSurfaceIndex].clips[index], 0.6f);
            previousFootstepClip = index;
        }

        bool IsLocalPlayerTarget()
        {
            foreach (var affectedObject in targets)
            {
                if (!affectedObject.gameObject.TryGetComponent(out PlayerControllerB player)) { continue; }
                if (player == localPlayer) { return true; }
            }
            return false;
        }

        void SetVisibility()
        {
            bool shouldBeVisible = IsLocalPlayerTarget();
            if (shouldBeVisible == enemyMeshEnabled) { return; }
            EnableEnemyMesh(shouldBeVisible);
        }

        public void EnableMesh(bool enable)
        {
            meshEnabled = enable;
            scanNode.enabled = enable;
            collider.enabled = enable;
        }

        public override void OnCollideWithPlayer(Collider other)
        {
            base.OnCollideWithPlayer(other);
            PlayerControllerB player = other.gameObject.GetComponent<PlayerControllerB>();
            if (player == null || player != localPlayer || player != targetPlayer) { return; }
            player.KillPlayer(Vector3.zero);
        }
    }
}