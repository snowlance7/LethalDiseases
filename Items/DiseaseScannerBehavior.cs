using GameNetcodeStuff;
using SnowyLib;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items
{
    internal class DiseaseScannerBehavior : PhysicsProp // TODO: add scan self behavior
    {
        public Animator animator = null!;
        public AudioSource audioSource = null!;

        PlayerControllerB previousPlayerHeldBy = null!;

        Coroutine? scanRoutine;

        const float scanRadius = 5f;
        const float scanDistance = 5f;

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0.08f, 0.19f, 0.05f);
            itemProperties.rotationOffset = new Vector3(0, 100, -90);
            itemProperties.floorYOffset = 0;
            itemProperties.syncGrabFunction = true;
            itemProperties.syncDiscardFunction = true;
            itemProperties.syncUseFunction = true;
            //itemProperties.grabAnim = "HoldPatcherTool";
            grabbableToEnemies = false;
        }

        public override void OnDestroy()
        {
            DiseaseScanNode.ClearScanNodesOnScreen();
            base.OnDestroy();
        }

        public override void PocketItem()
        {
            base.PocketItem();
            if (previousPlayerHeldBy != null && previousPlayerHeldBy == localPlayer)
            {
                previousPlayerHeldBy.equippedUsableItemQE = false;
                DiseaseScanNode.ClearScanNodesOnScreen();
            }
        }

        public override void DiscardItem()
        {
            base.DiscardItem();
            if (previousPlayerHeldBy != null && previousPlayerHeldBy == localPlayer)
            {
                previousPlayerHeldBy.equippedUsableItemQE = false;
                DiseaseScanNode.ClearScanNodesOnScreen();
            }
        }

        public override void EquipItem()
        {
            base.EquipItem();
            previousPlayerHeldBy = playerHeldBy;
            if (previousPlayerHeldBy != null && previousPlayerHeldBy == localPlayer)
                previousPlayerHeldBy.equippedUsableItemQE = true;
        }

        public override void ItemInteractLeftRight(bool right)
        {
            if (right) // E: Clear disease scan nodes TODO
            {
                DiseaseScanNode.ClearScanNodesOnScreen();
            }
            else // Q: Scan self TODO
            {
                logger.LogDebug("Scanning self");

                if (scanRoutine != null) { StopCoroutine(scanRoutine); }
                scanRoutine = StartCoroutine(SelfScan());

            }
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!buttonDown || insertedBattery.empty) { return; }

            if (scanRoutine != null) { StopCoroutine(scanRoutine); }
            scanRoutine = StartCoroutine(Scan());
        }

        IEnumerator SelfScan()
        {
            animator.SetTrigger("self_scan");
            audioSource.Play();

            yield return new WaitForSeconds(0.84375f);

            bool infected = playerHeldBy.NetworkObject.HasDisease();
            animator.SetTrigger(infected ? "red" : "green");
            scanRoutine = null;
        }

        IEnumerator Scan()
        {
            bool foundDisease = false;
            animator.SetTrigger("scan");
            audioSource.Play();

            RaycastHit[] raycastHits = new RaycastHit[20];
            DiseaseScanNode.EnableColliders(true);

            for (int i = 0; i < 9; i++)
            {
                if (base.IsOwner)
                {
                    if (isPocketed || !isHeld) { yield break; }
                    int hitCount = Physics.SphereCastNonAlloc(new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward), scanRadius, raycastHits, scanDistance, PluginInstance.playerEnemiesPropsInteractableObjectMask);
                    logger.LogDebug($"Scanning {i}: {hitCount} hits");
                }
                yield return new WaitForSeconds(0.09375f);
            }

            foreach (var hit in raycastHits)
            {
                if (hit.transform != null) { logger.LogDebug(hit.transform.name); }
                if (hit.transform != null && hit.collider.TryGetComponentInChildren(out DiseaseScanNode? diseaseScanNode) && diseaseScanNode != null && diseaseScanNode.diseaseHost != null && diseaseScanNode.diseaseHost.player != playerHeldBy && diseaseScanNode.diseaseHost.hasDisease)
                {
                    // TODO: Make sure this works on enemies and players
                    foundDisease = true;
                    diseaseScanNode.SetScanNode("DISEASES DETECTED", "", (int)scanDistance * 2, requiresLineOfSight: true);
                }
            }

            animator.SetTrigger(foundDisease ? "red" : "green");
            DiseaseScanNode.EnableColliders(false);
            scanRoutine = null;
        }
    }
}
