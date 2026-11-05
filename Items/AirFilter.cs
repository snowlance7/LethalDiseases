using GameNetcodeStuff;
using SnowyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items
{
    internal class AirFilter : PhysicsProp // UPDATE: Make an advanced air filter that also filters cadavar spores
    {
        public static float Usage => PluginInstance.Config.Bind("Air Filter Options", "Air Filter | Usage", 0.05f, "How much of the air filter get used when filtering airborne diseases").Value;
        public static float Multiplier => PluginInstance.Config.Bind("Air Filter Options", "Air Filter | Multiplier", 0.5f, "When a player would be infected by an airborne disease, its infection chance is multiplied by this number if the air filter is installed").Value;

        AudioSource audioSource = null!;
        PlayerControllerB previousPlayerHeldBy = null!;
        Coroutine? routine;

        [StaticInit]
        public static void Init()
        {
            _ = Usage;
            _ = Multiplier;
        }

        public void Awake()
        {
            itemProperties.syncUseFunction = true;
            itemProperties.positionOffset = new Vector3(0, 0.07f, -0.04f);
            itemProperties.rotationOffset = new Vector3(0, 0, 0);
            audioSource = GetComponent<AudioSource>();
        }

        public override void Update()
        {
            base.Update();

            if (playerHeldBy != null)
                previousPlayerHeldBy = playerHeldBy;
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);

            previousPlayerHeldBy.activatingItem = buttonDown;
            previousPlayerHeldBy.playerBodyAnimator.SetBool("useTZPItem", buttonDown);

            if (buttonDown) 
                audioSource.Play();
            else 
                audioSource.Stop();

            if (!base.IsOwner) { return; }

            if (buttonDown)
            {
                IEnumerator itemActivate()
                {
                    yield return new WaitForSeconds(2f);
                    ReplaceAirFilter();
                    localPlayer.DespawnHeldObject();
                }

                routine = StartCoroutine(itemActivate());
            }
            else
            {
                StopCoroutine(routine);
            }
        }

        public static void ReplaceAirFilter()
        {
            var cadaverGrowth = GameObject.FindObjectOfType<CadaverGrowthAI>();
            if (cadaverGrowth != null)
            {
                cadaverGrowth.localPlayerImmunityTimer = 0f;
            }

            localPlayer.GetHost().ReplaceAirFilterSynced();
        }
    }
}