using BepInEx;
using Dawn;
using Dawn.Interfaces;
using GameNetcodeStuff;
using LethalDiseases.Unlockables;
using Newtonsoft.Json.Linq;
using SnowyCraftingCore;
using SnowyLib;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items
{
    internal class SyringeBehavior : GunAmmo, IChemistryOutputContainer, IDawnSaveData
    {
        public SkinnedMeshRenderer fluidRenderer = null!;
        public Animator animator = null!;

        ScanNodeProperties scanNode = null!;

        public string storedDisease = "";

        public bool isFilled => !storedDisease.IsNullOrWhiteSpace();

        public bool freezingLocalPlayer;

        Coroutine? routine;

        bool LocalPlayerIsLookingDown => Vector3.Dot(localPlayer.gameplayCamera.transform.forward, Vector3.down) > 0.98f;

        string mainControlTip = "";

        const float fillTime = 1.5f;

        readonly float maxDistance = 5f;

        bool inStabbingAnimation;
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
                if (isFilled)
                {
                    if (LocalPlayerIsLookingDown && mainControlTip != "Self Inject [LMB]")
                    {
                        mainControlTip = "Self Inject [LMB]";
                        SetControlTipsForItem();
                    }
                    else if (!LocalPlayerIsLookingDown && mainControlTip != "Inject [LMB]")
                    {
                        mainControlTip = "Inject [LMB]";
                        SetControlTipsForItem();
                    }
                }
                else
                {
                    if (mainControlTip != "")
                    {
                        mainControlTip = "";
                        SetControlTipsForItem();
                    }
                }
            }
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

        public override void SetControlTipsForItem()
        {
            string[] toolTips = string.IsNullOrWhiteSpace(mainControlTip) ? ["Empty [Q]"] : [mainControlTip, "Empty [Q]"];
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

        public override void EnableItemMeshes(bool enable)
        {
            base.EnableItemMeshes(enable);
            fluidRenderer.enabled = enable && isFilled;
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

            SetDiseaseRpc("");
        }

        public void DoStabAnimation()
        {
            IEnumerator doStabAnimation()
            {
                yield return null;

                inStabbingAnimation = true;
                localPlayer.playerBodyAnimator.SetTrigger("UseHeldItem1"); // Stab animation
                DoStabAnimationRpc(localPlayer.actualClientId);

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
                    SetDiseaseRpc("");
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
            inStabbingAnimation = false;
            routine = null;
        }

        public void SetDisease(string diseaseId, bool doAnimation = true)
        {
            Disease? disease = Disease.GetDiseaseFromString(diseaseId);
            
            if (disease != null)
            {
                storedDisease = diseaseId;
                scanNode.subText = disease.name;

                SetFluidColor(disease.GetChemistryLiquidAppearance());
                animator.SetTrigger(doAnimation ? "fill" : "filled");
            }
            else
            {
                storedDisease = "";
                animator.SetTrigger("inject");
            }
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void SetDiseaseRpc(string diseaseId, bool doAnimation = false)
        {
            SetDisease(diseaseId, doAnimation);
        }

        [Rpc(SendTo.NotMe, RequireOwnership = false)]
        public void DoStabAnimationRpc(ulong clientId)
        {
            PlayerControllerB? player = PlayerFromId(clientId);
            player?.playerBodyAnimator.SetTrigger("UseHeldItem1");
        }

        bool IChemistryOutputContainer.ReceiveChemistryOutput(ChemistryIngredient ingredient)
        {
            if (isFilled) { return false; }
            SetDisease(ingredient.specialInstructions);
            return true;
        }

        public JToken GetDawnDataToSave()
        {
            return JToken.FromObject((object)storedDisease);
        }

        public void LoadDawnSaveData(JToken saveData)
        {
            storedDisease = saveData.Value<string>();
            SetDisease(storedDisease);
        }
    }
}