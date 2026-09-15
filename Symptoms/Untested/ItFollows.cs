using Dawn.Utils;
using GameNetcodeStuff;
using SnowyLib;
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
            if (disease.player != null)
            {
                AddSymptomAffectedObject("ItFollows", disease.networkObject);
            }
            return new OnRemoveActionEffect((effect) =>
            {
                if (disease.player == null) { return; }
                RemoveSymptomAffectedObject("ItFollows", disease.networkObject);
            }, disease.ToString(), "ItFollows", disease.strengthTime, SetHighestDurationAndDeny);
        }

        [StaticUpdate]
        public static void ItFollowsUpdate()
        {
            if (!IsServerOrHost || ItFollowsEntityAI.Instance != null || StartOfRound.Instance.inShipPhase || StartOfRound.Instance.shipIsLeaving || symptomAffectedPlayers["ItFollows"].Count <= 0) { return; }
            logger?.LogDebug("Spawning ItFollowsEntity");
            ItFollowsEntityAI.Init();
        }
    }

    internal class ItFollowsEntityAI : NetworkBehaviour
    {
        public static ItFollowsEntityAI? Instance { get; private set; }

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
            Instantiate(LethalDiseasesContentHandler.Instance.DiseaseAssets.ItFollowsEntityPrefab, spawnNode.transform.position, Quaternion.identity);
        }

        public void Start()
        {
            nav.SetAllValues(isOutside: false);
            logger?.LogDebug("ItFollowsEntity spawned");
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (Instance != null && Instance == this)
            {
                Instance = null;
            }
        }

        public override void OnNetworkPostSpawn()
        {
            base.OnNetworkPostSpawn();
            if (Instance == null)
            {
                Instance = this;
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

        void SetVisibility()
        {
            bool shouldBeVisible = symptomAffectedPlayers["ItFollows"].Contains(localPlayer);
            if (shouldBeVisible == meshEnabled) { return; }
            EnableMesh(shouldBeVisible);
        }

        public void EnableMesh(bool enable)
        {
            meshEnabled = enable;
            scanNode.enabled = enable;
            collider.enabled = enable;
        }

        public void OnCollideWithPlayer(Collider other)
        {
            PlayerControllerB player = other.gameObject.GetComponent<PlayerControllerB>();
            if (player == null || player != localPlayer || player != targetPlayer) { return; }
            player.KillPlayer(Vector3.zero);
        }
    }

    internal class ItFollowsEntityCollisionDetect : MonoBehaviour
    {
        public ItFollowsEntityAI mainScript = null!;

        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                mainScript.OnCollideWithPlayer(other);
            }
        }
    }
}