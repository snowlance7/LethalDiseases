using Dawn.Utils;
using GameNetcodeStuff;
using LethalDiseases.UniqueSymptoms;
using SnowyLib;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Enemies
{
    internal class ItFollowsEntity : EnemyAI
    {
        public static ItFollowsEntity? Instance { get; private set; }

        public Transform turnCompass = null!;

        public Collider collider = null!;

        public SmartAgentNavigator nav = null!;

        public ScanNodeProperties scanNode = null!;

        NetworkObject? target;
        HashSet<NetworkObject> targets => SymptomAffectedObjects.symptomAffectedObjects[ItFollowsSymptom.symptomName];

        EnemyAI? targetEnemy;
        new PlayerControllerB? targetPlayer;

        public new bool isOutside => nav.IsAgentOutside();

        bool enemyMeshEnabled;

        Vector3 lastPosition;
        float currentSpeed;
        float idleTime;
        int currentFootstepSurfaceIndex;
        private int previousFootstepClip;

        int hashSpeed;

        public bool isInsideFactory => !isOutside;

        public enum State
        {
            Following
        }

        public static void Init()
        {
            Vector3 mainEntrancePosition = RoundManager.FindMainEntrancePosition(getTeleportPosition: true, getOutsideEntrance: false);
            GameObject? spawnNode = Utils.insideAINodes.GetFarthestFromPosition(mainEntrancePosition, (x) => x.transform.position);
            if (spawnNode == null) { return; }
            Instance = (ItFollowsEntity)Utils.SpawnEnemy(LethalDiseasesKeys.ItFollows, spawnNode.transform.position);
        }

        public override void Start()
        {
            base.Start();

            currentBehaviourStateIndex = (int)State.Following;

            nav.SetAllValues(isOutside: false);

            hashSpeed = Animator.StringToHash("speed");
            logger.LogDebug("ItFollowsEntity spawned");
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance != null && Instance == this)
            {
                Instance = null;
            }
        }

        public override void Update()
        {
            base.Update();
            SetVisibility();
        }

        public void LateUpdate()
        {
            currentSpeed = ((transform.position - lastPosition).magnitude / Time.deltaTime) / 2;
            lastPosition = transform.position;
            idleTime = currentSpeed <= 0f ? idleTime + Time.deltaTime : 0f;
            creatureAnimator.SetFloat(hashSpeed, currentSpeed);
        }

        public override void DoAIInterval()
        {
            nav.agent.speed = 2;
            target = targets.LastOrDefault();
            if (target == null) { logger.LogError("Target is null"); return; } // TODO: Target is null for some reason <<
            targetPlayer = target.gameObject.GetComponent<PlayerControllerB>();
            targetEnemy = target.gameObject.GetComponent<EnemyAI>();

            if (target == null)
            {
                KillEnemyOnOwnerClient();
                return;
            }

            nav.DoPathingToDestination(target.gameObject.transform.position);
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

        public override void AnimationEventA()
        {
            int index;

            GetCurrentMaterialStandingOn();
            index = Random.Range(0, StartOfRound.Instance.footstepSurfaces[currentFootstepSurfaceIndex].clips.Length);
            if (index == previousFootstepClip)
            {
                index = (index + 1) % StartOfRound.Instance.footstepSurfaces[currentFootstepSurfaceIndex].clips.Length;
            }
            creatureSFX.pitch = Random.Range(0.93f, 1.07f);
            creatureSFX.PlayOneShot(StartOfRound.Instance.footstepSurfaces[currentFootstepSurfaceIndex].clips[index], 0.6f);
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

        public override void EnableEnemyMesh(bool enable, bool overrideDoNotSet = false, bool tamperWithMeshes = false)
        {
            base.EnableEnemyMesh(enable, overrideDoNotSet, tamperWithMeshes);
            scanNode.enabled = enable;
            enemyMeshEnabled = enable;
            collider.enabled = enable;
        }

        public override void HitEnemy(int force = 0, PlayerControllerB playerWhoHit = null!, bool playHitSFX = true, int hitID = -1) // Synced
        {
            return;
        }

        public override void OnCollideWithPlayer(Collider other) // Synced
        {
            base.OnCollideWithPlayer(other);
            PlayerControllerB player = other.gameObject.GetComponent<PlayerControllerB>();
            if (player == null || player != localPlayer || player != targetPlayer) { return; }
            player.KillPlayer(Vector3.zero);
        }

        public override void OnCollideWithEnemy(Collider other, EnemyAI collidedEnemy = null)
        {
            base.OnCollideWithEnemy(other, collidedEnemy);
            if (collidedEnemy == null || collidedEnemy != targetEnemy) { return; }
            collidedEnemy.KillEnemyOnOwnerClient();
        }
    }
}