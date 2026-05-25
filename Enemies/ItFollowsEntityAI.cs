using Dawn.Utils;
using GameNetcodeStuff;
using LethalDiseases.UniqueSymptoms;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;
using static LethalDiseases.UniqueSymptoms.ItFollowsSymptom;

namespace LethalDiseases.Enemies
{
    internal class ItFollowsEntity : EnemyAI
    {
        public Transform turnCompass = null!;

        public Collider collider = null!;

        public SmartAgentNavigator nav = null!;

        public ScanNodeProperties scanNode = null!;

        NetworkObject? target;
        HashSet<NetworkObject> targets => SymptomAffectedObjects.symptomAffectedObjects[ItFollowsSymptom.symptomName];

        public new bool isOutside => nav.IsAgentOutside();

        bool enemyMeshEnabled;

        Vector3 lastPosition;
        float currentSpeed;
        float idleTime;
        int currentFootstepSurfaceIndex;
        private int previousFootstepClip;

        public bool isInsideFactory => !isOutside;

        public enum State
        {
            Following
        }

        public override void Start()
        {
            base.Start();

            currentBehaviourStateIndex = (int)State.Following;

            nav.SetAllValues(isOutside: true);
        }

        public override void Update()
        {
            if (StartOfRound.Instance.allPlayersDead) { return; }
            SetVisibility();
        }

        public void LateUpdate()
        {
            currentSpeed = ((transform.position - lastPosition).magnitude / Time.deltaTime) / 2;
            lastPosition = transform.position;
            idleTime = currentSpeed <= 0f ? idleTime + Time.deltaTime : 0f;
        }

        public override void DoAIInterval()
        {
            target = targets.LastOrDefault();

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

        public void PlayFootstepSFX() // Animation
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
        }

        public override void HitEnemy(int force = 0, PlayerControllerB playerWhoHit = null!, bool playHitSFX = true, int hitID = -1) // Synced
        {

        }

        public override void OnCollideWithPlayer(Collider other) // Synced
        {
            base.OnCollideWithPlayer(other);
        }
    }
}