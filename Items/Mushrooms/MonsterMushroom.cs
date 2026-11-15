using GameNetcodeStuff;
using SnowyLib;
using System;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items.Mushrooms;

// Mushroom monster (March and Adamance)

internal class MonsterMushroom : Mushroom, IHittable
{
    Animator animator = null!;
    AudioSource audioSource = null!;

    bool bitingPlayer;
    float timeHeld;

    bool isDead;

    Vector3 restingRotation => isDead ? itemProperties.restingRotation : Vector3.zero;

    public void Awake()
    {
        animator = gameObject.GetComponent<Animator>();
        audioSource = gameObject.GetComponent<AudioSource>();
    }

    public override void Update()
    {
        ModifiedBaseUpdate();
        
        if (!IsServer || isDead) { return; }
        timeHeld = playerHeldBy != null ? timeHeld + Time.deltaTime : 0;

        if (timeHeld > 3f && !bitingPlayer)
        {
            bitingPlayer = true;
            DoAnimationRpc("bite");
        }
    }

    public void ModifiedBaseUpdate()
    {
        if (currentUseCooldown >= 0f)
        {
            currentUseCooldown -= Time.deltaTime;
        }
        if (base.IsOwner)
        {
            if (isBeingUsed && itemProperties.requiresBattery)
            {
                if (insertedBattery.charge > 0f)
                {
                    if (!itemProperties.itemIsTrigger)
                    {
                        insertedBattery.charge -= Time.deltaTime / itemProperties.batteryUsage;
                    }
                }
                else if (!insertedBattery.empty)
                {
                    insertedBattery.empty = true;
                    if (isBeingUsed)
                    {
                        Debug.Log("Use up batteries local");
                        isBeingUsed = false;
                        UseUpBatteries();
                        UseUpItemBatteriesServerRpc();
                    }
                }
            }
            if (!wasOwnerLastFrame)
            {
                wasOwnerLastFrame = true;
            }
        }
        else if (wasOwnerLastFrame)
        {
            wasOwnerLastFrame = false;
        }
        if (!isHeld && parentObject == null)
        {
            if (fallTime < 1f)
            {
                reachedFloorTarget = false;
                FallWithCurve();
                if (base.transform.localPosition.y - targetFloorPosition.y < 0.05f && !hasHitGround)
                {
                    PlayDropSFX();
                    OnHitGround();
                }
                return;
            }
            if (!reachedFloorTarget)
            {
                if (!hasHitGround)
                {
                    PlayDropSFX();
                    OnHitGround();
                }
                reachedFloorTarget = true;
                if (floorYRot == -1)
                {
                    base.transform.rotation = Quaternion.Euler(restingRotation.x, base.transform.eulerAngles.y, restingRotation.z);
                }
                else
                {
                    base.transform.rotation = Quaternion.Euler(restingRotation.x, (float)(floorYRot + itemProperties.floorYOffset) + 90f, restingRotation.z);
                }
            }
            base.transform.localPosition = targetFloorPosition;
        }
        else
        {
            if (!isHeld && !isHeldByEnemy)
            {
                return;
            }
            reachedFloorTarget = false;
            if (playerHeldBy != null && !isPocketed && playerHeldBy.scrapJiggleAudioDelay > -4f)
            {
                if (playerHeldBy.scrapJiggleAudioDelay <= 0f)
                {
                    playerHeldBy.scrapJiggleAudioDelay = -5f;
                    JiggleItemEffect(UnityEngine.Random.Range(0.18f, 0.36f));
                }
                else
                {
                    playerHeldBy.scrapJiggleAudioDelay -= Time.deltaTime;
                }
            }
        }
    }

    public override void GrabItem()
    {
        base.GrabItem();
        DoAnimationRpc("hide");
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        if (!buttonDown || !isDead) { return; }
        OnEatMushroom();
        playerHeldBy.DespawnHeldObject();
    }

    public override void OnEatMushroom()
    {
        localPlayer.HealPlayerSynced(5, allowOverhealing: true);
    }

    public void OnBitePlayer() // Animation
    {
        bitingPlayer = false;

        if (playerHeldBy != null && localPlayer == playerHeldBy)
        {
            localPlayer.DiscardItemInSlotSynced(Array.IndexOf(localPlayer.ItemSlots, this));
            localPlayer.DamagePlayer(5, causeOfDeath: CauseOfDeath.Mauling);
        }
    }

    public override void FallWithCurve()
    {
        float num = startFallingPosition.y - targetFloorPosition.y;
        if (floorYRot == -1)
        {
            base.transform.rotation = Quaternion.Lerp(base.transform.rotation, Quaternion.Euler(restingRotation.x, base.transform.eulerAngles.y, restingRotation.z), Mathf.Clamp(14f * Time.deltaTime / num, 0f, 1f));
        }
        else
        {
            base.transform.rotation = Quaternion.Lerp(base.transform.rotation, Quaternion.Euler(restingRotation.x, (float)(floorYRot + itemProperties.floorYOffset) + 90f, restingRotation.z), Mathf.Clamp(14f * Time.deltaTime / num, 0f, 1f));
        }
        if (num > 5f)
        {
            base.transform.localPosition = Vector3.Lerp(startFallingPosition, targetFloorPosition, StartOfRound.Instance.objectFallToGroundCurveNoBounce.Evaluate(fallTime));
        }
        else
        {
            base.transform.localPosition = Vector3.Lerp(startFallingPosition, targetFloorPosition, StartOfRound.Instance.objectFallToGroundCurve.Evaluate(fallTime));
        }
        fallTime += Mathf.Abs(Time.deltaTime * 6f / num);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void DoAnimationRpc(string animationName)
    {
        animator.SetTrigger(animationName);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void BitePlayerRpc()
    {
        animator.SetTrigger("bite");
        audioSource.Play();
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void HitRpc()
    {
        isDead = true;
        animator.SetTrigger("die");
    }

    public bool Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        if (isDead || playerHeldBy != null || playerAttachedTo != null) { return false; }
        HitRpc();
        return true;
    }
}

//internal class MonsterMushroomCollisionDetect : MonoBehaviour, IHittable
//{
//    [SerializeField] MonsterMushroom mainScript = null!;

//    private void OnTriggerStay(Collider other)
//    {
//        if (!other.CompareTag("Player")) { return; }
//        mainScript.OnCollideWithPlayer(other);
//    }

//    bool IHittable.Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit, bool playHitSFX, int hitID)
//    {
//        if (playerWhoHit == null) { return false; }
//        mainScript.OnHit(force, playerWhoHit);
//        return true;
//    }
//}