using GameNetcodeStuff;
using SnowyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items
{
    internal class DiseaseAnalyzerBehavior : PhysicsProp // TODO: add scan self behavior (use overheat player animation for it?)
    {
        public Animator animator = null!;
        public AudioSource audioSource = null!;
        public MeshRenderer screenRenderer = null!;
        public GameObject scanNodePrefab = null!;

        PlayerControllerB previousPlayerHeldBy = null!;

        int mask = 524872; // Props, InteractableObject, Enemies, Player

        Coroutine? scanRoutine;

        const float scanRadius = 5f;
        const float scanDistance = 10f;
        const float scanForwardOffset = 1f;//3f;
        const int maxScanNodes = 10;
        const int anomalyMask = 524296;

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(-0.1f, 0.235f, 0.035f);
            itemProperties.rotationOffset = new Vector3(0, 10, -90);
            itemProperties.floorYOffset = 0;
            itemProperties.syncGrabFunction = true;
            itemProperties.syncDiscardFunction = true;
            itemProperties.syncUseFunction = true;
            itemProperties.grabAnim = "HoldPatcherTool";
            grabbableToEnemies = false;
        }

        public override void Update()
        {
            base.Update();
            //localPlayer.LineOfSightToPositionAngle
        }

        public override void OnDestroy()
        {
            DiseaseScanNode.ClearScanNodesOnScreen();
            base.OnDestroy();
        }

        public override void PocketItem()
        {
            base.PocketItem();
            screenRenderer.enabled = false;
            if (previousPlayerHeldBy != null && previousPlayerHeldBy == localPlayer)
            {
                previousPlayerHeldBy.equippedUsableItemQE = false;
                DiseaseScanNode.ClearScanNodesOnScreen();
            }
        }

        public override void DiscardItem()
        {
            base.DiscardItem();
            screenRenderer.enabled = false;
            if (previousPlayerHeldBy != null && previousPlayerHeldBy == localPlayer)
            {
                previousPlayerHeldBy.equippedUsableItemQE = false;
                DiseaseScanNode.ClearScanNodesOnScreen();
            }
        }

        public override void EquipItem()
        {
            base.EquipItem();
            screenRenderer.enabled = true;
            previousPlayerHeldBy = playerHeldBy;
            if (previousPlayerHeldBy != null && previousPlayerHeldBy == localPlayer)
                previousPlayerHeldBy.equippedUsableItemQE = true;
        }

        public override void ItemInteractLeftRight(bool right)
        {
            if (right) // E: Clear disease scan nodes TODO
            {
                logger.LogDebug("Clearing disease scan nodes");
                DiseaseScanNode.ClearScanNodesOnScreen();
            }
            else // Q: Scan self TODO
            {
                logger.LogDebug("Scanning self");
                // TODO: Add seperate ui for this
            }
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!buttonDown || insertedBattery.empty) { return; }

            DiseaseScanNode.ClearScanNodesOnScreen();
            if (scanRoutine != null) { StopCoroutine(scanRoutine); }
            scanRoutine = StartCoroutine(ScanGun());
        }

        IEnumerator ScanGun()
        {
            animator.SetTrigger("scan");
            audioSource.Play();

            RaycastHit[] raycastHits = new RaycastHit[maxScanNodes];

            for (int i = 0; i < 12; i++)
            {
                if (base.IsOwner)
                {
                    if (isPocketed || !isHeld) { yield break; }
                    DiseaseScanNode.EnableColliders(true);
                    int hitCount = Physics.SphereCastNonAlloc(new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward), scanRadius, raycastHits, scanDistance, mask);
                    DiseaseScanNode.EnableColliders(false);
                    logger.LogDebug($"Scanning {i}: {hitCount} hits");
                }
                yield return new WaitForSeconds(0.125f);
            }

            foreach (var hit in raycastHits)
            {
                //if (hit.transform != null) { logger.LogDebug(hit.transform.name); }
                if (hit.transform != null && hit.collider.transform.root.TryGetComponent(out DiseaseHost diseaseHost) && diseaseHost.player != playerHeldBy && diseaseHost.hasDisease)
                {
                    diseaseHost.diseaseScanNode?.SetScanNode((int)scanDistance * 2);
                }
            }

            scanRoutine = null;
        }
    }
}
