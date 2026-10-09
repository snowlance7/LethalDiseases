using Dawn;
using SnowyLib;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Unlockables
{
    internal class ShipSanitizationUpgrade : NetworkBehaviour
    {
        public static bool IsEnabled => LethalContent.Unlockables[LethalDiseasesKeys.ShipSanitizationUpgrade] != null;
        public static ShipSanitizationUpgrade? Instance { get; private set; }

        [SerializeField] ParticleSystem particleSystem = null!;
        [SerializeField] AudioSource audioSource = null!;

        Transform parentObject = null!;
        HangarShipDoor hangarShipDoor => _door ??= FindObjectOfType<HangarShipDoor>();

        bool closingDoor;
        HangarShipDoor? _door;

        float timeSinceDamageActors;
        public float cooldown;

        Coroutine? sanitizeRoutine;
        public bool sanitizing => sanitizeRoutine != null;

        public static float SanitizeTime => PluginInstance.Config.Bind("Ship Sanitization Upgrade Options", "Ship Sanitization Upgrade | Sanitize Time", 10f, "How long it takes for the ship to be sanitized").Value;
        public static float Cooldown => PluginInstance.Config.Bind("Ship Sanitization Upgrade Options", "Ship Sanitization Upgrade | Cooldown", 120f, "Cooldown in seconds for how long it takes for the upgrade to become usable again after use").Value;
        public static float PowerUsage => PluginInstance.Config.Bind("Ship Sanitization Upgrade Options", "Ship Sanitization Upgrade | Power Usage", 0.1f, "").Value; // TODO

        [StaticInit]
        public static void Init()
        {
            _ = SanitizeTime;
            _ = Cooldown;
            _ = PowerUsage;
        }

        public void Awake()
        {
            Instance ??= this;
        }

        public void Start()
        {
            parentObject = GameObject.Find("HangarShip").transform.Find("ShipInside");
        }

        public void Update()
        {
            transform.position = parentObject.position;

            if (closingDoor)
            {
                hangarShipDoor.doorPower = 1f;

                timeSinceDamageActors += Time.deltaTime;

                if (timeSinceDamageActors > 1f)
                {
                    timeSinceDamageActors = 0f;
                    DamageActorsInShip();
                }
            }

            if (cooldown > 0f && !StartOfRound.Instance.inShipPhase)
            {
                cooldown -= Time.deltaTime;
            }
        }

        private void DamageActorsInShip()
        {
            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                if (!player.isInHangarShipRoom || !player.isPlayerControlled) { continue; }
                player.inSpecialInteractAnimation = true;
                player.DamagePlayer(5, hasDamageSFX: false, callRPC: false, causeOfDeath: CauseOfDeath.Suffocation);
                player.inSpecialInteractAnimation = false;
            }
            foreach (var enemy in RoundManager.Instance.SpawnedEnemies)
            {
                if (enemy.isEnemyDead || !enemy.enemyType.canDie || !enemy.isInsidePlayerShip) { continue; }
                enemy.HitEnemy();
            }
        }

        private void CloseHangarDoor(bool close)
        {
            closingDoor = close;
            hangarShipDoor.shipDoorsAnimator.SetBool("Closed", close);
            hangarShipDoor.SetDoorButtonsEnabled(!close);
        }

        private void SanitizeShip()
        {
            foreach (var host in DiseaseHost.Instances)
            {
                if ((host.player != null && host.player.isInHangarShipRoom)
                    || (host.grabbableObject != null && host.grabbableObject.isInShipRoom)
                    || (host.enemy != null && host.enemy.isInsidePlayerShip))
                {
                    host.ClearDiseases();
                }
            }
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void SanitizeRpc()
        {
            IEnumerator sanitize()
            {
                yield return null;

                if (localPlayer.isInHangarShipRoom)
                {
                    HUDManager.Instance.DisplayTip("Ship Sanitization", "Ship sanitization process will start in 10 seconds. Please exit the ship.", isWarning: true);
                }

                yield return new WaitForSeconds(10f);

                CloseHangarDoor(true);
                particleSystem.Play();
                audioSource.Play();

                yield return new WaitForSeconds(SanitizeTime);

                CloseHangarDoor(false);
                particleSystem.Stop();
                audioSource.Stop();

                SanitizeShip();
                cooldown = Cooldown;
                sanitizeRoutine = null;
            }

            sanitizeRoutine = StartCoroutine(sanitize());
        }
    }
}
