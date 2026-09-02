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
        [Symptom("Anitidaephobia", "You hallucinate duck sounds", Symptom.SymptomType.Neutral, 50)]
        public static StatusEffect Anitidaephobia(Disease disease)
        {
            return new RandomIntervalActionEffect(new BoundedRange(30f, 120f), () =>
            {
                if (disease.player == null) { return; }
                var clips = audioLibrary.GetClips(SoundEffect.Duck.ToString());
                var pos = RoundManager.Instance.GetRandomPositionInRadius(localPlayer.transform.position, 1f, 15f);
                Utils.PlaySoundAtPosition(pos, clips, max3DDistance: 20);
            }, disease.ToString(), "Anitidaephobia", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }
}
