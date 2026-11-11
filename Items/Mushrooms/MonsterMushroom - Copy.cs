using Dawn.Utils;
using GameNetcodeStuff;
using SnowyLib;
using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items.Mushrooms;

// Mushroom monster (March and Adamance)

internal class MonsterMushroom : Mushroom
{
    [SerializeField] GameObject radarIconObj = null!;

    NavMeshAgent agent = null!;
    Animator animator = null!;
    AudioSource audioSource = null!;

    PlayerControllerB? targetPlayer;

    float timeSinceAIInterval;
    float timeHeld;

    bool isDead;
    int hp = 2;
    bool dormant = true;
    float currentSpeed;
    Vector3 lastPosition;
    float idleTime;

    int hashSpeed;

    const float aggroRange = 10f;

    public void Awake()
    {
        hashSpeed = Animator.StringToHash("speed");
        agent = gameObject.GetComponent<NavMeshAgent>();
        animator = gameObject.GetComponent<Animator>();
        audioSource = gameObject.GetComponent<AudioSource>();
    }

    public override void Update()
    {
        timeHeld = playerHeldBy != null ? timeHeld + Time.deltaTime : 0;

        if (playerHeldBy == null && !isDead && !dormant && hasHitGround)
        {
            agent.enabled = true;
            targetFloorPosition = transform.position;
            timeSinceAIInterval += Time.deltaTime;

            if (timeSinceAIInterval > 0.2f)
            {
                timeSinceAIInterval = 0f;
                DoAIInterval();
            }
        }
        else
        {
            agent.enabled = false;
            base.Update();
        }
    }

    public void DoAIInterval()
    {
        targetPlayer = StartOfRound.Instance.allPlayerScripts.Where(x => x.isPlayerControlled && x.sinkingValue < 0.73f).OrderByDistance(transform.position, (player) => player.transform.position).FirstOrDefault();
        if (targetPlayer == null || Vector3.Distance(targetPlayer.transform.position, transform.position) > aggroRange)
        {
            targetPlayer = null;
        }
    }

    public override void LateUpdate()
    {
        base.LateUpdate();

        currentSpeed = Mathf.Clamp((transform.position - lastPosition).magnitude / Time.deltaTime / 2, 0f, 1.5f);
        lastPosition = transform.position;
        animator.SetFloat(hashSpeed, currentSpeed);

        idleTime = currentSpeed > 0.1f ? 0f : idleTime + Time.deltaTime;
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

    public void OnHit(int force, PlayerControllerB playerWhoHit)
    {
        if (playerHeldBy != null || playerWhoHit != localPlayer) { return; }
        OnHitRpc(force, playerWhoHit.actualClientId);
    }

    public void OnCollideWithPlayer(Collider other)
    {
        if (other.)
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void SetAwakeRpc()
    {
        dormant = false;

    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void SetDormantRpc()
    {
        dormant = value;
        agent.enabled = !dormant;
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void DoAnimationRpc(string animationName)
    {
        animator.SetTrigger(animationName);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void OnHitRpc(int force, ulong playerWhoHitId)
    {
        hp -= force;

        if (hp <= 0)
        {
            isDead = true;

        }
    }
}

internal class MonsterMushroomCollisionDetect : MonoBehaviour, IHittable
{
    [SerializeField] MonsterMushroom mainScript = null!;

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) { return; }
        mainScript.OnCollideWithPlayer(other);
    }

    bool IHittable.Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit, bool playHitSFX, int hitID)
    {
        if (playerWhoHit == null) { return false; }
        mainScript.OnHit(force, playerWhoHit);
        return true;
    }
}