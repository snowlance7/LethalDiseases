using SnowyCraftingCore;
using SnowyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static LethalDiseases.Plugin;
using UnityEngine;

namespace LethalDiseases.Items.Mushrooms;

internal class RadioMushroom : Mushroom // Radio Mushroom (near apparatus)
{
    private static LungProp? apparatus;

    [StaticInit]
    public static void Init()
    {
        PostGenSpawnableObject.Register(LethalDiseasesKeys.RadioMushroom, () =>
        {
            apparatus = GameObject.FindObjectsOfType<LungProp>().FirstOrDefault(x => x.isLungDocked);
            if (apparatus == null) { return 0; }
            return UnityEngine.Random.Range(0, 25);
        }, () =>
        {
            if (apparatus == null) { return null; }
            if (!Physics.Raycast(apparatus.transform.position, UnityEngine.Random.onUnitSphere, out RaycastHit hit, 3f, PluginInstance.roomMask)) { return null; }
            return new SpawnDefinition(hit.point, Quaternion.FromToRotation(Vector3.up, hit.normal));
        });
    }

    public override void OnEatMushroom()
    {
        throw new NotImplementedException();
    }
}