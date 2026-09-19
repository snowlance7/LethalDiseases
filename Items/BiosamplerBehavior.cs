using BepInEx;
using Dawn;
using GameNetcodeStuff;
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

        bool LocalPlayerIsLookingDown => Vector3.Dot(localPlayer.gameplayCamera.transform.forward, Vector3.down) > 0.98f;

        string mainControlTip = "Extract [LMB]";

        const float fillTime = 1.5f;
        const float distanceRequired = 1f;

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

        public override void Update()
        {
            base.Update();

            if (playerHeldBy != null && playerHeldBy == localPlayer && isHeld)
            {
                if (LocalPlayerIsLookingDown)
                {
                    if (isFilled && mainControlTip != "Self Inject [LMB]")
                    {
                        mainControlTip = "Self Inject [LMB]";
                        SetControlTipsForItem();
                    }
                    else if (!isFilled && mainControlTip != "Self Extract [LMB]")
                    {
                        mainControlTip = "Self Extract [LMB]";
                        SetControlTipsForItem();
                    }
                }
                else if (!LocalPlayerIsLookingDown && mainControlTip != "Extract [LMB]")
                {
                    mainControlTip = "Extract [LMB]";
                    SetControlTipsForItem();
                }
            }
        }

        public override void SetControlTipsForItem()
        {
            string[] toolTips = [mainControlTip, "Empty [Q]"];
            HUDManager.Instance.ChangeControlTipMultiple(toolTips, holdingItem: true, itemProperties);
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
            if (!buttonDown) { return; }

            DoStabAnimation();
        }

        public override void ItemInteractLeftRight(bool right)
        {
            base.ItemInteractLeftRight(right);
            if (right || !isFilled) { return; }

            EmptyAnimationRpc();
        }

        public void DoStabAnimation()
        {
            IEnumerator doStabAnimation()
            {
                yield return null;

                localPlayer.playerBodyAnimator.SetTrigger("UseHeldItem1"); // Stab animation
                DoStabAnimationRpc(localPlayer.actualClientId);

                yield return new WaitForSeconds(11f / 60f);

                if (isFilled)
                {
                    if (!LocalPlayerIsLookingDown)
                    {
                        CancelStabAnimation();
                        yield break;
                    }

                    freezingLocalPlayer = true;
                    localPlayer.FreezePlayer(true);
                    localPlayer.playerBodyAnimator.speed = 0f;

                    float elapsedTime = 0f;

                    while (elapsedTime < fillTime)
                    {
                        yield return null;

                        elapsedTime += Time.deltaTime;
                        fluidRenderer.SetBlendShapeWeight(0, Mathf.Lerp(0, 100, elapsedTime / fillTime));
                    }

                    localPlayer.Infect(storedDisease);
                    CancelStabAnimation();
                    EmptyRpc();
                }
                else
                {
                    float closestDistance = 0f;
                    DiseaseHost? host = LocalPlayerIsLookingDown ? localPlayer.NetworkObject.GetHost() : DiseaseHost.Instances.Where(x => x.hasActor && x.hasDisease && x.player != localPlayer).GetClosestToPosition(transform.position, (x) => x.transform.position, out closestDistance, fastDistanceCheck: true);

                    if (host == null || closestDistance > distanceRequired)
                    {
                        CancelStabAnimation();
                        yield break;
                    }

                    Disease disease = Disease.MergeDiseases(host.Diseases);
                    SetFluidColor(disease.GetChemistryLiquidAppearance());

                    freezingLocalPlayer = true;
                    localPlayer.FreezePlayer(true);
                    localPlayer.playerBodyAnimator.speed = 0f;

                    fluidRenderer.enabled = true;

                    float elapsedTime = 0f;

                    while (elapsedTime < fillTime)
                    {
                        yield return null;

                        if ((host.transform.position - localPlayer.transform.position).sqrMagnitude > distanceRequired * distanceRequired)
                        {
                            fluidRenderer.SetBlendShapeWeight(0, 100);
                            fluidRenderer.enabled = false;
                            CancelStabAnimation();
                            yield break;
                        }

                        elapsedTime += Time.deltaTime;
                        fluidRenderer.SetBlendShapeWeight(0, Mathf.Lerp(100, 0, elapsedTime / fillTime));
                    }

                    fluidRenderer.SetBlendShapeWeight(0, 0);
                    CancelStabAnimation();

                    FillRpc(disease.ToString());
                }
            }

            if (routine != null) { return; }
            localPlayer.activatingItem = true;
            routine = StartCoroutine(doStabAnimation());
        }

        private void CancelStabAnimation()
        {
            if (freezingLocalPlayer)
            {
                localPlayer.FreezePlayer(false);
                freezingLocalPlayer = false;
            }
            localPlayer.playerBodyAnimator.speed = 1f;
            localPlayer.activatingItem = false;
            routine = null;
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

            if (routine != null) { return; }
            routine = StartCoroutine(doEmptyAnimation());
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void EmptyRpc()
        {
            fluidRenderer.SetBlendShapeWeight(0, 100);
            fluidRenderer.enabled = false;
            storedDisease = "";
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void EmptyAnimationRpc()
        {
            storedDisease = "";
            DoEmptyAnimation();
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void FillRpc(string _disease)
        {
            Disease? disease = Disease.GetDiseaseFromString(_disease);
            if (disease == null) { logger.LogError("Unable to parse disease from id"); return; }

            storedDisease = _disease;
            scanNode.subText = storedDisease;

            fluidRenderer.enabled = true;
            fluidRenderer.SetBlendShapeWeight(0, 0);
            SetFluidColor(disease.GetChemistryLiquidAppearance());
        }

        [Rpc(SendTo.NotMe, RequireOwnership = false)]
        public void DoStabAnimationRpc(ulong clientId)
        {
            PlayerControllerB? player = PlayerFromId(clientId);
            player?.playerBodyAnimator.SetTrigger("UseHeldItem1");
        }

        ChemistryIngredient IChemistryIngredient.GetIngredient()
        {
            return new ChemistryIngredient(LethalDiseasesKeys.Biosampler, new ChemistryLiquidAppearance(fluidRenderer.material.color, fluidRenderer.material.GetFloat("_EmissionIntensity")), storedDisease);
        }

        void IChemistryIngredient.OnChemicalOutput(string specialInstructions)
        {
            return;
        }

        ChemistryIngredient? IDistillableIngredient.DistilleryOutput()
        {
            var _storedDisease = storedDisease;
            storedDisease = "";
            DoEmptyAnimation();
            return new ChemistryIngredient(LethalDiseasesKeys.TestTube, new ChemistryLiquidAppearance(fluidRenderer.material.color, fluidRenderer.material.GetFloat("_EmissionIntensity")), _storedDisease);
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