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

        public void SetScanNode(int maxRange) // TODO: Make it so when you look at a scan node when holding the analyzer it shows more info about that node on the analyzer screen
        {
            if (diseaseHost.diseaseScanNode == null) { return; }
            logger.LogDebug("Setting disease scan node");

            ScanNodeProperties node = diseaseHost.diseaseScanNode.scanNode;
            node.maxRange = maxRange;
            node.headerText = diseaseHost.Diseases.Count > 1 ? $"{diseaseHost.Diseases.Count} diseases detected" : "1 disease detected";
            node.subText = string.Join("\n", diseaseHost.Diseases.Select((d) => d.name));

            HUDManager h = HUDManager.Instance;

            if (!h.nodesOnScreen.Contains(node))
            {
                h.nodesOnScreen.Add(node);
            }

            if (h.scanNodes.ContainsValue(node)) { return; }

            for (int i = 0; i < h.scanElements.Length; i++)
            {
                if (h.scanNodes.TryAdd(h.scanElements[i], node))
                {
                    diseaseHost.diseaseScanNode.elementIndex = i;
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
            foreach (var node in Instances)
            {
                if (!HUDManager.Instance.nodesOnScreen.Contains(node.scanNode)) { continue; }
                HUDManager.Instance.NodeIsNotVisible(node.scanNode, node.elementIndex);
            }
        }
    }
}
