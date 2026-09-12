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
    internal class SyringeBehavior : GunAmmo
    {
        public SkinnedMeshRenderer fluidRenderer = null!;
        public Animator animator = null!;

        ScanNodeProperties scanNode = null!;

        public string storedDisease = "";

        public bool isFilled => !storedDisease.IsNullOrWhiteSpace();

        public bool freezingLocalPlayer;

        Coroutine? routine;

        bool LocalPlayerIsLookingDown => Vector3.Dot(localPlayer.gameplayCamera.transform.forward, Vector3.down) > 0.98f;

        string mainControlTip = "Extract [LMB]";

        const float fillTime = 1.5f;

        readonly float maxDistance = 5f;

        readonly Vector3 stabPositionOffset = new Vector3(0f, 0.16f, 0.01f);
        readonly Vector3 stabRotationOffset = new Vector3(0, 0, 170);

        public void SetFluidColor(ChemistryLiquidAppearance color)
        {
            fluidRenderer.enabled = true;
            fluidRenderer.material.color = color.liquidColor;
            fluidRenderer.material.SetColor("_EmissionColor", color.liquidColor);
            fluidRenderer.material.SetFloat("_EmissionIntensity", color.emissionIntensity);
        }

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0f, 0.08f, 0.01f);
            itemProperties.rotationOffset = new Vector3(0, -4, 20);
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

            logger.LogDebug("StabAnimation");
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

                localPlayer.playerBodyAnimator.SetTrigger("UseHeldItem1"); // Stab animation
                //DoStabAnimationRpc(localPlayer.actualClientId);

                yield return new WaitForSeconds(11f / 60f);

                if (isFilled)
                {
                    DiseaseHost? host = null;

                    if (LocalPlayerIsLookingDown)
                    {
                        host = localPlayer.NetworkObject.GetHost();
                    }
                    else
                    {
                        if (Physics.Raycast(playerHeldBy.gameplayCamera.transform.position + playerHeldBy.gameplayCamera.transform.forward, playerHeldBy.gameplayCamera.transform.forward, out RaycastHit hitInfo, maxDistance, PluginInstance.playerEnemiesPropsMask))
                        {
                            if (hitInfo.collider.gameObject.TryGetComponentInChildren(out NetworkObject? netObj) && netObj != null)
                            {
                                host = netObj.GetHost();
                            }
                        }
                    }


                    if (host == null )
                    {
                        CancelStabAnimation();
                        yield break;
                    }

                    freezingLocalPlayer = true;
                    localPlayer.FreezePlayer(true);
                    localPlayer.playerBodyAnimator.speed = 0f;

                    animator.SetTrigger("inject");

                    float elapsedTime = 0f;
                    while (elapsedTime < fillTime)
                    {
                        yield return null;

                        if ((host.transform.position - localPlayer.transform.position).sqrMagnitude > maxDistance * maxDistance)
                        {
                            animator.SetTrigger("fill");
                            CancelStabAnimation();
                            yield break;
                        }

                        elapsedTime += Time.deltaTime;
                    }

                    CancelStabAnimation();

                    host.networkObject.Infect(storedDisease);
                    storedDisease = "";
                }
                else
                {
                    CancelStabAnimation();
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

        [Rpc(SendTo.Everyone)]
        public void EmptyRpc()
        {
            animator.SetTrigger("inject");
            storedDisease = "";
        }

        [Rpc(SendTo.Everyone)]
        public void FillRpc(string _disease, bool doAnimation)
        {
            Disease? disease = Disease.GetDiseaseFromString(_disease);
            if (disease == null) { logger.LogError("Unable to parse disease from id"); return; }

            storedDisease = _disease;
            scanNode.subText = storedDisease;

            animator.SetTrigger(doAnimation ? "fill" : "filled");
        }

        [Rpc(SendTo.NotMe)]
        public void DoStabAnimationRpc(ulong clientId)
        {
            PlayerControllerB? player = PlayerFromId(clientId);
            player?.playerBodyAnimator.SetTrigger("SwitchHoldAnimation");
            player?.playerBodyAnimator.SetBool("HoldKnife", true);
            player?.playerBodyAnimator.SetTrigger("UseHeldItem1");
        }
    }
}