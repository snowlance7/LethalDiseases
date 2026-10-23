using SnowyCraftingCore;
using SnowyLib;
using Unity.Netcode;

namespace LethalDiseases.Items.Mushrooms;

internal abstract class Mushroom : AttachableObject, IMixableIngredient
{
    public bool DespawnItemAfterInput()
    {
        return true;
    }

    public abstract ChemistryIngredient? GetIngredient();

    public void OnChemicalOutput(ChemistryIngredient ingredient)
    {
        return;
    }

    public abstract void OnEatMushroom();

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (!buttonDown) { return; }

        OnEatMushroom();
        playerHeldBy.DespawnHeldObject();
    }
}