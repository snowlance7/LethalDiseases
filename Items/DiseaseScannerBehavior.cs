using System.Collections;
using System.Linq;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items
{
    internal class DiseaseScannerBehavior : PhysicsProp // TODO: add scan self behavior
    {
        public Animator animator = null!;
        public AudioSource audioSource = null!;
        public GameObject screenObj = null!;

        Ray ray;
        RaycastHit[] raycastHits = [];
        int anomalyMask = 524296;

        Coroutine? scanRoutine;

        const float scanDistance = 5f;
        const float scanForwardOffset = 3f;
        const bool bioOnly = false; // TODO: Set up configs

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
            if (!buttonDown || insertedBattery.empty) { return; }

            if (scanRoutine != null) { StopCoroutine(scanRoutine); }
            scanRoutine = StartCoroutine(ScanGun());
        }

        IEnumerator ScanGun()
        {
            animator.SetTrigger("scan");
            audioSource.Play();
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
                    ray = new Ray(playerHeldBy.gameplayCamera.transform.position - playerHeldBy.gameplayCamera.transform.forward * scanForwardOffset, playerHeldBy.gameplayCamera.transform.forward);
                    int num = Physics.SphereCastNonAlloc(ray, scanDistance, raycastHits, scanDistance, anomalyMask, QueryTriggerInteraction.Collide);
                    raycastHits = raycastHits.OrderBy((RaycastHit x) => x.distance).ToArray();
                    foreach (var hit in raycastHits)
                    {
                        foundDisease |= hit.transform != null && hit.transform.gameObject.TryGetComponent(out DiseaseHost diseaseHost) && diseaseHost.hasDisease && (diseaseHost.hasActor || !bioOnly);
                    }
                }
                yield return new WaitForSeconds(0.09375f);
            }

            animator.SetTrigger(foundDisease ? "detected" : "not_detected"); // TODO: Fix colors

            scanRoutine = null;
        }
    }
}
