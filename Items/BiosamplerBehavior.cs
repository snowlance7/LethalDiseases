using BepInEx;
using Dawn;
using LethalDiseases.Unlockables;
using SnowyCraftingCore;
using SnowyLib;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items
{
    internal class BiosamplerBehavior : PhysicsProp, IDistillableIngredient, IChemistryOutputContainer
    {
        public SkinnedMeshRenderer fluidRenderer = null!;

        ScanNodeProperties scanNode = null!;

        public string storedDisease = "";

        public bool isFilled => !storedDisease.IsNullOrWhiteSpace();

        public bool freezingLocalPlayer;

        Coroutine? routine;

        const float fillTime = 1.5f;

        public void SetFluidColor(ChemistryLiquidAppearance color)
        {
            fluidRenderer.enabled = true;
            fluidRenderer.material.color = color.liquidColor;
            fluidRenderer.material.SetColor("_EmissionColor", color.liquidColor);
            fluidRenderer.material.SetFloat("_EmissionIntensity", color.emissionIntensity);
        }

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(-0.25f, 0.14f, -0.05f);
            itemProperties.rotationOffset = new Vector3(90, 80, 0);
            itemProperties.restingRotation = new Vector3(90, 90, 90);
            itemProperties.floorYOffset = 0;
            itemProperties.grabAnim = "HoldKnife";
            scanNode = gameObject.GetComponentInChildren<ScanNodeProperties>();
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
            fluidRenderer.enabled = isFilled;
        }

        public override void DiscardItem()
        {
            playerHeldBy.equippedUsableItemQE = false;
            fluidRenderer.enabled = isFilled;
            base.DiscardItem();
        }

        public override void PocketItem()
        {
            playerHeldBy.equippedUsableItemQE = false;
            fluidRenderer.enabled = false;
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
            if (right || !isFilled) { return; }

            EmptyRpc();
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

                Disease disease = Disease.MergeDiseases(host.Diseases);
                SetFluidColor(disease.GetChemistryLiquidAppearance());

                freezingLocalPlayer = true;
                playerHeldBy.FreezePlayer(true);
                playerHeldBy.playerBodyAnimator.speed = 0f;
                fluidRenderer.enabled = true;

                float elapsedTime = 0f;

                while (elapsedTime < fillTime)
                {
                    yield return null;
                    elapsedTime += Time.deltaTime;
                    fluidRenderer.SetBlendShapeWeight(0, Mathf.Lerp(100, 0, elapsedTime / fillTime));
                }

                fluidRenderer.SetBlendShapeWeight(0, 0);
                playerHeldBy.FreezePlayer(false);
                freezingLocalPlayer = false;
                playerHeldBy.playerBodyAnimator.speed = 1f;
                playerHeldBy.activatingItem = false;

                FillRpc(disease.ToString());
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
                    fluidRenderer.SetBlendShapeWeight(0, Mathf.Lerp(0, 100, elapsedTime / fillTime));
                }

                fluidRenderer.SetBlendShapeWeight(0, 100);

                fluidRenderer.enabled = false;
                routine = null;
            }

            routine = StartCoroutine(doEmptyAnimation());
        }

        [Rpc(SendTo.Everyone)]
        public void EmptyRpc()
        {
            storedDisease = "";
            DoEmptyAnimation();
        }

        [Rpc(SendTo.Everyone)]
        public void FillRpc(string _disease)
        {
            Disease? disease = Disease.GetDiseaseFromString(_disease);
            if (disease == null) { logger.LogError("Unable to parse disease from id"); return; }

            storedDisease = _disease;

            fluidRenderer.enabled = true;
            fluidRenderer.SetBlendShapeWeight(0, 0);
            SetFluidColor(disease.GetChemistryLiquidAppearance());
        }

        ChemistryIngredient IChemistryIngredient.GetIngredient()
        {
            var _storedDisease = storedDisease;
            storedDisease = "";
            DoEmptyAnimation();
            return new ChemistryIngredient(itemProperties, new ChemistryLiquidAppearance(fluidRenderer.material.color, fluidRenderer.material.GetFloat("_EmissionIntensity")), storedDisease);
        }

        void IChemistryIngredient.OnChemicalMixerOutput(string specialInstructions)
        {
            return;
        }

        ChemistryIngredient? IDistillableIngredient.DistilleryOutput()
        {
            return new ChemistryIngredient(LethalContent.Items[LethalDiseasesKeys.TestTube].Item, new ChemistryLiquidAppearance(fluidRenderer.material.color, fluidRenderer.material.GetFloat("_EmissionIntensity")), storedDisease);
        }

        float IDistillableIngredient.DistilleryMixTime()
        {
            return 10f;
        }

        bool IDistillableIngredient.DespawnItemAfterDistilleryInput()
        {
            return false;
        }

        bool IChemistryOutputContainer.ReceiveChemistryOutput(ChemistryIngredient ingredient)
        {
            if (isFilled)
            {
                HUDManager.Instance.DisplayTip("Insert ingredient failed", "Container is already full", isWarning: true);
                return false;
            }

            if (ingredient.specialInstructions.IsNullOrWhiteSpace())
            {
                HUDManager.Instance.DisplayTip("Insert ingredient failed", "No diseases to insert", isWarning: true);
                return false;
            }

            storedDisease = ingredient.specialInstructions;
            return true;
        }
    }
}