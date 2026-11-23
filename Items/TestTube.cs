using Dawn;
using Dawn.Interfaces;
using GameNetcodeStuff;
using Newtonsoft.Json.Linq;
using SnowyCraftingCore;
using SnowyLib;
using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items
{
    internal class TestTube : PhysicsProp, IAnalyzableIngredient, IMixableIngredient, IDawnSaveData, IDiseaseStorable // TODO: Test analyzable ingredient to make sure its synced between all clients
    {
        public static bool IsEnabled => LethalContent.Items[LethalDiseasesKeys.TestTube] != null;

        public MeshRenderer fluidRenderer = null!;

        ScanNodeProperties scanNode = null!;

        public string StoredDisease { get; set; } = "";

        public bool IsFilled => !string.IsNullOrWhiteSpace(StoredDisease);

        [StaticInit]
        public static void Init()
        {
            var testTubeIngredient = new ChemistryIngredient(LethalDiseasesKeys.TestTube);
            ChemistryIngredient.Register(testTubeIngredient);

            ChemistryRecipe.Register(testTubeIngredient, testTubeIngredient, (ingredientA, ingredientB) =>
            {
                Disease? disease1 = Disease.GetDiseaseFromString(ingredientA.specialInstructions);
                Disease? disease2 = Disease.GetDiseaseFromString(ingredientB.specialInstructions);

                if (disease1 != null && disease2 != null)
                {
                    var array1 = disease1.symptoms;
                    var array2 = disease2.symptoms;

                    var result = array1.Except(array2).Concat(array2.Except(array1)).ToList();
                    Disease disease3 = Disease.MergeDiseases(disease1, disease2);
                    disease3.symptoms = result.ToHashSet();
                    return new ChemistryIngredient(LethalDiseasesKeys.TestTube, disease3.GetChemistryLiquidAppearance(), disease3.ToString());
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
                    return null; // TODO
                }
            });
        }

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0.05f, 0.05f, 0.01f);
            itemProperties.rotationOffset = new Vector3(0, 10, 0);
            itemProperties.floorYOffset = 90;
            scanNode = gameObject.GetComponentInChildren<ScanNodeProperties>();
        }

        public override void Update()
        {
            grabbable = localPlayer.currentlyHeldObjectServer is not IChemistryOutputContainer;
            customGrabTooltip = localPlayer.currentlyHeldObjectServer is IChemistryOutputContainer ? $"Fill {localPlayer.currentlyHeldObjectServer.itemProperties.itemName} : [E]" : "";
            
            if (!string.IsNullOrWhiteSpace(StoredDisease))
            {
                scanNode.subText = Disease.GetNameFromString(StoredDisease);
            }

            base.Update();
        }

        public override void InteractItem()
        {
            if (localPlayer.currentlyHeldObjectServer is not IChemistryOutputContainer) { return; }
            FillOutputContainerRpc(localPlayer.actualClientId);
        }

        public void SetFluidColor(ChemistryLiquidAppearance color)
        {
            logger.LogDebug("Setting fluid color to " + color.ToString());
            fluidRenderer.enabled = true;
            fluidRenderer.material.color = color.liquidColor;
            fluidRenderer.material.SetColor("_EmissiveColor", color.liquidColor);
            fluidRenderer.material.SetFloat("_EmissiveIntensity", color.emissionIntensity);
        }

        public override void EnableItemMeshes(bool enable)
        {
            base.EnableItemMeshes(enable);
            fluidRenderer.enabled = IsFilled && enable;
        }

        public ChemistryIngredient GetIngredient()
        {
            return new ChemistryIngredient(LethalDiseasesKeys.TestTube, new ChemistryLiquidAppearance(fluidRenderer.material.color, fluidRenderer.material.GetFloat("_EmissiveIntensity")), StoredDisease);
        }

        public void OnChemicalOutput(ChemistryIngredient ingredient)
        {
            SetDisease(ingredient.specialInstructions);
        }

        Action<AnalyzableIngredient, PlayerControllerB> IAnalyzableIngredient.OnAnalyze()
        {
            return static (ingredient, player) =>
            {
                Disease? disease = Disease.GetDiseaseFromString(ingredient.specialInstructions);
                if (disease == null) { HUDManager.Instance.DisplayTip("Analysis failed", "No diseases found", true); return; }

                disease.name = $"disease{Disease.namedDiseases.Count}";

                HUDManager.Instance.DisplayTip("Analysis complete", $"Results sent to terminal ({disease.name})");
            };
        }

        bool IMixableIngredient.DespawnItemAfterInput()
        {
            return true;
        }

        bool IAnalyzableIngredient.HoldItem()
        {
            return true;
        }

        public void SetDisease(string diseaseString)
        {
            logger.LogDebug($"Setting stored disease to disease with id: {diseaseString}");
            Disease? disease = Disease.GetDiseaseFromString(diseaseString);
            if (disease == null)
            {
                logger.LogDebug($"Unable to parse disease from id ({diseaseString})");
                SetFluidColor(new ChemistryLiquidAppearance(Color.black, 1f));
                scanNode.subText = "Toxic Sludge";
                return;
            }

            StoredDisease = diseaseString;

            SetFluidColor(disease.GetChemistryLiquidAppearance());
            return;
        }

        public JToken GetDawnDataToSave()
        {
            return JToken.FromObject(StoredDisease != null ? StoredDisease : "");
        }

        public void LoadDawnSaveData(JToken saveData)
        {
            StoredDisease = saveData.Value<string>();
            if (string.IsNullOrWhiteSpace(StoredDisease)) { return; }
            SetDisease(StoredDisease);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void SetDiseaseRpc(string diseaseId)
        {
            SetDisease(diseaseId);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void FillOutputContainerRpc(ulong clientId)
        {
            PlayerControllerB player = PlayerFromId(clientId);

            var outputContainer = localPlayer.currentlyHeldObjectServer as IChemistryOutputContainer;
            if (outputContainer == null) { return; }

            if (!outputContainer.ReceiveChemistryOutput(GetIngredient())) { return; }
            
            if (!IsServer) { return; }
            NetworkObject.Despawn(destroy: true);
        }
    }
}