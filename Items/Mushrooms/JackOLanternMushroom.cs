using System;
using static LethalDiseases.Plugin;
using UnityEngine;

namespace LethalDiseases.Items.Mushrooms;

internal class JackOLanternMushroom : Mushroom // Jack_O_Lantern_Mushroom: adamance. some species glow in the dark. (Can cause severe nausea, vomiting, abdominal cramps, and diarrhea)
{
    [SerializeField] MeshRenderer renderer = null!; // TODO: Set up glow in the dark effects

    public override void Update()
    {
        base.Update();

        // 650 - 1000
        if (!hasBeenHeld && (TimeOfDay.Instance.dayMode == DayMode.Sundown || TimeOfDay.Instance.dayMode == DayMode.Midnight))
        {
            SetGlow(Mathf.InverseLerp(650, 1000, )) // TODO
        }
        else
        {
            SetGlow(0f);
        }
    }

    public void SetGlow(float value)
    {
        renderer.material.SetFloat("_EmissiveIntensity", Mathf.Lerp(0, 0.5f, value));
    }

    public override void OnEatMushroom()
    {
        Disease disease = Disease.CreateRandomDiseaseWithSymptoms("Nausea", "Vomiting", "Cramping");
        localPlayer.InfectAndSync(disease);
    }
}