using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases
{
    public class DiseaseScanNode : MonoBehaviour
    {
        public static HashSet<DiseaseScanNode> Instances { get; private set; } = new HashSet<DiseaseScanNode>();

        public ScanNodeProperties node = null!; // TODO: Hook up in unity
        public Collider collider = null!;

        [HideInInspector] public DiseaseHost diseaseHost = null!;
        [HideInInspector] public Collider parentCollider = null!;

        public int elementIndex;

        public bool isOnScreen => HUDManager.Instance.nodesOnScreen.Contains(node);

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

        public void SetScanNode(int maxRange) // TODO: Make it so when you look at a scan node when holding the analyzer it shows more info about that node on the analyzer screen
        {
            if (diseaseHost == null) { logger.LogError($"Couldnt find disease host for disease scan node {name}"); return; }
            logger.LogDebug("Setting disease scan node");
            collider.enabled = true;

            node.maxRange = maxRange;
            node.headerText = diseaseHost.Diseases.Count > 1 ? $"{diseaseHost.Diseases.Count} diseases detected" : "1 disease detected";
            node.subText = string.Join("\n", diseaseHost.Diseases.Select((d) => " - " + d.name));
            node.requiresLineOfSight = false;

            HUDManager h = HUDManager.Instance;

            if (!h.nodesOnScreen.Contains(node))
            {
                logger.LogDebug("Adding node to nodesOnScreen");
                h.nodesOnScreen.Add(node);
            }

            if (h.scanNodes.ContainsValue(node)) { logger.LogError($"scanNodes already contains node for {diseaseHost.GetInfectedName()}"); return; }

            logger.LogDebug("Adding to scanNode");
            for (int i = 0; i < h.scanElements.Length; i++)
            {
                if (h.scanNodes.TryAdd(h.scanElements[i], node))
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
                node.collider.enabled = enable;
            }
        }

        public static void ClearScanNodesOnScreen()
        {
            logger.LogDebug("Clearing scan nodes");
            foreach (var node in Instances)
            {
                if (!HUDManager.Instance.nodesOnScreen.Contains(node.node)) { continue; }
                HUDManager.Instance.NodeIsNotVisible(node.node, node.elementIndex);
                node.collider.enabled = false;
            }
        }
    }
}
