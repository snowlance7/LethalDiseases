using Dawn;
using SnowyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Rendering.HighDefinition;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Unlockables
{
    internal class AdvancedShipSanitizationUpgrade : NetworkBehaviour
    {
        public static bool IsEnabled => LethalContent.Unlockables[LethalDiseasesKeys.AdvancedShipSanitizationUpgrade] != null;
        public static AdvancedShipSanitizationUpgrade? Instance { get; private set; }

        [SerializeField] ParticleSystem particleSystem = null!;
        [SerializeField] AudioSource audioSource = null!;

        Transform parentObject = null!;
        HangarShipDoor hangarShipDoor => _door ??= FindObjectOfType<HangarShipDoor>();

        bool closingDoor;
        HangarShipDoor? _door;

        public float cooldown;

        Coroutine? sanitizeRoutine;
        public bool sanitizing => sanitizeRoutine != null;

        public static float SanitizeTime => PluginInstance.Config.Bind("Advanced Ship Sanitization Upgrade Options", "Advanced Ship Sanitization Upgrade | Sanitize Time", 5f, "How long it takes for the ship to be sanitized").Value;
        public static float Cooldown => PluginInstance.Config.Bind("Advanced Ship Sanitization Upgrade Options", "Advanced Ship Sanitization Upgrade | Cooldown", 360f, "Cooldown in seconds for how long it takes for the upgrade to become usable again after use").Value;
        public static float PowerUsage => PluginInstance.Config.Bind("Advanced Ship Sanitization Upgrade Options", "Advanced Ship Sanitization Upgrade | Power Usage", 0.1f, "How much apparatus power should be required to run this upgrade").Value;


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
            }

            if (cooldown > 0f && !StartOfRound.Instance.inShipPhase)
            {
                cooldown -= Time.deltaTime;
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
                    HUDManager.Instance.DisplayTip("Advanced Ship Sanitization", "Safe ship sanitization process will start in 10 seconds. Stay inside ship to be sanitized.");
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
