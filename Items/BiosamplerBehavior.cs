using BepInEx;
using Dawn;
using Dawn.Interfaces;
using GameNetcodeStuff;
using Newtonsoft.Json.Linq;
using SnowyCraftingCore;
using SnowyLib;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items
{
    internal class BiosamplerBehavior : PhysicsProp, IDistillableIngredient, IChemistryOutputContainer, IDawnSaveData
    {
        public static bool IsEnabled => LethalContent.Items[LethalDiseasesKeys.Biosampler] != null;

        public Animator animator = null!;
        public SkinnedMeshRenderer fluidRenderer = null!;

        ScanNodeProperties scanNode = null!;

        public string storedDisease = "";

        public bool IsFilled => !storedDisease.IsNullOrWhiteSpace();

        public bool freezingLocalPlayer;

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
        }

        public override void Update()
        {
            base.Update();

            if (playerHeldBy != null && playerHeldBy == localPlayer && isHeld)
            {
                if (LocalPlayerIsLookingDown)
                {
                    if (IsFilled && mainControlTip != "Self Inject [LMB]")
                    {
                        mainControlTip = "Self Inject [LMB]";
                        SetControlTipsForItem();
                    }
                    else if (!IsFilled && mainControlTip != "Self Extract [LMB]")
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
            fluidRenderer.enabled = enable && IsFilled;
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
            fluidRenderer.enabled = IsFilled;
        }

        public override void DiscardItem()
        {
            playerHeldBy.equippedUsableItemQE = false;
            fluidRenderer.enabled = IsFilled;
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
            if (right || !IsFilled) { return; }

            SetDiseaseRpc("", true);
        }

        public void DoStabAnimation()
        {
            IEnumerator doStabAnimation()
            {
                yield return null;

                inStabbingAnimation = true;
                localPlayer.playerBodyAnimator.SetTrigger("UseHeldItem1"); // Stab animation
                DoStabAnimationRpc();

                yield return new WaitForSeconds(11f / 60f);

                if (IsFilled)
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

                    freezingLocalPlayer = true;
                    localPlayer.FreezePlayer(true);
                    localPlayer.playerBodyAnimator.speed = 0f;

                    animator.SetTrigger("empty");

                    float elapsedTime = 0f;
                    while (elapsedTime < fillTime)
                    {
                        yield return null;

                        if ((host.transform.position - localPlayer.transform.position).sqrMagnitude > maxDistance * maxDistance)
                        {
                            animator.SetTrigger("filled");
                            CancelStabAnimation();
                            yield break;
                        }

                        elapsedTime += Time.deltaTime;
                    }

                    CancelStabAnimation();

                    host.networkObject.Infect(storedDisease);
                    SetDiseaseRpc("", false);
                }
                else
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

                    if (host == null || !host.hasDisease)
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

                    Disease disease = Disease.MergeDiseases(host.Diseases);
                    SetFluidColor(disease.GetChemistryLiquidAppearance());

                    freezingLocalPlayer = true;
                    localPlayer.FreezePlayer(true);
                    localPlayer.playerBodyAnimator.speed = 0f;

                    animator.SetTrigger("fill");

                    float elapsedTime = 0f;
                    while (elapsedTime < fillTime)
                    {
                        yield return null;

                        if ((host.transform.position - localPlayer.transform.position).sqrMagnitude > maxDistance * maxDistance)
                        {
                            animator.SetTrigger("emptied");
                            CancelStabAnimation();
                            yield break;
                        }

                        elapsedTime += Time.deltaTime;
                    }

                    CancelStabAnimation();

                    SetDiseaseRpc(disease.ToString(), false);
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

        public void SetDisease(string diseaseId, bool doAnimation)
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
                animator.SetTrigger(doAnimation ? "empty" : "emptied");
            }
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void SetDiseaseRpc(string diseaseId, bool doAnimation)
        {
            SetDisease(diseaseId, doAnimation);
        }

        [Rpc(SendTo.NotMe, RequireOwnership = false)]
        public void DoStabAnimationRpc()
        {
            playerHeldBy?.playerBodyAnimator.SetTrigger("UseHeldItem1");
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void DamagePlayerRpc(ulong clientId)
        {
            if (localPlayer.actualClientId != clientId || localPlayer.health == 1) { return; }
            localPlayer.inSpecialInteractAnimation = true;
            localPlayer.DamagePlayer(1, hasDamageSFX: false, causeOfDeath: CauseOfDeath.Stabbing);
            localPlayer.inSpecialInteractAnimation = false;
        }

        public ChemistryIngredient GetIngredient()
        {
            return new ChemistryIngredient(LethalDiseasesKeys.Biosampler, new ChemistryLiquidAppearance(fluidRenderer.material.color, fluidRenderer.material.GetFloat("_EmissiveIntensity")), storedDisease);
        }

        public void OnChemicalOutput(ChemistryIngredient ingredient)
        {
            if (string.IsNullOrWhiteSpace(ingredient.specialInstructions)) { return; }
            SetDisease(ingredient.specialInstructions, false);
        }

        public ChemistryIngredient? DistilleryOutput()
        {
            var _storedDisease = storedDisease;
            SetDisease("", false);
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
            if (IsFilled)
            {
                HUDManager.Instance.DisplayTip("Insert ingredient failed", "Container is already full", isWarning: true);
                return false;
            }

            if (string.IsNullOrWhiteSpace(ingredient.specialInstructions))
            {
                HUDManager.Instance.DisplayTip("Insert ingredient failed", "No diseases to insert", isWarning: true);
                return false;
            }

            SetDisease(ingredient.specialInstructions, false);

            return true;
        }

        public JToken GetDawnDataToSave()
        {
            return JToken.FromObject(storedDisease);
        }

        public void LoadDawnSaveData(JToken saveData)
        {
            storedDisease = saveData.Value<string>();
            if (string.IsNullOrWhiteSpace(storedDisease)) { return; }
            SetDisease(storedDisease, false);
        }
    }
}