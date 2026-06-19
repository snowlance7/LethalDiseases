using Dawn;
using Dawn.Utils;
using GameNetcodeStuff;
using SnowyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.Rendering.HighDefinition;
using static LethalDiseases.LethalDiseasesNetworkHandler;
using static LethalDiseases.Plugin;
using static SnowyLib.PlayerControllerBExtensions;

namespace LethalDiseases.Symptoms // TODO: Add accessibility features
{
    internal static partial class Symptoms
    {
        private static LethalDiseasesNetworkHandler networkHandler => LethalDiseasesNetworkHandler.Instance;

        internal static void Load()
        {
            Assembly myAssembly = Assembly.GetExecutingAssembly();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .Where(a =>
                {
                    try
                    {
                        return a == myAssembly ||
                               a.GetReferencedAssemblies()
                                .Any(r => r.FullName == myAssembly.FullName);
                    }
                    catch
                    {
                        return false;
                    }
                });

            var methods = assemblies
                .SelectMany(assembly =>
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        return ex.Types.Where(t => t != null)!;
                    }
                })
                .SelectMany(t => t.GetMethods(
                    BindingFlags.Static |
                    BindingFlags.Public |
                    BindingFlags.NonPublic));

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<SymptomAttribute>();
                if (attr == null)
                    continue;

                var parameters = method.GetParameters();

                if (parameters.Length != 1 ||
                    parameters[0].ParameterType != typeof(Disease) ||
                    method.ReturnType != typeof(StatusEffect))
                {
                    throw new Exception(
                        $"Invalid symptom factory signature: {method.DeclaringType?.FullName}.{method.Name}");
                }

                var factory = (Func<Disease, StatusEffect>)Delegate.CreateDelegate(
                    typeof(Func<Disease, StatusEffect>),
                    method);

