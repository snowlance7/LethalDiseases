using Dawn;
using JetBrains.Annotations;
using SnowyLib;
using System.Linq;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items.Mushrooms;

internal class JellyMushroom : PhysicsProp // TODO: Register chemistry recipe for making tonic!
{
    [StaticInit]
    public static void Init()
    {
        if (!IsServerOrHost) { return; }
        PostGenSpawnableObject.Register(new PostGenSpawnableObject(LethalContent.Items[LethalDiseasesKeys.JellyMushroom].Item.spawnPrefab, () => RoundManager.Instance.currentLevel.GetDawnInfo().TypedKey == LethalContent.Moons[MoonKeys.March].TypedKey ? UnityEngine.Random.Range(5, 16) : 0, () =>
        {
            while (true)
            {
                var trigger = GameObject.FindObjectsOfType<QuicksandTrigger>().Where(x => x.isInsideWater || x.isWater).GetRandom()!;
                var triggerCollider = trigger.gameObject.GetComponent<Collider>();
                var castPoint = triggerCollider.GetRandomPointInsideBounds();

                if (Physics.Raycast(castPoint, Vector3.down, out RaycastHit hit, 10f, LayerMask.GetMask("Room")))
                {
                    return new SpawnDefinition(hit.point, Quaternion.FromToRotation(Vector3.up, hit.normal), destroyWithScene: true); // TODO: Test this
                }
            }
        }));
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (!buttonDown) { return; }
        Utils.InvokeAfterDelay(() => Utils.overridingLocalPlayerDeath = false, 2f);
        Utils.overridingLocalPlayerDeath = true;
    }
}