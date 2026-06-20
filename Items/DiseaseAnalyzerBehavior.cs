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
        public GameObject screenObj = null!;
        public GameObject scanNodePrefab = null!;

        List<DiseaseScanNode> diseaseScanNodes = new List<DiseaseScanNode>();

        PlayerControllerB previousPlayerHeldBy = null!;

        Ray ray;
        RaycastHit[] raycastHits = [];
        int anomalyMask = 524296;

        Coroutine? scanRoutine;

        const float scanDistance = 5f;
        const float scanForwardOffset = 3f;

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(-0.1f, 0.235f, 0.035f);
            itemProperties.rotationOffset = new Vector3(0, 10, -90);
            itemProperties.floorYOffset = 0;
            itemProperties.syncGrabFunction = true;
            itemProperties.syncDiscardFunction = true;
            itemProperties.syncUseFunction = true;
            itemProperties.grabAnim = "HoldPatcherTool";
        }

        public override void OnDestroy()
        {
            ClearScanNodes();
            base.OnDestroy();
        }

        public override void PocketItem()
        {
            base.PocketItem();
            screenObj.SetActive(false);
            if (previousPlayerHeldBy != null && previousPlayerHeldBy == localPlayer)
            {
                previousPlayerHeldBy.equippedUsableItemQE = false;
                ClearScanNodes();
            }
        }

        public override void DiscardItem()
        {
            base.DiscardItem();
            screenObj.SetActive(false);
            if (previousPlayerHeldBy != null && previousPlayerHeldBy == localPlayer)
            {
                previousPlayerHeldBy.equippedUsableItemQE = false;
                ClearScanNodes();
            }
        }

        public override void EquipItem()
        {
            base.EquipItem();
            screenObj.SetActive(true);
            previousPlayerHeldBy = playerHeldBy;
            if (previousPlayerHeldBy != null && previousPlayerHeldBy == localPlayer)
                previousPlayerHeldBy.equippedUsableItemQE = true;
        }

        public override void ItemInteractLeftRight(bool right)
        {
            base.ItemInteractLeftRight(right);

            if (right) // E: Clear disease scan nodes TODO
            {
                logger.LogDebug("Clearing disease scan nodes");

            }
            else // Q: Scan self TODO
            {
                logger.LogDebug("Scanning self");

            }
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

            for (int i = 0; i < 12; i++)
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
                        if (hit.transform != null && hit.transform.gameObject.TryGetComponent(out DiseaseHost diseaseHost) && diseaseHost.hasDisease)
                        {
                            CreateScanNode(diseaseHost);
                        }
                    }
                }
                yield return new WaitForSeconds(0.125f);
            }

            scanRoutine = null;
        }

        void CreateScanNode(DiseaseHost diseaseHost) // TODO: Make it so each scan node just shows "Infected", "x diseases detected" and when you look at it when holding the scanner it shows more info about that node on the analyzer screen
        {
            Transform parentTo;
            if (diseaseHost.player != null)
            {
                parentTo = diseaseHost.player.bodyParts[5];
            }
            else
            {
                parentTo = diseaseHost.gameObject.TryGetComponentInChildren(out ScanNodeProperties? hostNode) && hostNode != null ? hostNode.transform : diseaseHost.transform;
            }

            GameObject scanNodeObj = Instantiate(scanNodePrefab, parentTo);

            ScanNodeProperties scanNode = scanNodeObj.GetComponent<ScanNodeProperties>();
            scanNode.maxRange = (int)scanDistance * 2;
            scanNode.minRange = 1;
            scanNode.requiresLineOfSight = true;
            scanNode.scrapValue = 0;
            scanNode.creatureScanID = -1;
            scanNode.nodeType = 1;
            scanNode.headerText = "Infected";
            scanNode.subText = diseaseHost.Diseases.Count > 1 ? $"{diseaseHost.Diseases.Count} diseases detected" : "1 disease detected";

            HUDManager.Instance.nodesOnScreen.Add(scanNode);
            HUDManager.Instance.AssignNodeToUIElement(scanNode);
            diseaseScanNodes.Add(new DiseaseScanNode(diseaseHost, scanNode));
        }

        void ClearScanNodes() // TODO: Test this and make sure it clears up scan nodes correctly and doesnt give any errors
        {
            if (diseaseScanNodes.Count <= 0) { return; }

            foreach (var node in diseaseScanNodes)
            {
                if (node.scanNode == null || node.scanNode.gameObject == null) { continue; }

                //HUDManager.Instance.nodesOnScreen.Remove(node.scanNode);
                //HUDManager.Instance.scanNodes.valu

                Destroy(node.scanNode.gameObject);
            }

            diseaseScanNodes.Clear();
        }
    }

    public class DiseaseScanNode(DiseaseHost diseaseHost, ScanNodeProperties scanNode)
    {
        public DiseaseHost diseaseHost = diseaseHost;
        public ScanNodeProperties scanNode = scanNode;
    }
}
