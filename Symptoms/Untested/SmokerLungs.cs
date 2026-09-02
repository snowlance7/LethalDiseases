using Dawn.Utils;
using GameNetcodeStuff;
using HarmonyLib;
using SnowyLib;
using System;
using System.Linq;
using UnityEngine.Rendering.HighDefinition;
using static LethalDiseases.NetworkHandler;
using static LethalDiseases.Plugin;
using static LethalDiseases.SymptomAffectedObjects;

namespace LethalDiseases.Symptoms
{
    internal static partial class Symptoms
    {
        [Symptom("Smoker Lungs", "You cough randomly, making noise", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect SmokerLungs(Disease disease)
        {
            return new RandomIntervalActionEffect(new BoundedRange(15, 60), () =>
            {
                if (disease.player == null) { return; }
                networkHandler.PlaySoundEffectRpc(disease.player.actualClientId, SoundEffect.Cough, volume: 0.7f, cutoffFrequency: 1500);
                if (disease.transmissionType == Disease.TransmissionType.Airborne)
                    disease.TrySpreadAirborne(disease.player.gameplayCamera.transform.position, 1.5f);
            }, disease.ToString(), "Smoker Lungs", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }
}
