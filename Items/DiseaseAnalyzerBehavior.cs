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

        PlayerControllerB previousPlayerHeldBy = null!;

        HashSet<DiseaseScanNode> visibleNodes = new HashSet<DiseaseScanNode>();

        const int mask = 524872; // Props, InteractableObject, Enemies, Player

        Coroutine? scanRoutine;

        DiseaseScanNode? nodeInLOS;

        float timeSinceScreenRefresh;

        const float scanRadius = 5f;
        const float scanDistance = 10f;

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

            if (visibleNodes.Count > 0)
            {
                timeSinceScreenRefresh += Time.deltaTime;

                if (timeSinceScreenRefresh > 1f)
                {
                    timeSinceScreenRefresh = 0f;

                    nodeInLOS = GetNodeInLineOfSightToPositionAngle();
                    RefreshScreen();
                }
            }
        }

        public override void OnDestroy()
        {
            ClearNodes();
            base.OnDestroy();
        }

        public override void PocketItem()
        {
            base.PocketItem();
            screenRenderer.enabled = false;
            if (previousPlayerHeldBy != null && previousPlayerHeldBy == localPlayer)
            {
                previousPlayerHeldBy.equippedUsableItemQE = false;
                ClearNodes();
            }
        }

        public override void DiscardItem()
        {
            base.DiscardItem();
            screenRenderer.enabled = false;
            if (previousPlayerHeldBy != null && previousPlayerHeldBy == localPlayer)
            {
                previousPlayerHeldBy.equippedUsableItemQE = false;
                ClearNodes();
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
                ClearNodes();
            }
            else // Q: Scan self TODO
            {
                animator.SetTrigger("self_scan");
                audioSource.Play();
                var host = playerHeldBy.NetworkObject.GetHost();
                if (!host.hasDisease) { return; }
                nodeInLOS = host.diseaseScanNode;
                RefreshScreen();
            }
        }

        void RefreshScreen()
        {
            if (nodeInLOS == null)
            {
                screenRenderer.enabled = false;
                return;
            }

            // TODO
        }

        DiseaseScanNode? GetNodeInLineOfSightToPositionAngle()
        {
            if (visibleNodes.Count == 0) { return null; }

            var closestNode = visibleNodes.FirstOrDefault();
            var closestDistance = Mathf.Infinity;

            foreach (var node in visibleNodes)
            {
                float distance = Vector3.Angle(localPlayer.playerEye.transform.forward, node.transform.position - localPlayer.gameplayCamera.transform.position);
                if (distance > closestDistance) { continue; }
                closestNode = node;
                closestDistance = distance;
            }

            return closestNode;
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!buttonDown || insertedBattery.empty) { return; }

            ClearNodes();
            if (scanRoutine != null) { StopCoroutine(scanRoutine); }
            scanRoutine = StartCoroutine(ScanGun());
        }

        void ClearNodes()
        {
            DiseaseScanNode.ClearScanNodesOnScreen();
            nodeInLOS = null;
            visibleNodes.Clear();
            RefreshScreen();
        }

        IEnumerator ScanGun()
        {
            animator.SetTrigger("scan");
            audioSource.Play();

            RaycastHit[] raycastHits = new RaycastHit[20];

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
                if (hit.transform != null) { logger.LogDebug(hit.transform.name); }
                if (hit.transform != null && hit.collider.TryGetComponentInChildren(out DiseaseScanNode? diseaseScanNode) && diseaseScanNode != null && diseaseScanNode.diseaseHost != null && diseaseScanNode.diseaseHost.player != playerHeldBy && diseaseScanNode.diseaseHost.hasDisease)
                {
                    logger.LogDebug("Setting scan node"); // TODO: Make sure this works on enemies and players
                    diseaseScanNode.SetScanNode(diseaseScanNode.diseaseHost.Diseases.Count > 1 ? $"{diseaseScanNode.diseaseHost.Diseases.Count} diseases detected" : "1 disease detected", string.Join("\n", diseaseScanNode.diseaseHost.Diseases.Select((d) => " - " + d.name)), (int)scanDistance * 2, requiresLineOfSight: false);
                    visibleNodes.Add(diseaseScanNode);
                }
            }

            scanRoutine = null;
        }
    }
}
