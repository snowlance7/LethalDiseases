using Dawn;
using SnowyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items.Mushrooms;

internal class JellyMushroom : PhysicsProp // TODO: Register chemistry recipe for making tonic! // TODO: Make them a bit bigger and spawn around underwater trees
{
    public static int roomMask = LayerMask.GetMask("Room");
    public static List<GameObject> availableTrees = new List<GameObject>();

    [StaticInit]
    public static void Init()
    {
        if (!IsServerOrHost) { return; }
        PostGenSpawnableObject.Register(LethalContent.Items[LethalDiseasesKeys.JellyMushroom].Item.spawnPrefab, () => RoundManager.Instance.currentLevel.GetDawnInfo().TypedKey == LethalContent.Moons[MoonKeys.March].TypedKey ? UnityEngine.Random.Range(10, 20) : 0, () =>
        {
            if (availableTrees.Count() == 0)
            {
                availableTrees = GameObject.FindGameObjectsWithTag("Tree").Where(x => x.name.StartsWith("WychElmA")).ToList(); // TODO: This doesnt work because underwater trees cant be walked under
                logger.LogDebug($"Got {availableTrees.Count()} trees");
            }

            var tree = availableTrees.GetRandom();
            Vector3 castPoint = Utils.GetRandomPositionInCircle(tree.transform.position, 1f);
            Physics.Raycast(castPoint, Vector3.down, out RaycastHit hit, Mathf.Infinity, roomMask);
            return new SpawnDefinition(hit.point, Quaternion.identity, destroyWithScene: true);
        }, () => availableTrees.Clear());
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (!buttonDown) { return; }
        Utils.InvokeAfterDelay(() => Utils.overridingLocalPlayerDeath = false, 2f);
        Utils.overridingLocalPlayerDeath = true;
    }
}