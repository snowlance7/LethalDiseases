using LethalDiseases.Unlockables;
using SnowyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items
{
    internal class BiosamplerBehavior : PhysicsProp, IChemistryIngredient
    {
        public SkinnedMeshRenderer fluidRender = null!;

        public List<string> filledDiseases = [];

        public bool isFilled => filledDiseases.Count >= maxDiseases;

        public bool freezingLocalPlayer;

        Coroutine? routine;

        const float fillTime = 1.5f;

        int maxDiseases = 4;

        ChemistryIngredient IChemistryIngredient.GetInputIngredient()
        {
            return new ChemistryIngredient(itemProperties, new ChemistryLiquidAppearance(fluidRender.material.color, fluidRender.material.GetColor("_EmissionColor"), fluidRender.material.GetFloat("_EmissionIntensity")), string.Join("^", filledDiseases));
        }

        void IChemistryIngredient.OnOutputIngredient(string specialInstructions)
        {
            return;
        }

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(-0.25f, 0.14f, -0.05f);
            itemProperties.rotationOffset = new Vector3(90, 80, 0);
            itemProperties.floorYOffset = 1;
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

        public override void ItemInteractLeftRight(bool right)
        {
            base.ItemInteractLeftRight(right);
            if (right || filledDiseases.Count == 0) { return; }

            EmptyRpc();
        }

        public float GetFillAmount(int diseaseCount)
        {
            return 100 - ((diseaseCount / maxDiseases) * 100);
        }

        public void DoStabAnimation()
        {
            IEnumerator doStabAnimation()
            {
                yield return null;

                playerHeldBy.playerBodyAnimator.SetTrigger("UseHeldItem1"); // Stab animation

                yield return new WaitForSeconds(11f / 60f);

                float closestDistance = 0f;
                DiseaseHost? host = DiseaseHost.Instances.Where(x => x.hasActor && x.hasDisease && x.player != playerHeldBy).GetClosestToPosition(transform.position, (x) => x.transform.position, out closestDistance, fastDistanceCheck: true);

                if (host == null || closestDistance > 1.5f)
                {
                    playerHeldBy.activatingItem = false;
                    routine = null;
                    yield break;
                }

                freezingLocalPlayer = true;
                playerHeldBy.FreezePlayer(true);
                playerHeldBy.playerBodyAnimator.speed = 0f;
                fluidRender.enabled = true;

                float currentFillAmount = GetFillAmount(filledDiseases.Count);
                List<string> addingDiseases = host.DiseaseIds.Take(maxDiseases - filledDiseases.Count).ToList();
                float newFillAmount = GetFillAmount((addingDiseases.Count + filledDiseases.Count));

                float elapsedTime = 0f;

                while (elapsedTime < fillTime)
                {
                    yield return null;
                    elapsedTime += Time.deltaTime;
                    fluidRender.SetBlendShapeWeight(0, Mathf.Lerp(currentFillAmount, newFillAmount, elapsedTime / fillTime));
                }

                fluidRender.SetBlendShapeWeight(0, 0);
                playerHeldBy.FreezePlayer(false);
                freezingLocalPlayer = false;
                playerHeldBy.playerBodyAnimator.speed = 1f;
                playerHeldBy.activatingItem = false;

                FillRpc(string.Join("^", host.DiseaseIds));
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

                float currentFillAmount = GetFillAmount(filledDiseases.Count);
                float elapsedTime = 0f;

                while (elapsedTime < fillTime)
                {
                    yield return null;
                    elapsedTime += Time.deltaTime;
                    fluidRender.SetBlendShapeWeight(0, Mathf.Lerp(currentFillAmount, 100, elapsedTime / fillTime));
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
            DoEmptyAnimation();
        }

        [Rpc(SendTo.Everyone)]
        public void FillRpc(string diseases)
        {
            filledDiseases.AddRange(diseases.Split("^"));
            float currentFillAmount = GetFillAmount(filledDiseases.Count);
            fluidRender.SetBlendShapeWeight(0, currentFillAmount);
        }
    }
}