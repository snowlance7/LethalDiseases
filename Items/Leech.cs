using Dawn.Utils;
using SnowyLib;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items
{
    internal class Leech : AttachableObject
    {
        float leechHealthCooldown;
        public static float timeSinceLastSpawnAttempt;

        public static float SpawnChancePerSecond => PluginInstance.Config.Bind("Leech Options", "Leech | Spawn Chance Per Second", 0.1f, "Percent chance from 0-1 per second that a leech will spawn attached to a player while they are in water").Value;
        public static float RemoveDiseaseChance => PluginInstance.Config.Bind("Leech Options", "Leech | Remove Disease Chance", 0.25f, "Percent chance from 0-1 that a leech will remove a random disease from a player when used.").Value;
        public static BoundedRange LeechHealthInterval => PluginInstance.Config.Bind("Leech Options", "Leech | Leech Health Interval", new BoundedRange(10f, 360f), "Interval that the leech leeches health from the player its attached to").Value;
        public static int LeechDamage => PluginInstance.Config.Bind("Leech Options", "Leech | Leech Damage", 1, "The amount of damage the leech does when leeching health from the player its attached to").Value;
        public static int UseDamage => PluginInstance.Config.Bind("Leech Options", "Leech | Use Damage", 1, "The amount of damage the leech does when the player uses it to remove a disease from themselves").Value;

        [StaticInit]
        public static void Init()
        {
            _ = RemoveDiseaseChance;
        }

        [StaticUpdate]
        public static void StaticUpdate()
        {
            if (!IsServerOrHost) { return; }

            timeSinceLastSpawnAttempt += Time.deltaTime;

            if (timeSinceLastSpawnAttempt > 1f)
            {
                timeSinceLastSpawnAttempt = 0f;

                if (localPlayer.isUnderwater && UnityEngine.Random.Range(0f, 1f) > SpawnChancePerSecond)
                {
                    NetworkHandler.Instance.SpawnLeechRpc(localPlayer.actualClientId);
                }
            }
        }

        public override void Start()
        {
            base.Start();
            leechHealthCooldown = LeechHealthInterval.GetRandomInRange(Utils.randomLocal);
        }

        public override void Update()
        {
            base.Update();

            if (playerAttachedTo != null && playerAttachedTo == localPlayer && !StartOfRound.Instance.inShipPhase)
            {
                if (leechHealthCooldown > 0f)
                {
                    leechHealthCooldown -= Time.deltaTime;
                }

                if (leechHealthCooldown <= 0f)
                {
                    leechHealthCooldown = LeechHealthInterval.GetRandomInRange(Utils.randomLocal);
                    playerAttachedTo.DamagePlayer(LeechDamage);
                }
            }
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!buttonDown) { return; }

            localPlayer.inSpecialInteractAnimation = true;
            localPlayer.DamagePlayer(UseDamage);
            localPlayer.inSpecialInteractAnimation = false;

            TryRemoveRandomDisease();
            localPlayer.DespawnHeldObject();
        }

        public void TryRemoveRandomDisease()
        {
            if (UnityEngine.Random.Range(0f, 1f) > RemoveDiseaseChance) { return; }

            string? diseaseId = localPlayer.NetworkObject.GetDiseaseIds().GetRandom();
            if (diseaseId == null) { return; }
            NetworkHandler.Instance.RemoveDiseaseRpc(localPlayer.NetworkObject, diseaseId);
        }
    }
}
