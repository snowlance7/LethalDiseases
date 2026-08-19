using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Unlockables
{
    internal class ChemistryStation : NetworkBehaviour
    {
        public static List<ChemistryIngredient> registeredIngredients = [];
        public static List<ChemistryRecipe> registeredRecipies = [];

        public InteractTrigger input1Trigger = null!;
        public InteractTrigger input2Trigger = null!;
        public InteractTrigger outputTrigger = null!;

        public MeshRenderer input1Renderer = null!;
        public MeshRenderer input2Renderer = null!;
        public MeshRenderer outputRenderer = null!;

        public AudioSource audioSource = null!;
        public Transform explosionPosition = null!;

        public Sprite mixIcon = null!;
        public Sprite handIcon = null!;

        Collider input1TriggerCollider = null!;
        Collider input2TriggerCollider = null!;
        Collider outputTriggerCollider = null!;

        ChemistryIngredient? input1Ingredient;
        ChemistryIngredient? input2Ingredient;
        ChemistryIngredient? outputIngredient;

        public void Awake()
        {
            input1TriggerCollider = input1Trigger.GetComponent<Collider>();
            input2TriggerCollider = input2Trigger.GetComponent<Collider>();
            outputTriggerCollider = outputTrigger.GetComponent<Collider>();
        }

        public void Update()
        {
            input1Trigger.enabled = input1Ingredient == null;
            input1Trigger.interactable = localPlayer.currentlyHeldObjectServer != null && !localPlayer.currentlyHeldObjectServer.itemProperties.twoHanded;
            
            input2Trigger.enabled = input2Ingredient == null;
            input2Trigger.interactable = localPlayer.currentlyHeldObjectServer != null && !localPlayer.currentlyHeldObjectServer.itemProperties.twoHanded;

            outputTrigger.enabled = (input1Ingredient != null && input2Ingredient != null) || outputIngredient != null;

            if (input1Ingredient != null && input2Ingredient != null && outputIngredient == null)
            {
                outputTrigger.hoverTip = "Mix [E]";
                outputTrigger.hoverIcon = mixIcon;
            }
            else if (outputIngredient != null)
            {
                outputTrigger.hoverTip = "Take Ingredient [E]";
                outputTrigger.hoverIcon = handIcon;
            }
        }

        public void Input1Trigger_Interact()
        {

        }

        public void Input2Trigger_Interact()
        {

        }

        public void OutputTrigger_Interact()
        {

        }

        public void SetFlaskColor(int flaskIndex, ChemistryLiquidAppearance color)
        {
            Material material = outputRenderer.materials[0];

            material.color = color.liquidColor;
            material.SetColor("_EmissionColor", color.emissionColor);
            material.SetFloat("_EmissionIntensity", color.emissionIntensity);

            switch (flaskIndex)
            {
                case 1:
                    input1Renderer.material = material;
                    break;
                case 2:
                    input2Renderer.material = material;
                    break;
                case 3:
                    outputRenderer.material = material;
                    break;
                default:
                    break;
            }
        }

        [Rpc(SendTo.Everyone)]
        public void InputIngredientRpc(NetworkObjectReference netRef, bool input1)
        {
            if (!netRef.TryGet(out NetworkObject netObj)) { return; }
            if (!netObj.TryGetComponent(out GrabbableObject item)) { return; }

            ChemistryIngredient? ingredient = registeredIngredients.Where(x => x.Item == item.itemProperties).FirstOrDefault();

            if (ingredient == null)
            {
                Color color = Random.ColorHSV();
                ingredient = new ChemistryIngredient(item.itemProperties, new ChemistryLiquidAppearance(color, color, 0));
            }

            if (input1)
            {
                input1Ingredient = ingredient;
                // TODO: Continue here
            }
            else
            {

            }
        }
    }

    public class ChemistryRecipe(IChemistryIngredient ingredientA, IChemistryIngredient ingredientB, IChemistryReaction reaction)
    {
        public IChemistryIngredient ingredientA = ingredientA;
        public IChemistryIngredient ingredientB = ingredientB;

        public IChemistryReaction reaction = reaction;
    }

    public class FixedOutputReaction(IChemistryIngredient output) : IChemistryReaction
    {
        public IChemistryIngredient React(IChemistryIngredient ingredientA, IChemistryIngredient ingredientB)
        {
            return output;
        }
    }

    public interface IChemistryReaction
    {
        IChemistryIngredient React(IChemistryIngredient ingredientA, IChemistryIngredient ingredientB);
    }

    public class ChemistryLiquidAppearance(Color liquidColor, Color emissionColor, float emissionIntensity)
    {
        public Color liquidColor = liquidColor;
        public Color emissionColor = emissionColor;
        public float emissionIntensity = emissionIntensity;
    }

    public interface IChemistryIngredient
    {
        public Item Item { get; }

        public ChemistryLiquidAppearance ChemistryLiquidAppearance { get; }
    }

    public class ChemistryIngredient(Item item, ChemistryLiquidAppearance chemistryLiquidAppearance) : IChemistryIngredient
    {
        public Item Item { get; } = item;

        public ChemistryLiquidAppearance ChemistryLiquidAppearance { get; } = chemistryLiquidAppearance;
    }
}
