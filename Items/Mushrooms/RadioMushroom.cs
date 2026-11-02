using SnowyLib;
using System.Linq;
using UnityEngine;
using static LethalDiseases.Plugin;

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
        Disease disease = Disease.CreateRandomDiseaseWithSymptoms("Ionizing", "Glowing", "Cramping", "Vomiting", "Itching", "Nausea");
        localPlayer.InfectSynced(disease);
    }
}