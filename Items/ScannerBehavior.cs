using DigitalRuby.ThunderAndLightning;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items
{
    internal class ScannerBehavior : PhysicsProp // TODO
    {
        public Animator animator = null!;
        public AudioClip scanSFX = null!;
        public AudioSource audioSource = null!;
        public Light flashlightBulb = null!;
        public Light flashlightBulbGlow = null!;

        private Ray ray;
        private RaycastHit[] raycastEnemies = [];

        private int anomalyMask = 524296;
        private RaycastHit hit;
        private bool isScanning;

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!buttonDown) { return; }

            logger.LogDebug("ItemActivate");
            if (insertedBattery.empty) { return; }


            SwitchFlashlight(on: true);
            StartCoroutine(ScanGun());
        }

        private IEnumerator ScanGun()
        {
            animator.SetTrigger("scan");
            audioSource.PlayOneShot(scanSFX);
            //lightningScript = lightningObject.GetComponent<LightningSplineScript>();
            //lightningDest.SetParent(null);
            //lightningBend1.SetParent(null);
            //lightningBend2.SetParent(null);
            Debug.Log("Scan A");
            for (int i = 0; i < 12; i++)
            {
                if (base.IsOwner)
                {
                    Debug.Log("Scan B");
                    if (isPocketed)
                    {
                        yield break;
                    }
                    ray = new Ray(playerHeldBy.gameplayCamera.transform.position - playerHeldBy.gameplayCamera.transform.forward * 3f, playerHeldBy.gameplayCamera.transform.forward);
                    //Debug.DrawRay(playerHeldBy.gameplayCamera.transform.position - playerHeldBy.gameplayCamera.transform.forward * 3f, playerHeldBy.gameplayCamera.transform.forward * 6f, Color.red, 5f);
                    int num = Physics.SphereCastNonAlloc(ray, 5f, raycastEnemies, 5f, anomalyMask, QueryTriggerInteraction.Collide);
                    raycastEnemies = raycastEnemies.OrderBy((RaycastHit x) => x.distance).ToArray();
                    for (int j = 0; j < num; j++)
                    {
                        if (j >= raycastEnemies.Length)
                        {
                            continue;
                        }
                        hit = raycastEnemies[j];
                        if (!(hit.transform == null) && hit.transform.gameObject.TryGetComponent<IShockableWithGun>(out var component) && component.CanBeShocked())
                        {
                            Vector3 shockablePosition = component.GetShockablePosition();
                            //Debug.Log("Got shockable transform name : " + component.GetShockableTransform().gameObject.name);
                            /*if (GunMeetsConditionsToShock(playerHeldBy, shockablePosition, 60f))
                            {
                                gunAudio.Stop();
                                BeginShockingAnomalyOnClient(component);
                                yield break;
                            }*/
                        }
                    }
                }
                yield return new WaitForSeconds(0.125f);
            }
            Debug.Log("Zap gun light off!!!");
            SwitchFlashlight(on: false);
            isScanning = false;
        }

        public void SwitchFlashlight(bool on)
        {
            flashlightBulb.enabled = on;
            flashlightBulbGlow.enabled = on;
        }
    }
}
