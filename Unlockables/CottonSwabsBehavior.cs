using Dawn;
using SnowyLib;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Unlockables
{
    internal class CottonSwabsBehavior : NetworkBehaviour
    {
        public static bool IsEnabled => LethalContent.Unlockables[LethalDiseasesKeys.CottonSwabs] != null;

        bool spawningCottonSwab;

        public void Interact()
        {
            if (spawningCottonSwab) { return; }
            spawningCottonSwab = true;
            SpawnCottonSwabServerRpc(localPlayer.actualClientId);
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        public void SpawnCottonSwabServerRpc(ulong clientGrabbingId)
        {
            if (!IsServer) { return; }

            GrabbableObject? cottonSwab = Utils.SpawnItem(LethalDiseasesKeys.CottonSwab, transform.position);
            if (cottonSwab != null)
            {
                IEnumerator sendSpawnOutputIngredient()
                {
                    yield return new WaitUntil(() => cottonSwab.NetworkObject != null && cottonSwab.NetworkObject.IsSpawned);
                    SpawnCottonSwabClientRpc(clientGrabbingId, cottonSwab.NetworkObject);
                }

                StartCoroutine(sendSpawnOutputIngredient());
            }
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void SpawnCottonSwabClientRpc(ulong clientGrabbingId, NetworkObjectReference netRef)
        {
            spawningCottonSwab = false;
            if (localPlayer.actualClientId != clientGrabbingId) { return; }
            if (!netRef.TryGet(out NetworkObject netObj)) { return; }
            if (!netObj.TryGetComponent(out GrabbableObject item)) { return; }

            localPlayer.GrabGrabbableObject(item);
        }
    }
}
