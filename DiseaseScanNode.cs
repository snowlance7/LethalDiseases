using System.Collections.Generic;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases
{
    public class DiseaseScanNode : MonoBehaviour
    {
        public static HashSet<DiseaseScanNode> Instances { get; private set; } = new HashSet<DiseaseScanNode>();

        public ScanNodeProperties scanNode = null!; // TODO: Hook up in unity
        public Collider collider = null!;

        [HideInInspector] public DiseaseHost diseaseHost = null!;
        [HideInInspector] public Collider parentCollider = null!;

        public int elementIndex;

        public bool isOnScreen => HUDManager.Instance.nodesOnScreen.Contains(scanNode);

        void Start()
        {
            Instances.Add(this);
        }

        void OnDestroy()
        {
            Instances.Remove(this);
        }

        void Update()
        {
            if (parentCollider != null)
                transform.position = parentCollider.bounds.center;
        }

        public void SetScanNode(string header, string sub, int maxRange, bool requiresLineOfSight) // TODO: Make it so when you look at a scan node when holding the analyzer it shows more info about that node on the analyzer screen
        {
            logger.LogDebug("Setting disease scan node");
            if (diseaseHost == null) { logger.LogError($"Couldnt find disease host for disease scan node {name}"); return; }
            collider.enabled = true;

            scanNode.headerText = header;
            scanNode.subText = sub;
            scanNode.maxRange = maxRange;
            scanNode.requiresLineOfSight = requiresLineOfSight;

            HUDManager h = HUDManager.Instance;

            if (!h.nodesOnScreen.Contains(scanNode))
            {
                logger.LogDebug("Adding node to nodesOnScreen");
                h.nodesOnScreen.Add(scanNode);
            }

            if (h.scanNodes.ContainsValue(scanNode)) { logger.LogError($"scanNodes already contains node for {diseaseHost.GetInfectedName()}"); return; }

            logger.LogDebug("Adding to scanNode");
            for (int i = 0; i < h.scanElements.Length; i++)
            {
                if (h.scanNodes.TryAdd(h.scanElements[i], scanNode))
                {
                    logger.LogDebug($"Setting scan node element index to {i}");
                    elementIndex = i;
                    break;
                }
            }
        }

        public static void EnableColliders(bool enable)
        {
            foreach (var node in Instances)
            {
                if (node.isOnScreen) { continue; }
                node.collider.enabled = enable;
            }
        }

        public static void ClearScanNodesOnScreen()
        {
            logger.LogDebug("Clearing disease scan nodes");
            foreach (var node in Instances)
            {
                if (!HUDManager.Instance.nodesOnScreen.Contains(node.scanNode)) { continue; }
                HUDManager.Instance.NodeIsNotVisible(node.scanNode, node.elementIndex);
                node.collider.enabled = false;
            }
        }
    }
}
