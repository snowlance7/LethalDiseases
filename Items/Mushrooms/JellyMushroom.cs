using Dawn;
using SnowyCraftingCore;
using SnowyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items.Mushrooms;

internal class JellyMushroom : Mushroom // Jelly_Mushroom (March water) // TODO: Register chemistry recipe for making tonic! // TODO: Make them a bit bigger and spawn around underwater trees
{
    [SerializeField] Color color;

    public static int roomMask = LayerMask.GetMask("Room");
    public static List<QuicksandTrigger> availableTriggers = new List<QuicksandTrigger>();

    [StaticInit]
    public static void Init()
    {
        if (!IsServerOrHost) { return; }
        PostGenSpawnableObject.Register(LethalContent.Items[LethalDiseasesKeys.JellyMushroom].Item.spawnPrefab, () => RoundManager.Instance.currentLevel.GetDawnInfo().TypedKey == LethalContent.Moons[MoonKeys.March].TypedKey ? UnityEngine.Random.Range(15, 30) : 0, () =>
        {
            if (availableTriggers.Count() == 0)
            {
                availableTriggers = GameObject.FindObjectsOfType<QuicksandTrigger>().Where(x => x.isWater || x.isInsideWater).ToList();
                logger.LogDebug($"Got {availableTriggers.Count()} triggers");
            }

            var trigger = availableTriggers.GetRandom();
            var triggerCollider = trigger.gameObject.GetComponent<Collider>();
            Vector3 castPoint = triggerCollider.GetRandomPointInsideBounds();
            Physics.Raycast(castPoint, Vector3.down, out RaycastHit hit, Mathf.Infinity, roomMask);
            return new SpawnDefinition(hit.point, Quaternion.identity, destroyWithScene: true);
        }, () => availableTriggers.Clear());
    }

    public override ChemistryIngredient? GetIngredient()
    {
        return new ChemistryIngredient(LethalDiseasesKeys.Mushroom, color, "TransmissionType"); // TODO: Make tonicaction and tonictype random at the start of each game
    }

    public override void OnEatMushroom()
    {
        Utils.InvokeAfterDelay(() => Utils.overridingLocalPlayerDeath = false, 2f);
        Utils.overridingLocalPlayerDeath = true;
    }
}