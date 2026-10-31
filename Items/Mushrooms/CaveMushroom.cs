using Dawn;
using SnowyLib;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items.Mushrooms;

internal class CaveMushroom : Mushroom // Cave Mushroom (mineshaft interior)
{
    [StaticInit]
    public static void Init()
    {
        if (!IsServerOrHost) { return; }
        PostGenSpawnableObject.Register(LethalDiseasesKeys.CaveMushroom, () =>
        {
            if (RoundManager.Instance.dungeonGenerator.Generator.DungeonFlow.GetDawnInfo().TypedKey == DungeonKeys.MineshaftFlow) { return UnityEngine.Random.Range(15, 30); }
            return 0;
        }, () =>
        {
            var castPoint = Utils.insideAINodes.GetRandom().transform.position;
            if (!Physics.Raycast(castPoint, UnityEngine.Random.onUnitSphere, out RaycastHit hit, Mathf.Infinity, PluginInstance.roomMask)) { return null; }
            return new SpawnDefinition(hit.point, Quaternion.FromToRotation(Vector3.up, hit.normal), destroyWithScene: true);
        });
    }

    public override void OnEatMushroom()
    {
        NightVision.Enable(true);
        Utils.InvokeAfterDelay(() => NightVision.Enable(false), 30f);
    }
}