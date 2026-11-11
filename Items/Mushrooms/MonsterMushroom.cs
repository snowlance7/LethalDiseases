using GameNetcodeStuff;
using SnowyLib;
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

    public void Awake()
    {
        animator = gameObject.GetComponent<Animator>();
        audioSource = gameObject.GetComponent<AudioSource>();
    }

    public override void Update()
    {
        base.Update();
        
        if (!IsServer || isDead) { return; }
        timeHeld = playerHeldBy != null ? timeHeld + Time.deltaTime : 0;

        if (timeHeld > 3f && !bitingPlayer)
        {
            bitingPlayer = true;
            DoAnimationRpc("bite");
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
            playerHeldBy.DiscardHeldObject();
            playerHeldBy.DamagePlayer(5, causeOfDeath: CauseOfDeath.Mauling);
        }
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void DoAnimationRpc(string animationName)
    {
        animator.SetTrigger(animationName);
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