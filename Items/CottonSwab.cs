using Dawn;
using SnowyCraftingCore;
using SnowyLib;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items;

internal class CottonSwab : DiseaseStorable, IDistillableIngredient
{
    public static bool IsEnabled => LethalContent.Items[LethalDiseasesKeys.CottonSwab] != null;

    readonly float maxDistance = 5f;

    [StaticInit]
    public static void Init()
    {
        ChemistryIngredient.Register(LethalDiseasesKeys.CottonSwab);
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (!buttonDown || IsFilled) { return; }

        localPlayer.playerBodyAnimator.SetTrigger("Throw");

        if (!Physics.Raycast(playerHeldBy.gameplayCamera.transform.position + playerHeldBy.gameplayCamera.transform.forward, playerHeldBy.gameplayCamera.transform.forward, out RaycastHit hitInfo, maxDistance, PluginInstance.playerEnemiesPropsMask)) { return; }

        logger.LogDebug(hitInfo.collider.gameObject.name);

        if (!hitInfo.collider.gameObject.TryGetComponentInChildren(out NetworkObject? netObj)) { return; } // TODO: Test this
        if (netObj == null) { return; }
        logger.LogDebug("Got network object: " + netObj.name);

        List<Disease> diseases = netObj.GetDiseases();
        if (diseases.Count == 0)
        {
            FillRpc("", new ChemistryLiquidAppearance(UnityEngine.Random.ColorHSV(), UnityEngine.Random.Range(0f, 10f)));
            return;
        }
        Disease disease = diseases.GetRandom();
        FillRpc(disease.ToString(), disease.GetChemistryLiquidAppearance());
    }

    public ChemistryIngredient? DistilleryOutput()
    {
        return new ChemistryIngredient(LethalDiseasesKeys.TestTube, FluidColor, StoredFluid);
    }

    public float DistilleryMixTime()
    {
        return 10f;
    }

    public bool DespawnItemAfterDistilleryInput()
    {
        return true;
    }

    public override bool AllowChemistryOutputContainer()
    {
        return false;
    }
}