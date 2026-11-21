using BepInEx;
using Dawn;
using Dawn.Interfaces;
using GameNetcodeStuff;
using Newtonsoft.Json.Linq;
using SnowyCraftingCore;
using SnowyLib;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements.Experimental;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items
{
    internal class Biosampler : PhysicsProp, IDistillableIngredient, IChemistryOutputContainer, IDawnSaveData, IDiseaseStorable
    {
        public static bool IsEnabled => LethalContent.Items[LethalDiseasesKeys.Biosampler] != null;

        public SkinnedMeshRenderer fluidRenderer = null!;

        ScanNodeProperties scanNode = null!;
        Animator animator = null!;
        PlayerControllerB previousPlayerHeldBy = null!;

        public string StoredDisease { get; set; } = "";

        public bool IsFilled { get; private set; }

        Coroutine? routine;

        bool LocalPlayerIsLookingDown => Vector3.Dot(localPlayer.gameplayCamera.transform.forward, Vector3.down) > 0.98f;

        string mainControlTip = "Extract [LMB]";
        bool inStabbingAnimation;

        const float fillTime = 1.5f;
        const float maxDistance = 1f;
        readonly Vector3 stabPositionOffset = new Vector3(-0.05f, 0.1f, 0f);
        readonly Vector3 stabRotationOffset = new Vector3(-10, -90, 0);

        public void SetFluidColor(ChemistryLiquidAppearance color)
        {
            //fluidRenderer.enabled = true;
            fluidRenderer.material.color = color.liquidColor;
            fluidRenderer.material.SetColor("_EmissiveColor", color.liquidColor);
            fluidRenderer.material.SetFloat("_EmissiveIntensity", color.emissionIntensity);
        }

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0.05f, 0.1f, 0f);
            itemProperties.rotationOffset = new Vector3(-10, 90, 0);
            itemProperties.floorYOffset = 0;
            itemProperties.grabAnim = "HoldKnife";
            scanNode = gameObject.GetComponentInChildren<ScanNodeProperties>();
            animator = GetComponent<Animator>();
        }

        public override void Update()
        {
            base.Update();

            if (playerHeldBy != null)
            {
                previousPlayerHeldBy = playerHeldBy;
            }

            if (playerHeldBy != null && playerHeldBy == localPlayer && isHeld)
            {
                if (LocalPlayerIsLookingDown)
                {
                    if (!IsFilled && mainControlTip != "Self Extract [LMB]")
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

            scanNode.subText = Disease.GetNameFromString(StoredDisease);
        }

        public override void LateUpdate()
        {
            if (parentObject != null)
            {
                base.transform.rotation = parentObject.rotation;
                base.transform.Rotate(inStabbingAnimation ? stabRotationOffset : itemProperties.rotationOffset);
                base.transform.position = parentObject.position;
                Vector3 positionOffset = inStabbingAnimation ? stabPositionOffset : itemProperties.positionOffset;
                positionOffset = parentObject.rotation * positionOffset;
                base.transform.position += positionOffset;
            }
            if (rotateObject)
            {
                base.transform.Rotate(new Vector3(0f, Time.deltaTime * 60f, 0f), Space.World);
            }
            if (radarIcon != null)
            {
                radarIcon.position = base.transform.position;
            }
        }

        public override void EnableItemMeshes(bool enable)
        {
            base.EnableItemMeshes(enable);
            fluidRenderer.enabled = enable && IsFilled; // TODO: This isnt working
            logger.LogDebug("enabled: " + (enable && IsFilled).ToString());
        }

        public override void SetControlTipsForItem()
        {
            string[] toolTips = [mainControlTip, "Empty [Q]"];
            HUDManager.Instance.ChangeControlTipMultiple(toolTips, holdingItem: true, itemProperties);
        }

        public override void EquipItem()
        {
            base.EquipItem();
            playerHeldBy.equippedUsableItemQE = true;
        }

        public override void DiscardItem()
        {
            playerHeldBy.equippedUsableItemQE = false;
            base.DiscardItem();
        }

        public override void PocketItem()
        {
            playerHeldBy.equippedUsableItemQE = false;
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
            if (right || !IsFilled) { return; }

            Empty();
        }

        public void DoStabAnimation() // TODO: Test this
        {
            IEnumerator doStabAnimation()
            {
                yield return null;

                inStabbingAnimation = true;
                localPlayer.playerBodyAnimator.SetTrigger("UseHeldItem1");
                DoStabAnimationRpc();

                yield return new WaitForSeconds(11f / 60f);

                DiseaseHost? host = null;

                if (LocalPlayerIsLookingDown)
                {
                    host = localPlayer.GetHost();
                }
                else
                {
                    host = GetClosestDiseaseHost();
                }

                if (host == null)
                {
                    CancelStabAnimation();
                    yield break;
                }

                if (host.player != null)
                {
                    if (host.player == localPlayer)
                    {
                        if (localPlayer.health != 1)
                        {
                            localPlayer.inSpecialInteractAnimation = true;
                            localPlayer.DamagePlayer(1, hasDamageSFX: false, causeOfDeath: CauseOfDeath.Stabbing);
                            localPlayer.inSpecialInteractAnimation = false;
                        }
                    }
                    else
                    {
                        DamagePlayerRpc(host.player.actualClientId);
                    }
                }
                else if (host.enemy != null)
                {
                    host.enemy.HitEnemyServerRpc(0, (int)localPlayer.actualClientId, false);
                }

                if (IsFilled)
                {
                    CancelStabAnimation();
                    yield break;
                }

                string diseaseString = "";

                if (host.Diseases.Count > 0)
                {
                    Disease disease = Disease.MergeDiseases(host.Diseases);
                    diseaseString = disease.ToString();
                    FillRpc(diseaseString);
                }
                else
                {
                    FillRpc();
                }

                localPlayer.playerBodyAnimator.speed = 0f;
                FreezePlayerAnimatorRpc(true);

                float elapsedTime = 0f;
                while (elapsedTime < fillTime)
                {
                    yield return null;

                    if (!CanContinueAnimation() || Vector3.Distance(host.transform.position, localPlayer.transform.position) > maxDistance)
                    {
                        animator.SetTrigger("emptied");
                        EmptyRpc(doAnimation: false);
                        FreezePlayerAnimatorRpc(false);
                        CancelStabAnimation();
                        yield break;
                    }

                    elapsedTime += Time.deltaTime;
                }

                FreezePlayerAnimatorRpc(false);
                CancelStabAnimation();
            }

            if (routine != null) { return; }
            logger.LogDebug("Doing stab animation");
            localPlayer.activatingItem = true;
            routine = StartCoroutine(doStabAnimation());
        }

        private DiseaseHost? GetClosestDiseaseHost()
        {
            DiseaseHost? closest = null;
            float closestAngle = 361;

            foreach (var host in DiseaseHost.Instances)
            {
                if (!host.hasActor) { continue; }
                var position = host.player != null ? host.player.bodyParts[5].position : host.enemy!.eye.position;
                float angle = localPlayer.LineOfSightToPositionAngle(position, (int)maxDistance);
                if (angle == -361f || angle > closestAngle) { continue; }
                closest = host;
                closestAngle = angle;
            }

            return closest;
        }

        private bool CanContinueAnimation()
        {
            return localPlayer != null
                && localPlayer == localPlayer
                && !localPlayer.isWalking
                && !localPlayer.isJumping
                && localPlayer.IsPlayerNearGround()
                && !localPlayer.isPlayerSliding
                && !localPlayer.isPlayerDead
                && localPlayer.isPlayerControlled
                && !localPlayer.inSpecialMenu
                && !localPlayer.inShockingMinigame
                && !localPlayer.inTerminalMenu
                && !localPlayer.isTypingChat
                && !localPlayer.twoHanded
                && !localPlayer.twoHandedAnimation
                && !localPlayer.inSpecialInteractAnimation
                && !localPlayer.isClimbingLadder
                && !localPlayer.isGrabbingObjectAnimation
                && !localPlayer.takingFallDamage
                && localPlayer.timeSinceTakingDamage > Time.deltaTime; // TODO: Test this
        }

        private void CancelStabAnimation()
        {
            localPlayer.playerBodyAnimator.speed = 1f;
            localPlayer.activatingItem = false;
            inStabbingAnimation = false;
            routine = null;
        }

        public void Fill(string diseaseString = "", bool doAnimation = true)
        {
            StoredDisease = diseaseString;

            Disease? disease = Disease.GetDiseaseFromString(diseaseString);
            SetFluidColor(disease != null ? disease.GetChemistryLiquidAppearance() : new ChemistryLiquidAppearance(Random.ColorHSV()));
            animator.SetTrigger(doAnimation ? "fill" : "filled");
            IsFilled = true;
        }

        public void Empty(bool doAnimation = true)
        {
            StoredDisease = "";
            animator.SetTrigger(doAnimation ? "empty" : "emptied");
            IsFilled = false;
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void FillRpc(string diseaseString = "", bool doAnimation = true)
        {
            Fill(diseaseString, doAnimation);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void EmptyRpc(bool doAnimation = true)
        {
            Empty(doAnimation);
        }

        [Rpc(SendTo.NotMe, RequireOwnership = false)]
        public void DoStabAnimationRpc()
        {
            inStabbingAnimation = true;
            previousPlayerHeldBy.playerBodyAnimator.SetTrigger("UseHeldItem1");
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void DamagePlayerRpc(ulong clientId)
        {
            if (localPlayer.actualClientId != clientId || localPlayer.health == 1) { return; }
            localPlayer.inSpecialInteractAnimation = true;
            localPlayer.DamagePlayer(1, hasDamageSFX: false, causeOfDeath: CauseOfDeath.Stabbing);
            localPlayer.inSpecialInteractAnimation = false;
        }

        [Rpc(SendTo.NotMe, RequireOwnership = false)]
        public void FreezePlayerAnimatorRpc(bool value)
        {
            previousPlayerHeldBy.playerBodyAnimator.speed = value ? 0 : 1;
        }

        public ChemistryIngredient GetIngredient()
        {
            return new ChemistryIngredient(LethalDiseasesKeys.TestTube, new ChemistryLiquidAppearance(fluidRenderer.material.color, fluidRenderer.material.GetFloat("_EmissiveIntensity")), StoredDisease);
        }

        public void OnChemicalOutput(ChemistryIngredient ingredient)
        {
            Fill(ingredient.specialInstructions);
        }

        public ChemistryIngredient? DistilleryOutput()
        {
            var _storedDisease = StoredDisease;
            Empty();
            return new ChemistryIngredient(LethalDiseasesKeys.TestTube, new ChemistryLiquidAppearance(fluidRenderer.material.color, fluidRenderer.material.GetFloat("_EmissiveIntensity")), _storedDisease);
        }

        public float DistilleryMixTime()
        {
            return 10f;
        }

        public bool DespawnItemAfterDistilleryInput()
        {
            return false;
        }

        public bool ReceiveChemistryOutput(ChemistryIngredient ingredient)
        {
            if (IsFilled) { return false; }

            Fill(ingredient.specialInstructions);
            return true;
        }

        public JToken GetDawnDataToSave()
        {
            return JToken.FromObject(StoredDisease);
        }

        public void LoadDawnSaveData(JToken saveData)
        {
            string? diseaseString = saveData.Value<string>();
            Fill(diseaseString != null ? diseaseString : "", false);
        }
    }
}