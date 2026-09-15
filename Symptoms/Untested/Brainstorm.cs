using GameNetcodeStuff;
using HarmonyLib;
using SnowyLib;
using System;
using System.Linq;
using Unity.Netcode;
using static LethalDiseases.Plugin;
using static LethalDiseases.SymptomAffectedObjects;

namespace LethalDiseases.Symptoms
{
    internal static partial class Symptoms
    {
        [Symptom("Brainstorm", "You are only targeted by lightning strikes", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Brainstorm(Disease disease)
        {
            AddSymptomAffectedObject("Brainstorm", disease.networkObject);
            return new OnRemoveActionEffect((effect) =>
            {
                RemoveSymptomAffectedObject("Brainstorm", disease.networkObject);
            }, disease.ToString(), "Brainstorm", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }

    [HarmonyPatch]
    internal static class BrainstormSymptomPatches
    {
        [HarmonyPrefix, HarmonyPatch(typeof(RoundManager), nameof(RoundManager.ShowStaticElectricityWarningServerRpc))]
        static void RoundManager_ShowStaticElectricityWarningServerRpc_PreFix(ref NetworkObjectReference warningObject) // TODO: Test this
        {
            try
            {
                if (!IsServerOrHost) { return; }
                if (symptomAffectedObjects["Brainstorm"].Count <= 0) { return; }
                var objects = symptomAffectedObjects["Brainstorm"].Where(x =>
                {
                    if (x.gameObject.TryGetComponent(out PlayerControllerB player))
                    {
                        return !player.isInsideFactory;
                    }
                    if (x.gameObject.TryGetComponent(out EnemyAI enemy))
                    {
                        return enemy.isOutside;
                    }
                    if (x.gameObject.TryGetComponent(out GrabbableObject grabbableObject))
                    {
                        return !grabbableObject.isInFactory;
                    }
                    return false;
                });
                if (objects.Count() <= 0) { return; }
                var strikeObject = objects.GetRandom();
                warningObject = strikeObject;
            }
            catch (Exception e)
            {
                logger.LogError(e);
                return;
            }
        }
    }
}
