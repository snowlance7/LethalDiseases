using SnowyLib;
using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items.Mushrooms;

internal class OysterMushroom : Mushroom
{
    [SerializeField] Transform rotationPoint = null!;

    [StaticInit]
    public static void Init()
    {
        if (!IsServerOrHost) { return; }
        PostGenSpawnableObject.Register(LethalDiseasesKeys.OysterMushroom, () =>
        {
            return (int)(Utils.currentLevelTrees.Count() * UnityEngine.Random.Range(0.1f, 0.25f));
        }, () => GetRandomOutsideSpawn());
    }

    public override void OnEatMushroom()
    {
        throw new NotImplementedException();
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void RotateMushroomRpc(float amount)
    {
        Vector3 rotation = rotationPoint.localEulerAngles;
        rotation.y = amount;
        rotationPoint.localEulerAngles = rotation;
    }
}