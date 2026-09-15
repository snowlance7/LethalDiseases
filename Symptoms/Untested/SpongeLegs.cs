using GameNetcodeStuff;
using HarmonyLib;
using SnowyLib;
using System;
using static LethalDiseases.NetworkHandler;
using static LethalDiseases.Plugin;
using static LethalDiseases.SymptomAffectedObjects;

namespace LethalDiseases.Symptoms
{
    internal static partial class Symptoms
    {
        [Symptom("SpongeLegs", "You make an annoying sound when you walk", Symptom.SymptomType.Neutral, 50)]
        public static StatusEffect SpongeLegs(Disease disease)
        {
            if (disease.player != null)
                AddSymptomAffectedObject("SpongeLegs", disease.player.NetworkObject);
            return new OnRemoveActionEffect((effect) =>
            {
                if (disease.player == null) { return; }
                RemoveSymptomAffectedObject("SpongeLegs", disease.player.NetworkObject);
            }, disease.ToString(), "SpongeLegs", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }

    [HarmonyPatch]
    internal static class SpongeLegsSymptomPatches
    {
        [HarmonyPrefix, HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.PlayFootstepSound))]
        static bool PlayerControllerB_PlayFootstepSound_PreFix(PlayerControllerB __instance)
        {
            try
            {
                if (symptomAffectedPlayers["SpongeLegs"].Count == 0 || !symptomAffectedPlayers["SpongeLegs"].Contains(__instance)) { return true; }

                var clips = LethalDiseasesContentHandler.Instance.DiseaseAssets!.AudioLibrary.GetClips(SoundEffect.Sponge.ToString());
                RoundManager.PlayRandomClip(__instance.movementAudio, clips);
                return false;
            }
            catch (Exception e)
            {
                logger.LogError(e);
                return true;
            }
        }
    }
}
