using SnowyLib;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items
{
    internal class BiosamplerBehavior : PhysicsProp
    {
        public SkinnedMeshRenderer fluidRender = null!;

        public string[] filledDiseases = [];

        public bool isFilled;

        public bool freezingLocalPlayer;

        Coroutine? routine;

        bool stabbingSelf;

        const float fillTime = 1.5f;

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(-0.25f, 0.14f, -0.05f);
            itemProperties.rotationOffset = new Vector3(90, 80, 0);
            itemProperties.floorYOffset = 0;
            itemProperties.grabAnim = "HoldKnife";
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            if (freezingLocalPlayer)
                localPlayer.FreezePlayer(false);
        }

        public override void EquipItem()
        {
            base.EquipItem();
            playerHeldBy.equippedUsableItemQE = true;
            fluidRender.enabled = isFilled;
        }

        public override void DiscardItem()
        {
            playerHeldBy.equippedUsableItemQE = false;
            fluidRender.enabled = isFilled;
            base.DiscardItem();
        }

        public override void PocketItem()
        {
            playerHeldBy.equippedUsableItemQE = false;
            fluidRender.enabled = false;
            base.PocketItem();
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!buttonDown || isFilled || routine != null) { return; }

            DoStabAnimation();
        }

        public override void ItemInteractLeftRight(bool right) // TODO: Fix this and dont do switch mode functionality
        {
            base.ItemInteractLeftRight(right);
            if (right || !isFilled) { return; }

            if (right) // E
            {
                stabbingSelf = !stabbingSelf;
            }
            else if (isFilled) // Q
            {
                EmptyRpc();
            }
        }

        public void DoTestStabAnimation()
        {
            IEnumerator doTestStabAnimation()
            {
                yield return null;

                playerHeldBy.playerBodyAnimator.SetTrigger("UseHeldItem1"); // Stab animation

                yield return new WaitForSeconds(11f / 60f);

                freezingLocalPlayer = true;
                playerHeldBy.FreezePlayer(true);
                playerHeldBy.playerBodyAnimator.speed = 0f;
                fluidRender.enabled = true;

                float elapsedTime = 0f;

                while (elapsedTime < fillTime)
                {
                    yield return null;
                    elapsedTime += Time.deltaTime;
                    fluidRender.SetBlendShapeWeight(0, Mathf.Lerp(100, 0, elapsedTime / fillTime));
                }

                fluidRender.SetBlendShapeWeight(0, 0);
                playerHeldBy.FreezePlayer(false);
                freezingLocalPlayer = false;
                playerHeldBy.playerBodyAnimator.speed = 1f;
                playerHeldBy.activatingItem = false;

                isFilled = true;
                routine = null;
            }

            playerHeldBy.activatingItem = true;
            routine = StartCoroutine(doTestStabAnimation());
        }

        public void DoStabAnimation()
        {
            IEnumerator doStabAnimation()
            {
                yield return null;

                playerHeldBy.playerBodyAnimator.SetTrigger("UseHeldItem1"); // Stab animation

                yield return new WaitForSeconds(11f / 60f);

                float closestDistance = 0f;
                DiseaseHost? host = stabbingSelf ? playerHeldBy.NetworkObject.GetHost() : DiseaseHost.Instances.Where(x => x.hasActor && x.hasDisease && x.player != playerHeldBy).GetClosestToPosition(transform.position, (x) => x.transform.position, out closestDistance, fastDistanceCheck: true);

                if (host == null || (!stabbingSelf && closestDistance > 1.5f))
                {
                    playerHeldBy.activatingItem = false;
                    routine = null;
                    yield break;
                }

                freezingLocalPlayer = true;
                playerHeldBy.FreezePlayer(true);
                playerHeldBy.playerBodyAnimator.speed = 0f;
                fluidRender.enabled = true;

                float elapsedTime = 0f;

                while (elapsedTime < fillTime)
                {
                    yield return null;
                    elapsedTime += Time.deltaTime;
                    fluidRender.SetBlendShapeWeight(0, Mathf.Lerp(100, 0, elapsedTime / fillTime));
                }

                fluidRender.SetBlendShapeWeight(0, 0);
                playerHeldBy.FreezePlayer(false);
                freezingLocalPlayer = false;
                playerHeldBy.playerBodyAnimator.speed = 1f;
                playerHeldBy.activatingItem = false;

                FillRpc(string.Join("|", host.DiseaseIds));
                routine = null;
            }

            playerHeldBy.activatingItem = true;
            routine = StartCoroutine(doStabAnimation());
        }

        public void DoEmptyAnimation()
        {
            IEnumerator doEmptyAnimation()
            {
                yield return null;

                float elapsedTime = 0f;

                while (elapsedTime < fillTime)
                {
                    yield return null;
                    elapsedTime += Time.deltaTime;
                    fluidRender.SetBlendShapeWeight(0, Mathf.Lerp(0, 100, elapsedTime / fillTime));
                }

                fluidRender.SetBlendShapeWeight(0, 100);

                fluidRender.enabled = false;
                routine = null;
            }

            routine = StartCoroutine(doEmptyAnimation());
        }

        [Rpc(SendTo.Everyone)]
        public void EmptyRpc()
        {
            filledDiseases = [];
            isFilled = false;
            DoEmptyAnimation();
        }

        [Rpc(SendTo.Everyone)]
        public void FillRpc(string diseases)
        {
            filledDiseases = diseases.Split("|");
            isFilled = true;
        }
    }
}