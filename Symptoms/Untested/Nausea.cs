using Dawn.Utils;
using GameNetcodeStuff;
using HarmonyLib;
using SnowyLib;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static LethalDiseases.Plugin;
using static LethalDiseases.SymptomAffectedObjects;

namespace LethalDiseases.Symptoms
{
    internal static partial class Symptoms
    {
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
                    networkHandler.PlacePukeDecalRpc(disease.player.transform.position + disease.player.transform.up, Vector3.down);
                    playerThrewUp = true;
                }
            }, onEndAction: () => { }, disease.ToString(), "Nausea", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }
}
