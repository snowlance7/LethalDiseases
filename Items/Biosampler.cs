using Dawn;
using Dawn.Interfaces;
using GameNetcodeStuff;
using Newtonsoft.Json.Linq;
using SnowyCraftingCore;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items
{
    internal class Biosampler : PhysicsProp, IDistillableIngredient, IChemistryOutputContainer, IDawnSaveData, IDiseaseStorable
    {
        public static bool IsEnabled => LethalContent.Items[LethalDiseasesKeys.Biosampler] != null;

        public SkinnedMeshRenderer fluidRenderer = null!;

        ScanNodeProperties scanNode = null!;
        Animator animator = null!;
        AudioSource audioSource = null!;
        PlayerControllerB previousPlayerHeldBy = null!;

        public string StoredDisease { get; set; } = "";

        public bool IsFilled { get; private set; }

        Coroutine? routine;

        bool LocalPlayerIsLookingDown => Vector3.Dot(localPlayer.gameplayCamera.transform.forward, Vector3.down) > 0.98f;

        string mainControlTip = "Extract [LMB]";
        bool inStabbingAnimation;

        const float fillTime = 1.5f;
        const float maxDistance = 2f;
        readonly Vector3 stabPositionOffset = new Vector3(-0.05f, 0.1f, 0f);
        readonly Vector3 stabRotationOffset = new Vector3(-10, -90, 0);

        public void SetFluidColor(ChemistryLiquidAppearance color)
        {
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
            audioSource = gameObject.GetComponent<AudioSource>();
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

            EmptyRpc();
        }

        public void DoStabAnimation()
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
                            localPlayer.DamagePlayer(0, hasDamageSFX: false, causeOfDeath: CauseOfDeath.Stabbing);
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
                    FillRpc(diseaseString, true, disease.GetChemistryLiquidAppearance());
                }
                else
                {
                    FillRpc("", true, new ChemistryLiquidAppearance(UnityEngine.Random.ColorHSV(), UnityEngine.Random.Range(0f, 10f)));
                }

                localPlayer.playerBodyAnimator.speed = 0f;
                FreezePlayerAnimatorRpc(true);

                float elapsedTime = 0f;
                while (elapsedTime < fillTime)
                {
                    yield return null;

                    if (!CanContinueAnimation() || Vector3.Distance(host.transform.position, localPlayer.transform.position) > maxDistance)
                    {
                        logger.LogDebug("Cancelling stab animation early");
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
            DiseaseHost? host = null;
            bool hasHitSomething = false;
            int surfaceIndex = -1;

            var objectsHitByKnife = Physics.SphereCastAll(previousPlayerHeldBy.gameplayCamera.transform.position + previousPlayerHeldBy.gameplayCamera.transform.right * 0.1f, 0.3f, previousPlayerHeldBy.gameplayCamera.transform.forward, 0.75f, 1084754248, QueryTriggerInteraction.Collide);
            var objectsHitByKnifeList = objectsHitByKnife.OrderBy((x) => x.distance).ToList();

            foreach (var hit in objectsHitByKnifeList)
            {
                if (hit.transform.gameObject.layer == 8 || hit.transform.gameObject.layer == 11)
                {
                    hasHitSomething = true;
                    string hitTag = hit.collider.gameObject.tag;
                    for (int j = 0; j < StartOfRound.Instance.footstepSurfaces.Length; j++)
                    {
                        if (StartOfRound.Instance.footstepSurfaces[j].surfaceTag == hitTag)
                        {
                            surfaceIndex = j;
                            break;
                        }
                    }
                }
                else
                {
                    if (!hit.transform.TryGetComponent<IHittable>(out var hittable) || hit.transform == previousPlayerHeldBy.transform || !(hit.point == Vector3.zero) && Physics.Linecast(previousPlayerHeldBy.gameplayCamera.transform.position, hit.point, out var _, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore)) { continue; }

                    hasHitSomething = true;

                    EnemyAICollisionDetect collision = hit.transform.GetComponent<EnemyAICollisionDetect>();
                    if (collision != null && collision.mainScript != null)
                    {
                        host = collision.mainScript.GetHost();
                        break;
                    }

                    PlayerControllerB? playerHit = hit.transform.GetComponent<PlayerControllerB>();
                    if (playerHit != null)
                    {
                        host = playerHit.GetHost();
                    }
                }
            }

            if (hasHitSomething)
            {
                audioSource.Play();
                RoundManager.Instance.PlayAudibleNoise(transform.position, 17f, 0.8f);
                if (host == null && surfaceIndex != -1)
                {
                    HitSurfaceRpc(surfaceIndex);
                }
            }

            return host;
        }

        private bool CanContinueAnimation()
        {
            return localPlayer != null
                && localPlayer == previousPlayerHeldBy
                && !localPlayer.isWalking
                && !localPlayer.isJumping
                && !localPlayer.isPlayerDead
                && localPlayer.isPlayerControlled
                && !localPlayer.inSpecialMenu
                && !localPlayer.inShockingMinigame
                && !localPlayer.inTerminalMenu
                && !localPlayer.isTypingChat
                && !localPlayer.inSpecialInteractAnimation
                && !localPlayer.isClimbingLadder
                && !localPlayer.takingFallDamage;
        }

        private void CancelStabAnimation()
        {
            localPlayer.playerBodyAnimator.speed = 1f;
            localPlayer.activatingItem = false;
            inStabbingAnimation = false;
            routine = null;
        }

        public void Fill(string diseaseString, bool doAnimation, ChemistryLiquidAppearance color)
        {
            StoredDisease = diseaseString;
            SetFluidColor(color);
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
        public void FillRpc(string diseaseString, bool doAnimation, ChemistryLiquidAppearance color)
        {
            Fill(diseaseString, doAnimation, color);
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

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void HitSurfaceRpc(int hitSurfaceID)
        {
            audioSource.PlayOneShot(StartOfRound.Instance.footstepSurfaces[hitSurfaceID].hitSurfaceSFX);
            WalkieTalkie.TransmitOneShotAudio(audioSource, StartOfRound.Instance.footstepSurfaces[hitSurfaceID].hitSurfaceSFX);
        }

        public ChemistryIngredient GetIngredient()
        {
            return new ChemistryIngredient(LethalDiseasesKeys.TestTube, new ChemistryLiquidAppearance(fluidRenderer.material.color, fluidRenderer.material.GetFloat("_EmissiveIntensity")), StoredDisease);
        }

        public void OnChemicalOutput(ChemistryIngredient ingredient)
        {
            Fill(ingredient.specialInstructions, true, ingredient.chemistryLiquidAppearance);
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

            Fill(ingredient.specialInstructions, true, ingredient.chemistryLiquidAppearance);
            return true;
        }

        public JToken GetDawnDataToSave()
        {
            return JToken.FromObject(StoredDisease);
        }

        public void LoadDawnSaveData(JToken saveData)
        {
            string? diseaseString = saveData.Value<string>();
            if (diseaseString == null) { return; }
            Disease? disease = Disease.GetDiseaseFromString(diseaseString);
            if (disease == null) { return; }
            Fill(diseaseString, false, disease.GetChemistryLiquidAppearance());
        }
    }
}