                Symptom.RegisterSymptom(new Symptom(
                    attr.Name,
                    attr.Description,
                    attr.Type,
                    attr.Rarity,
                    factory
                ));
            }
        }

        public static GameObject? GetClosestActorToPosition(Vector3 position, float range = 0f)
        {
            float closestSqrDistance = range > 0f ? range * range : float.PositiveInfinity;
            GameObject? closestActor = null;

            void TryCandidate(GameObject? obj)
            {
                if (obj == null) return;

                Vector3 delta = obj.transform.position - position;
                float sqrDist = delta.sqrMagnitude;

                if (sqrDist > closestSqrDistance) return;

                closestSqrDistance = sqrDist;
                closestActor = obj;
            }

            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player != null && player.isPlayerControlled)
                    TryCandidate(player.gameObject);
            }

            foreach (var enemy in RoundManager.Instance.SpawnedEnemies)
            {
                if (enemy != null && !enemy.isEnemyDead)
                    TryCandidate(enemy.gameObject);
            }

            return closestActor;
        }

        public static List<GameObject> GetActorsInRange(Vector3 position, float range)
        {
            List<GameObject> actors = new List<GameObject>();

            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player == null || !player.isPlayerControlled) { continue; }
                Vector3 delta = player.transform.position - position;
                float sqrDist = delta.sqrMagnitude;
                if (sqrDist > range) { continue; }
                actors.Add(player.gameObject);
            }

            foreach (var enemy in RoundManager.Instance.SpawnedEnemies)
            {
                if (enemy == null || enemy.isEnemyDead) { continue; }
                Vector3 delta = enemy.transform.position - position;
                float sqrDist = delta.sqrMagnitude;
                if (sqrDist > range) { continue; }
                actors.Add(enemy.gameObject);
            }

            return actors;
        }

        public static StatusEffectController.ConflictResult SetHighestDurationAndDeny(StatusEffect existing, StatusEffect incoming)
        {
            existing.duration = Mathf.Max(existing.duration, incoming.duration);
            return StatusEffectController.ConflictResult.Deny;
        }

        [Symptom("Broken Legs", "You are always limping", Symptom.SymptomType.Bad, 100)]
        public static StatusEffect BrokenLegs(Disease disease)
        {
            return new TickActionEffect(() =>
            {
                if (disease.player == null) { return; }
                disease.player.criticallyInjured = true;
                disease.player.playerBodyAnimator.SetBool("Limp", value: true);
            }, disease.id, "Broken Legs", disease.strengthTime, SetHighestDurationAndDeny, onRemove: (effect) =>
            {
                if (disease.player == null) { return; }
                disease.player.criticallyInjured = false;
                disease.player.playerBodyAnimator.SetBool("Limp", value: false);
            });
        }

        [Symptom("Clumsiness", "Randomly trip", Symptom.SymptomType.Bad, 100)]
        public static StatusEffect Clumsiness(Disease disease)
        {
            return new RandomIntervalActionEffect(new BoundedRange(15, 120), () =>
            {
                if (disease.player == null) { return; }
                disease.player.PlayQuickSpecialAnimation(1.5f);
                disease.player.playerBodyAnimator.SetTrigger("SA_PushLeverBack");
            }, disease.id, "Clumsiness", disease.strengthTime, SetHighestDurationAndDeny);
        }

        [Symptom("Buttery Fingers", "Periodically drop what you're holding", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect ButteryFingers(Disease disease)
        {
            return new RandomIntervalActionEffect(new BoundedRange(60, 120), () =>
            {
                if (disease.player == null) { return; }
                disease.player.DiscardHeldObject();
            }, disease.id, "Buttery Fingers", disease.strengthTime, SetHighestDurationAndDeny);
        }

        [Symptom("Infested", "Periodically spawn hoarderbugs around you", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Infested(Disease disease)
        {
            return new RandomIntervalActionEffect(new BoundedRange(30, 300), () =>
            {
                if (disease.player == null) { return; }
                if (!disease.player.isInsideFactory || UnityEngine.Random.Range(0, 2) == 1) { return; }
                LethalDiseasesNetworkHandler.Instance.SpawnEnemyServerRpc(EnemyKeys.Hoardingbug, disease.player.transform.position - disease.player.transform.forward);
            }, disease.id, "Infested", disease.strengthTime, SetHighestDurationAndDeny);
        }

        [Symptom("Congested", "Your voice is muffled", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Congested(Disease disease)
        {
            if (disease.player != null)
                LethalDiseasesNetworkHandler.Instance.MufflePlayerServerRpc(disease.player.actualClientId, true);

            return new OnRemoveActionEffect(() =>
            {
                if (disease.player == null) { return; }
                LethalDiseasesNetworkHandler.Instance.MufflePlayerServerRpc(disease.player.actualClientId, false);
            }, disease.id, "Congested", disease.strengthTime, SetHighestDurationAndDeny);
        }

        [Symptom("Smoker Lungs", "You cough randomly, making noise", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect SmokerLungs(Disease disease)
        {
            return new RandomIntervalActionEffect(new BoundedRange(15, 60), () =>
            {
                if (disease.player == null) { return; }
                LethalDiseasesNetworkHandler.Instance.PlaySoundEffectServerRpc(disease.player.actualClientId, LethalDiseasesNetworkHandler.SoundEffect.Cough, volume: 0.7f, cutoffFrequency: 1500);
                if (disease.transmissionType == Disease.TransmissionType.Airborne)
                    disease.TrySpreadAirborne(disease.player.gameplayCamera.transform.position, 1.5f);
            }, disease.id, "Smoker Lungs", disease.strengthTime, SetHighestDurationAndDeny);
        }

        [Symptom("Trigger Finger", "Randomly use whatever youre holding (left mouse click)", Symptom.SymptomType.Bad, 100)]
        public static StatusEffect TriggerFinger(Disease disease)
        {
            return new RandomIntervalActionEffect(new BoundedRange(30, 60), () =>
            {
                if (disease.player == null) { return; }
                if (!disease.player.CanUseItem()) { return; }
                if (UnityEngine.Random.Range(0, 2) == 0) { return; }
                disease.player.currentlyHeldObjectServer?.UseItemOnClient(buttonDown: true);
            }, disease.id, "Trigger Finger", disease.strengthTime, SetHighestDurationAndDeny);
        }

        [Symptom("Mucus Filled", "Periodically spit mucus on other player, which can spread the disease", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect MucusFilled(Disease disease)
        {
            return new RandomIntervalActionEffect(new BoundedRange(30, 120), () =>
            {
                if (disease.player == null) { return; }
                List<ulong> clientIds = new List<ulong>();
                Collider[] collided = [];
                int collidersHitCount = Physics.OverlapCapsuleNonAlloc(disease.player.gameplayCamera.transform.position, disease.player.gameplayCamera.transform.position + disease.player.gameplayCamera.transform.forward * 5f, 2.5f, collided, StartOfRound.Instance.playersMask, QueryTriggerInteraction.Ignore);
                if (collidersHitCount <= 0) { return; }
                foreach (var playerCollider in collided)
                {
                    PlayerControllerB player = playerCollider.gameObject.GetComponent<PlayerControllerB>();
                    if (player == null) continue;
                    clientIds.Add(player.actualClientId);
                }
                LethalDiseasesNetworkHandler.Instance.SlimePlayersServerRpc(clientIds.ToArray(), disease.id); // TODO: Test this
            }, disease.id, "Mucus Filled", disease.strengthTime, SetHighestDurationAndDeny);
        }

        [Symptom("Lightheaded", "Get dizzy when running too much", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Lightheaded(Disease disease)
        {
            return new ConditionalActionEffect(() => disease.player?.sprintMeter < 0.5f, () =>
            {
                if (disease.player == null) { return; }
                disease.player.drunkness = Mathf.Max(disease.player.drunkness, 0.4f);
            }, false, disease.id, 0, 0, "Lightheaded", disease.strengthTime, SetHighestDurationAndDeny);
        }

        [Symptom("Nausea", "Get the TZP effect periodically and throw up if moving too much during it", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Nausea(Disease disease)
        {
            float timeSinceStart = 0;
            float timeToMaxNausea = 0;
            bool playerThrewUp = false;

            return new RandomIntervalPhaseActionEffect(new BoundedRange(60f, 500f), new BoundedRange(15f, 30f), onStartAction: (phaseTimer) =>
            {
                if (disease.player == null) { return; }
                timeSinceStart = 0;
                timeToMaxNausea = phaseTimer / 4;
                playerThrewUp = false;
            }, tickAction: (phaseTimer) =>
            {
                if (disease.player == null) { return; }
                if (playerThrewUp) { return; }

                timeSinceStart += Time.deltaTime;

                if (timeSinceStart < timeToMaxNausea)
                {
                    float normalizedTime = timeSinceStart / timeToMaxNausea;
                    disease.player.drunkness = Mathf.Lerp(0f, 1f, normalizedTime);
                }
                else
                {
                    disease.player.drunkness = 1f;
                }

                if (disease.player.drunkness > 0.5f && disease.player.isSprinting)
                {
                    disease.player.PlayQuickSpecialAnimation(1f);
                    disease.player.playerBodyAnimator.SetTrigger("SpawnPlayer");
                    networkHandler.PlacePukeDecalServerRpc(disease.player.transform.position + disease.player.transform.up, Vector3.down);
                    playerThrewUp = true;
                }
            }, onEndAction: () => { }, disease.id, "Nausea", disease.strengthTime, SetHighestDurationAndDeny);
        }

        [Symptom("Klepto", "Randomly and periodically take items from nearby players", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Klepto(Disease disease)
        {
            return new RandomIntervalActionEffect(new BoundedRange(30f, 120f), () =>
            {
                PlayerControllerB? player = StartOfRound.Instance.allPlayerScripts.Where(x => x.isPlayerControlled).GetClosestToPosition(localPlayer.transform.position, (x) => x.transform.position);
                if (player == null) { return; }
                GrabbableObject? grabbable = player.currentlyHeldObjectServer;
                if (grabbable == null || localPlayer.FirstEmptyItemSlot(grabbable) == -1) { return; }
                networkHandler.PlayerDiscardHeldObjectServerRpc(player.actualClientId);
                localPlayer.GrabGrabbableObject(grabbable);
            }, disease.id, "Klepto", disease.strengthTime, SetHighestDurationAndDeny);
        }

        [Symptom("Cursed", "Spawns a ghost girl that targets you", Symptom.SymptomType.Bad, 1)] // Spawn ghost girl
        public static StatusEffect Cursed(Disease disease)
        {
            return new IntervalActionEffect(10f, () =>
            {
                if (disease.player == null || StartOfRound.Instance.inShipPhase) { return; }
                if (RoundManager.Instance.SpawnedEnemies.Any(x => x is DressGirlAI girl && girl.hauntingPlayer == disease.player)) { return; }
                networkHandler.SpawnGhostGirlServerRpc(disease.player.actualClientId);
            }, disease.id, "Cursed", disease.strengthTime, SetHighestDurationAndDeny);
        }

        [Symptom("Aquaphobia", "You fear water", Symptom.SymptomType.Neutral, 50)]
        public static StatusEffect Aquaphobia(Disease disease)
        {
            LayerMask mask = LayerMask.GetMask("Water");

            return new IntervalActionEffect(0.2f, () =>
            {
                PlayerControllerB? player = disease.player;
                if (player == null) { return; }
                if (player.isInsideFactory || !Physics.Raycast(player.gameplayCamera.transform.position, player.gameplayCamera.transform.forward, 100f, mask)) { return; }
                player.JumpToFearLevel(1f);
            }, disease.id, "Aquaphobia", disease.strengthTime, SetHighestDurationAndDeny);
        }

        [Symptom("Paranoia", "You hear things you shouldnt", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Paranoia(Disease disease)
        {
            return new RandomIntervalActionEffect(new BoundedRange(15f, 120f), () =>
            {
                if (disease.player == null) { return; }
                var clips = networkHandler.soundEffects[(int)SoundEffect.Paranoia].clips;
                var pos = RoundManager.Instance.GetRandomPositionInRadius(localPlayer.transform.position, 1f, 15f);
                Utils.PlaySoundAtPosition(pos, clips, max3DDistance: 20);
            }, disease.id, "Paranoia", disease.strengthTime, SetHighestDurationAndDeny);
        }

        [Symptom("Necrosis", "Randomly take damage until death", Symptom.SymptomType.Bad, 50)] // TODO: Body parts that fall off and you can sell
        public static StatusEffect Necrosis(Disease disease)
        {
            if (disease.player != null)
            {
                return new RandomIntervalActionEffect(new BoundedRange(15f, 60f), () =>
                {
                    disease.player.DamagePlayer(1);
                }, disease.id, "Necrosis", disease.strengthTime, SetHighestDurationAndDeny);
            }
            else
            {
                return new RandomIntervalActionEffect(new BoundedRange(30f, 90f), () =>
                {
                    if (disease.enemy != null)
                    {
                        disease.enemy.HitEnemyOnLocalClient(1);
                    }
                }, disease.id, "Necrosis", disease.strengthTime, SetHighestDurationAndDeny);
            }
        }

        [Symptom("Burnt Lungs", "1.5x stamina usage", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect BurntLungs(Disease disease)
        {
            return new TickActionEffect(() =>
            {
                if (disease.player == null) { return; }
                disease.player.sprintMeter = Mathf.Clamp(disease.player.sprintMeter, 0, 0.75f);
            }, disease.id, "Burnt Lungs", disease.strengthTime, SetHighestDurationAndDeny);
        }


        [Symptom("Anitidaephobia", "You hallucinate duck sounds", Symptom.SymptomType.Neutral, 50)]
        public static StatusEffect Anitidaephobia(Disease disease)
        {
            return new RandomIntervalActionEffect(new BoundedRange(30f, 120f), () =>
            {
                if (disease.player == null) { return; }
                var clips = networkHandler.soundEffects[(int)SoundEffect.DuckSounds].clips;
                var pos = RoundManager.Instance.GetRandomPositionInRadius(localPlayer.transform.position, 1f, 15f);
                Utils.PlaySoundAtPosition(pos, clips, max3DDistance: 20);
            }, disease.id, "Anitidaephobia", disease.strengthTime, SetHighestDurationAndDeny);
        }

        [Symptom("Anarchist", "Randomly summon landmines in front of you", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Anarchist(Disease disease)
        {
            return new RandomIntervalActionEffect(new BoundedRange(120f, 450f), () =>
            {
                if (disease.hasActor)
                    networkHandler.SpawnMapObjectServerRpc(MapObjectKeys.Landmine, disease.networkObject.gameObject.transform.position + disease.networkObject.gameObject.transform.forward * 2);
            }, disease.id, "Anarchist", disease.strengthTime, SetHighestDurationAndDeny);
        }

        [Symptom("IBS", "You explode on death", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect IBS(Disease disease)
        {
            return new OnRemoveActionEffect(() =>
            {
                if (disease.player != null && disease.player.isPlayerDead)
                    networkHandler.SpawnExplosionServerRpc(disease.player.transform.position, true);
                if (disease.enemy != null && disease.enemy.isEnemyDead)
                    networkHandler.SpawnExplosionServerRpc(disease.enemy.transform.position, true);
            }, disease.id, "IBS", disease.strengthTime, SetHighestDurationAndDeny);
        }

        [Symptom("Dementia", "Randomly teleport to a location periodically", Symptom.SymptomType.Bad, 50)] // TODO: Make them drop an item before teleporting
        public static StatusEffect Dementia(Disease disease)
        {
            return new RandomIntervalActionEffect(new BoundedRange(60, 500), () =>
            {
                if (disease.player == null) { return; }
                GameObject? node = Utils.allAINodes.GetRandom(Utils.randomLocal);
                if (node == null) { return; }
                disease.player.TeleportPlayer(node.transform.position);
            }, disease.id, "Dementia", disease.strengthTime, SetHighestDurationAndDeny);
        }

        [Symptom("Claustrophobia", "You fear being inside", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Claustrophobia(Disease disease)
        {
            return new TickActionEffect(() =>
            {
                if (localPlayer.isInsideFactory)
                    localPlayer.JumpToFearLevel(1f);
            }, disease.id, "Claustrophobia", disease.strengthTime, SetHighestDurationAndDeny);
        }

        [Symptom("MAD", "You kill the nearest living thing on death", Symptom.SymptomType.Neutral, 50)]
        public static StatusEffect MAD(Disease disease)
        {
            return new OnRemoveActionEffect(() =>
            {
                if ((disease.player != null && disease.player.isPlayerDead) || (disease.enemy != null && disease.enemy.isEnemyDead)) // TODO: Simplify this
                {
                    GameObject? closestObj = GetClosestActorToPosition(disease.networkObject.transform.position);
                    if (closestObj == null) { return; }
                    if (closestObj.TryGetComponent(out EnemyAI enemy))
                    {
                        enemy.KillEnemyServerRpc(false); // TODO: Test
                    }
                    if (closestObj.TryGetComponent(out PlayerControllerB player))
                    {
                        player.KillPlayerServerRpc(-1, true, Vector3.zero, 0, 0, Vector3.zero, false); // TODO: Test
                    }
                }
            }, disease.id, "IBS", disease.strengthTime, SetHighestDurationAndDeny);
        }

        [Symptom("Split Personality", "Randomly swap positions with a random player periodically", Symptom.SymptomType.Bad, 10)]
        public static StatusEffect SplitPersonality(Disease disease)
        {
            return new RandomIntervalActionEffect(new BoundedRange(60, 500), () =>
            {
                if (disease.player != null)
                {
                    PlayerControllerB? randomPlayer = StartOfRound.Instance.allPlayerScripts.Where(x => x.isPlayerControlled).GetRandom();
                    if (randomPlayer == null) { return; }
                    Vector3 playerPosition = disease.player.transform.position;
                    disease.player.TeleportPlayer(randomPlayer.transform.position);
                    networkHandler.TeleportPlayerServerRpc(randomPlayer.actualClientId, playerPosition); // TODO: Test
                }
            }, disease.id, "Split Personality", disease.strengthTime, SetHighestDurationAndDeny);
        }

        [Symptom("Narcissism", "Everyone is invisible to you", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Narcissism(Disease disease)
        {
            if (disease.player != null && disease.player == localPlayer)
            {
                logger?.LogDebug("Narcissism: Making players invisible");
                foreach (var player in StartOfRound.Instance.allPlayerScripts)
                {
                    if (player == null || !player.isPlayerControlled || player == localPlayer) { continue; }
                    player.MakePlayerInvisible(true);
                }
            }
            return new OnRemoveActionEffect(() =>
            {
                if (disease.player != null && disease.player == localPlayer)
                {
                    foreach (var player in StartOfRound.Instance.allPlayerScripts)
                    {
                        if (player == null || player == localPlayer) { continue; }
                        player.MakePlayerInvisible(false);
                    }
                }
            }, disease.id, "Narcissism", disease.strengthTime, SetHighestDurationAndDeny);
        }

        //[Symptom("Arachnophobia", "Spiders instakill you", Symptom.SymptomType.Bad, 50)]
        //public static StatusEffect Arachnophobia(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}

        //[Symptom("Crazy", "I was crazy once, they put me in a room", Symptom.SymptomType.Bad, 50)] // Everyone is wearing masks
        //public static StatusEffect Crazy(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}

        //[Symptom("Fresh Scent", "If circuit bees are triggered, they will hunt you down", Symptom.SymptomType.Bad, 50)]
        //public static StatusEffect FreshScent(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}

        //[Symptom("Egg Face", "Sapsuckers attack on sight", Symptom.SymptomType.Bad, 50)]
        //public static StatusEffect EggFace(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}

        //[Symptom("Knee Water", "Randomly get the quicksand effect", Symptom.SymptomType.Bad, 50)]
        //public static StatusEffect KneeWater(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}

        //[Symptom("Charlie Horse", "You randomly make horse noises", Symptom.SymptomType.Neutral, 50)]
        //public static StatusEffect CharlieHorse(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}

        //[Symptom("Bald", "Looking at the infected player for too long will trigger a flashbang", Symptom.SymptomType.Bad, 50)]
        //public static StatusEffect Bald(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}

        //[Symptom("Alarm", "You randomly make alarm sounds and attract enemies", Symptom.SymptomType.Bad, 50)]
        //public static StatusEffect Alarm(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}

        //[Symptom("Vampirism", "Leech health from other living things", Symptom.SymptomType.Bad, 50)]
        //public static StatusEffect Vampirism(Disease disease)
        //{
        //    throw new NotImplementedException();
        //    return new IntervalActionEffect(2.5f, () =>
        //    {
        //        if (disease.player != null)
        //        {
        //            if (disease.player.health >= 100) { return; }
        //            GameObject? leechObj = GetClosestActorToPosition(disease.player.transform.position, 5f);
        //            if (leechObj == null) { return; }
        //            if (leechObj.TryGetComponent(out PlayerControllerB player))
        //            {
        //                //player.DamagePlayerServerRpc(1, player.health - 1)
        //            }
        //        }
        //        if (disease.enemy != null)
        //        {

        //        }
        //    }, disease.id, "", disease.strengthTime, SetHighestDurationAndDeny);
        //}

        //[Symptom("Mute", "Cant use comms to speak", Symptom.SymptomType.Bad, 50)]
        //public static StatusEffect Mute(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}

        //[Symptom("Paragon", "Gain every other negative effect but cant die", Symptom.SymptomType.Bad, 1)]
        //public static StatusEffect Paragon(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}

        //[Symptom("Bad Gas", "Slowly gain the TZP effect when inside and fart randomly", Symptom.SymptomType.Bad, 50)]
        //public static StatusEffect BadGas(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}

        //[Symptom("Heartworm", "Spawns a earth leviathon on your location on death", Symptom.SymptomType.Bad, 50)]
        //public static StatusEffect Heartworm(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}

        //[Symptom("Selective Hearing", "You cant hear certain players", Symptom.SymptomType.Bad, 50)]
        //public static StatusEffect SelectiveHearing(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}

        //[Symptom("Norovirus", "You explode upon death, can chain to other players", Symptom.SymptomType.Bad, 50)]
        //public static StatusEffect Norovirus(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}

        //[Symptom("Mood Swings", "Randomly increases/decreases random stats every 2 ingame hours", Symptom.SymptomType.Neutral, 50)]
        //public static StatusEffect MoodSwings(Disease disease)
        //{
        //    /*Mood Swings -Randomly increases and/ or decreases each of your movement factors every 2 ingame hours.
        //    This includes:
        //    Sprint Stamina
        //    Jump Stamina
        //    Walk Speed
        //    Critical Speed
        //    Crouch Speed
        //    Sprint Speed
        //    Water Speed
        //    Jump Height
        //    Item Weight Influence*/
        //    throw new NotImplementedException();
        //}

        //[Symptom("Sterile", "Picking up a baby maneater instantly kills it", Symptom.SymptomType.Good, 50)]
        //public static StatusEffect Sterile(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}

        //[Symptom("Baculovirus", "", Symptom.SymptomType.Bad, 50)]
        //public static StatusEffect Baculovirus(Disease disease)
        //{
        //    // baculovirus. It makes caterpillars explode to infect other caterpillars. (By making them go high to the light but going to ship might be easier for coding reasons.) Not sure how hard/bad that would be but it'd be a fun zombie option
        //    throw new NotImplementedException();
        //}

        //[Symptom("Last Stand", "You kill the last thing that kills you", Symptom.SymptomType.Good, 50)]
        //public static StatusEffect LastStand(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}

        //[Symptom("Cold Fluu", "You sneeze periodically, sneezing on other players slows them down", Symptom.SymptomType.Bad, 50)]
        //public static StatusEffect ColdFluu(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}

        //[Symptom("Shroombiosis", "You grow shrooms on your body which heal players when eaten", Symptom.SymptomType.Good, 50)]
        //public static StatusEffect Shroombiosis(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}

        //[Symptom("Sponge Legs", "You make an annoying sound when you walk", Symptom.SymptomType.Neutral, 50)]
        //public static StatusEffect SpongeLegs(Disease disease)
        //{
        //    // spongebob walking noises
        //    throw new NotImplementedException();
        //}

        //[Symptom("Parasite Link", "You and another player with this symptom are chained together", Symptom.SymptomType.Bad, 50)]
        //public static StatusEffect ParasiteLink(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}

        //[Symptom("Parasite", "You are chained to another player", Symptom.SymptomType.Bad, 50)]
        //public static StatusEffect Parasite(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}

        //[Symptom("Tesla Coil", "Zap other players holding metal items", Symptom.SymptomType.Bad, 50)]
        //public static StatusEffect TeslaCoil(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}

        //[Symptom("Deja Vu", "Teleport back 10 seconds periodically", Symptom.SymptomType.Bad, 50)]
        //public static StatusEffect DejaVu(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}

        //[Symptom("Sproodling", "You become small", Symptom.SymptomType.Bad, 50)]
        //public static StatusEffect Sproodling(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}

        //[Symptom("Ionizing", "You periodically do damage to nearby players", Symptom.SymptomType.Bad, 50)]
        //public static StatusEffect Ionizing(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}

        //[Symptom("Mitosis", "Dying spawns two more of you with the same disease", Symptom.SymptomType.Bad, 50)]
        //public static StatusEffect Mitosis(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}

        //[Symptom("Lactose Intolerant", "Chance to explode on heal", Symptom.SymptomType.Bad, 50)]
        //public static StatusEffect LactoseIntolerant(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}

        //[Symptom("Empyema", "Leave a trail of slime when you move", Symptom.SymptomType.Neutral, 50)]
        //public static StatusEffect Empyema(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}

        //[Symptom("Bleeding", "Randomly bleed", Symptom.SymptomType.Bad, 50)]
        //public static StatusEffect Bleeding(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}

        //[Symptom("Daltonizm", "You see in black and white", Symptom.SymptomType.Neutral, 50)]
        //public static StatusEffect Daltonizm(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}

        //[Symptom("Repulsive", "Push everything away from you", Symptom.SymptomType.Bad, 50)]
        //public static StatusEffect Repulsive(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}

        //[Symptom("Attractive", "Attract things towards you", Symptom.SymptomType.Bad, 50)]
        //public static StatusEffect Attractive(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}

        //[Symptom("Stress Eating", "You randomly eat an inventory item", Symptom.SymptomType.Bad, 50)]
        //public static StatusEffect StressEating(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}

        //[Symptom("", "", Symptom.SymptomType.Bad, 50)]
        //public static StatusEffect Template(Disease disease)
        //{
        //    throw new NotImplementedException();
        //}
    }
}

/*
ShortFallLanding (Trigger) - coughing small motion
SpawnPlayer (Trigger) - puking
startCrouching (Trigger) - force crouch, specialanimation time for duration
Damage (Trigger) - hands in air
Overheat (Trigger) - hands in air lower
SA_Typing (Trigger) - puking motion, head forward?
SA_stopAnimation (Trigger)
SA_ChargeItem (Trigger) - hand out
SA_PushLeverBack (Trigger) - forces screen to middle and does quick animation
*/