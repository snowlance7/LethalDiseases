using Dawn;
using Dawn.Utils;
using GameNetcodeStuff;
using SnowyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static LethalDiseases.Plugin;
using static SnowyLib.PlayerControllerBExtensions;
using static LethalDiseases.NetworkHandler;

namespace LethalDiseases.Symptoms // TODO: Add accessibility features
{
    internal static partial class Symptoms
    {
        private static NetworkHandler networkHandler => NetworkHandler.Instance;
        private static AudioLibrary audioLibrary => LethalDiseasesContentHandler.Instance.DiseaseAssets!.AudioLibrary;

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