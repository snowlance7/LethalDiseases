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
    internal class DiseaseScannerBehavior : PhysicsProp // TODO
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

        Coroutine? scanRoutine;

        const float scanDistance = 5f;
        const float scanForwardOffset = 3f;
        const bool bioOnly = false; // TODO

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

            SwitchFlashlight(true);
            if (scanRoutine != null) { StopCoroutine(scanRoutine); }
            scanRoutine = StartCoroutine(ScanGun());
        }

        private IEnumerator ScanGun()
        {
            animator.SetTrigger("scan");
            audioSource.PlayOneShot(scanSFX);
            bool foundDisease = false;

            for (int i = 0; i < 9; i++)
            {
                if (base.IsOwner)
                {
                    logger.LogDebug($"Scanning {i}");
                    if (isPocketed || !isHeld)
                    {
                        yield break;
                    }
                    ray = new Ray(playerHeldBy.gameplayCamera.transform.position - playerHeldBy.gameplayCamera.transform.forward * 3f, playerHeldBy.gameplayCamera.transform.forward);
                    int num = Physics.SphereCastNonAlloc(ray, scanDistance, raycastHits, scanDistance, anomalyMask, QueryTriggerInteraction.Collide);
                    raycastHits = raycastHits.OrderBy((RaycastHit x) => x.distance).ToArray();
                    foreach (var hit in raycastHits)
                    {
                        foundDisease |= hit.transform != null && hit.transform.gameObject.TryGetComponent(out DiseaseHost diseaseHost) && diseaseHost.hasDisease && (diseaseHost.isActor || !bioOnly);
                    }
                }
                yield return new WaitForSeconds(0.09375f);
            }
            SwitchFlashlight(false);

            animator.SetTrigger(foundDisease ? "detected" : "no_detected");

            scanRoutine = null;
        }

        public void SwitchFlashlight(bool on)
        {
            flashlightBulb.enabled = on;
            flashlightBulbGlow.enabled = on;
        }
    }
}
