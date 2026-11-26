using BepInEx;
using Dawn;
using Dawn.Interfaces;
using GameNetcodeStuff;
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
    internal class Syringe : DiseaseStorable, IChemistryOutputContainer
    {
        public static bool IsEnabled => LethalContent.Items[LethalDiseasesKeys.Syringe] != null;

        AudioSource audioSource = null!;
        PlayerControllerB previousPlayerHeldBy = null!;

        Coroutine? routine;

        bool LocalPlayerIsLookingDown => Vector3.Dot(localPlayer.gameplayCamera.transform.forward, Vector3.down) > 0.98f;

        string mainControlTip = "";
        bool useStabOffsets;

        const float fillTime = 1.5f;
        const float maxDistance = 2f;

        readonly Vector3 stabPositionOffset = new Vector3(0f, 0.16f, 0.01f);
        readonly Vector3 stabRotationOffset = new Vector3(0, 0, 170);

        public override void Awake()
        {
            base.Awake();
            audioSource = gameObject.GetComponent<AudioSource>();
        }

        public override void Update()
        {
            base.Update();

            if (playerHeldBy != null)
            {
                previousPlayerHeldBy = playerHeldBy;
            }

            if (playerHeldBy != null && playerHeldBy == localPlayer && !isPocketed)
            {
                if (IsFilled)
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
                base.transform.Rotate(useStabOffsets ? stabRotationOffset : itemProperties.rotationOffset);
                base.transform.position = parentObject.position;
                Vector3 positionOffset = useStabOffsets ? stabPositionOffset : itemProperties.positionOffset;
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
            EmptyRpc(true);
        }

        public void DoStabAnimation()
        {
            IEnumerator doStabAnimation()
            {
                useStabOffsets = true;
                localPlayer.playerBodyAnimator.SetTrigger("UseHeldItem1");
                StartStabAnimationRpc();

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
                    StopStabAnimation();
                    yield break;
                }

                if (host.player != null)
                {
                    if (host.player == localPlayer)
                    {
                        localPlayer.inSpecialInteractAnimation = true;
                        localPlayer.DamagePlayer(localPlayer.health > 1 ? 1 : 0, hasDamageSFX: false, causeOfDeath: CauseOfDeath.Stabbing);
                        localPlayer.inSpecialInteractAnimation = false;
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

                if (!IsFilled)
                {
                    StopStabAnimation();
                    yield break;
                }

                var previousFluidColor = FluidColor;
                var diseaseString = StoredFluid;

                EmptyRpc(true);
                localPlayer.playerBodyAnimator.speed = 0f;
                FreezePlayerAnimatorRpc();

                float elapsedTime = 0f;
                while (elapsedTime < fillTime)
                {
                    yield return null;

                    if (!CanContinueAnimation() || Vector3.Distance(host.transform.position, localPlayer.transform.position) > maxDistance)
                    {
                        logger.LogDebug("Cancelling stab animation early");
                        StopStabAnimation(diseaseString, previousFluidColor);
                        yield break;
                    }

                    elapsedTime += Time.deltaTime;
                }

                host.InfectSynced(diseaseString);
                StopStabAnimation();
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

        private void StopStabAnimation()
        {
            previousPlayerHeldBy.playerBodyAnimator.speed = 1f;
            previousPlayerHeldBy.activatingItem = false;
            useStabOffsets = false;
            routine = null;

            StopStabAnimationRpc();
        }

        private void StopStabAnimation(string storedFluid, ChemistryLiquidAppearance fluidColor)
        {
            previousPlayerHeldBy.playerBodyAnimator.speed = 1f;
            previousPlayerHeldBy.activatingItem = false;
            useStabOffsets = false;
            routine = null;

            Fill(storedFluid, fluidColor);

            StopStabAnimationRpc(storedFluid, fluidColor);
        }

        [Rpc(SendTo.NotMe, RequireOwnership = false)]
        public void StopStabAnimationRpc()
        {
            previousPlayerHeldBy.playerBodyAnimator.speed = 1f;
            useStabOffsets = false;
        }

        [Rpc(SendTo.NotMe, RequireOwnership = false)]
        public void StopStabAnimationRpc(string storedFluid, ChemistryLiquidAppearance fluidColor)
        {
            previousPlayerHeldBy.playerBodyAnimator.speed = 1f;
            useStabOffsets = false;

            Fill(storedFluid, fluidColor);
        }

        [Rpc(SendTo.NotMe, RequireOwnership = false)]
        public void StartStabAnimationRpc()
        {
            useStabOffsets = true;
            previousPlayerHeldBy.playerBodyAnimator.SetTrigger("UseHeldItem1");
        }

        [Rpc(SendTo.NotMe, RequireOwnership = false)]
        public void FreezePlayerAnimatorRpc()
        {
            previousPlayerHeldBy.playerBodyAnimator.speed = 0f;
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void DamagePlayerRpc(ulong clientId)
        {
            if (localPlayer.actualClientId != clientId) { return; }
            localPlayer.inSpecialInteractAnimation = true;
            localPlayer.DamagePlayer(localPlayer.health > 1 ? 1 : 0, hasDamageSFX: false, causeOfDeath: CauseOfDeath.Stabbing);
            localPlayer.inSpecialInteractAnimation = false;
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void HitSurfaceRpc(int hitSurfaceID)
        {
            audioSource.PlayOneShot(StartOfRound.Instance.footstepSurfaces[hitSurfaceID].hitSurfaceSFX);
            WalkieTalkie.TransmitOneShotAudio(audioSource, StartOfRound.Instance.footstepSurfaces[hitSurfaceID].hitSurfaceSFX);
        }
    }
}