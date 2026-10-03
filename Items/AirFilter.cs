using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static LethalDiseases.Plugin;
using SnowyLib;

namespace LethalDiseases.Items
{
    internal class AirFilter : PhysicsProp
    {
        AudioSource audioSource = null!;

        public static float Usage => PluginInstance.Config.Bind("Air Filter Options", "Air Filter | Usage", 0.05f, "How much of the air filter get used when filtering airborne diseases").Value;
        public static float Multiplier => PluginInstance.Config.Bind("Air Filter Options", "Air Filter | Multiplier", 0.5f, "When a player would be infected by an airborne disease, its infection chance is multiplied by this number if the air filter is installed").Value;

        [StaticInit]
        public static void Init()
        {
            _ = Usage;
            _ = Multiplier;
        }

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0, 0.07f, -0.04f);
            itemProperties.rotationOffset = new Vector3(0, 0, 0);
            audioSource = GetComponent<AudioSource>();
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!buttonDown) { return; }

            audioSource.Play();

            var cadaverGrowth = GameObject.FindObjectOfType<CadaverGrowthAI>();
            if (cadaverGrowth != null)
            {
                cadaverGrowth.localPlayerImmunityTimer = 0f;
            }

            localPlayer.GetHost().ReplaceAirFilterRpc();
            localPlayer.DespawnHeldObject();
        }
    }
}