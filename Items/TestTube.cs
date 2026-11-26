using Dawn;
using GameNetcodeStuff;
using SnowyCraftingCore;
using SnowyLib;
using System;
using System.Linq;
using Unity.Netcode;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items
{
    internal class TestTube : DiseaseStorable, IAnalyzableIngredient, IMixableIngredient // TODO: Test analyzable ingredient to make sure its synced between all clients
    {
        public static bool IsEnabled => LethalContent.Items[LethalDiseasesKeys.TestTube] != null;

        [StaticInit]
        public static void Init()
        {
            var testTubeIngredient = new ChemistryIngredient(LethalDiseasesKeys.TestTube);

            ChemistryRecipe.Register(testTubeIngredient, testTubeIngredient, (ingredientA, ingredientB) =>
            {
                Disease? disease1 = Disease.GetDiseaseFromString(ingredientA.specialInstructions);
                Disease? disease2 = Disease.GetDiseaseFromString(ingredientB.specialInstructions);

                if (disease1 != null && disease2 != null)
                {
                    var array1 = disease1.symptoms;
                    var array2 = disease2.symptoms;

                    var result = array1.Except(array2).Concat(array2.Except(array1)).ToList();
                    Disease diseaseOutput = Disease.MergeDiseases(disease1, disease2);
                    diseaseOutput.symptoms = result.ToHashSet();
                    return new ChemistryIngredient(LethalDiseasesKeys.TestTube, diseaseOutput.GetChemistryLiquidAppearance(), diseaseOutput.ToString());
                }
                else if (disease1 != null)
                {
                    return ingredientA;
                }
                else if (disease2 != null)
                {
                    return ingredientB;
                }
                else
                {
                    return null;
                }
            });
        }

        public override void Update()
        {
            grabbable = localPlayer.currentlyHeldObjectServer is not IChemistryOutputContainer;
            customGrabTooltip = localPlayer.currentlyHeldObjectServer is IChemistryOutputContainer ? $"Fill {localPlayer.currentlyHeldObjectServer.itemProperties.itemName} : [E]" : "";
            
            base.Update();
        }

        public override void InteractItem()
        {
            if (localPlayer.currentlyHeldObjectServer is not IChemistryOutputContainer) { return; }
            FillOutputContainerRpc(localPlayer.actualClientId);
        }

        public Action<AnalyzableIngredient, PlayerControllerB> OnAnalyze()
        {
            return static (ingredient, player) =>
            {
                Disease? disease = Disease.GetDiseaseFromString(ingredient.specialInstructions);
                if (disease == null) { HUDManager.Instance.DisplayTip("Analysis failed", "No diseases found", true); return; }

                disease.name = $"disease{Disease.namedDiseases.Count}";

                HUDManager.Instance.DisplayTip("Analysis complete", $"Results sent to terminal ({disease.name})");
            };
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void FillOutputContainerRpc(ulong clientId)
        {
            PlayerControllerB player = PlayerFromId(clientId);

            var outputContainer = localPlayer.currentlyHeldObjectServer as IChemistryOutputContainer;
            if (outputContainer == null) { return; }

            var ingredient = GetIngredient();

            if (ingredient == null || !outputContainer.OnChemicalOutput(ingredient)) { return; }
            
            if (!IsServer) { return; }
            NetworkObject.Despawn(destroy: true);
        }

        public bool HoldItemDuringAnalysis()
        {
            return true;
        }

        public bool DespawnItemAfterChemicalMixerInput()
        {
            return true;
        }
    }
}