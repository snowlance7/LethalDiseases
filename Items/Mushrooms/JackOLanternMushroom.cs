using System;
using static LethalDiseases.Plugin;
using UnityEngine;

namespace LethalDiseases.Items.Mushrooms;

internal class JackOLanternMushroom : Mushroom // Jack_O_Lantern_Mushroom: adamance. some species glow in the dark. (Can cause severe nausea, vomiting, abdominal cramps, and diarrhea)
{
    [SerializeField] MeshRenderer renderer = null!; // TODO: Set up glow in the dark effects

    public override void OnEatMushroom()
    {
        Disease disease = Disease.CreateRandomDiseaseWithSymptoms("Nausea", "Vomiting", "Cramping");
        localPlayer.InfectAndSync(disease);
    }
}