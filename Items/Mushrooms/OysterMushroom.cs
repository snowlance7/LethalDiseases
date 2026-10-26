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
        }, () =>
        {
            Vector3 spawnPosition = Utils.currentLevelTrees.GetRandom().transform.position;
            float YOffset = UnityEngine.Random.Range(0f, 1f);
            spawnPosition = new Vector3(spawnPosition.x, spawnPosition.y + YOffset, spawnPosition.z);
            Vector3 randomDirection = UnityEngine.Random.onUnitSphere * 0.1f;
            randomDirection = new Vector3(randomDirection.x, 0, randomDirection.z);
            spawnPosition += randomDirection;
            return new SpawnDefinition(spawnPosition, Quaternion.FromToRotation(Vector3.forward, randomDirection), postSpawnAction: (netObj) =>
            {
                var mushroom = netObj.GetComponent<OysterMushroom>();
                mushroom.RotateMushroomRpc(UnityEngine.Random.Range(0f, 360f));
            });
        });
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