using DigitalRuby.ThunderAndLightning;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items
{
    internal class DiseaseAnalyzerBehavior : PhysicsProp // TODO
    {
        public Animator animator = null!;
        public AudioClip scanSFX = null!;
        public AudioSource audioSource = null!;
        public Light flashlightBulb = null!;
        public Light flashlightBulbGlow = null!;
        public GameObject screenObj = null!;

        private Ray ray;
        private RaycastHit[] raycastHits = [];

        private int anomalyMask = 524296;
        private RaycastHit hit;

        private bool isScanning;

        const float scanDistance = 5f;
        const float scanForwardOffset = 3f;

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0.08f, 0.19f, 0.05f);
            itemProperties.rotationOffset = new Vector3(0, 100, -90);
            itemProperties.floorYOffset = 0;
            itemProperties.syncGrabFunction = true;
            itemProperties.syncDiscardFunction = true;
            itemProperties.syncUseFunction = true;
        }

        public override void PocketItem()
        {
            base.PocketItem();
            screenObj.SetActive(false);
        }

        public override void DiscardItem()
        {
            base.DiscardItem();
            screenObj.SetActive(false);
        }

        public override void EquipItem()
        {
            base.EquipItem();
            screenObj.SetActive(true);
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!buttonDown) { return; }

            logger.LogDebug("ItemActivate");
            if (insertedBattery.empty) { return; }

            SwitchFlashlight(on: true);
            StartCoroutine(ScanGun());
        }

        private IEnumerator ScanGun()
        {
            animator.SetTrigger("scan"); // TODO: Make this only sweep once and the analyzer sweeps multiple times
            audioSource.PlayOneShot(scanSFX);

            for (int i = 0; i < 12; i++)
            {
                if (base.IsOwner)
                {
                    logger.LogDebug($"Scanning {i}");
                    if (isPocketed)
                    {
                        yield break;
                    }
                    ray = new Ray(playerHeldBy.gameplayCamera.transform.position - playerHeldBy.gameplayCamera.transform.forward * scanForwardOffset, playerHeldBy.gameplayCamera.transform.forward);
                    int num = Physics.SphereCastNonAlloc(ray, scanDistance, raycastHits, scanDistance, anomalyMask, QueryTriggerInteraction.Collide);
                    raycastHits = raycastHits.OrderBy((RaycastHit x) => x.distance).ToArray();
                    foreach (var hit in raycastHits)
                    {
                        if (hit.transform != null && hit.transform.gameObject.TryGetComponent<DiseaseHost>(out var component))
                        {
                            //Vector3 shockablePosition = component.GetShockablePosition();

                        }
                    }
                }
                yield return new WaitForSeconds(0.125f);
            }
            logger.LogDebug("Zap gun light off!!!");
            SwitchFlashlight(on: false);
            isScanning = false;
        }

        public void SwitchFlashlight(bool on)
        {
            flashlightBulb.enabled = on;
            flashlightBulbGlow.enabled = on;
        }
    }
}
