using Dawn;
using GameNetcodeStuff;
using SnowyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Unlockables
{
    internal class ChemistryStation : NetworkBehaviour
    {
        public static List<ChemistryIngredient> registeredIngredients { get; private set; } = [];
        public static List<ChemistryRecipe> registeredRecipies { get; private set; } = [];

        public InteractTrigger input1Trigger = null!;
        public InteractTrigger input2Trigger = null!;
        public InteractTrigger outputTrigger = null!;

        public Collider input1TriggerCollider = null!;
        public Collider input2TriggerCollider = null!;
        public Collider outputTriggerCollider = null!;

        public MeshRenderer input1Renderer = null!;
        public MeshRenderer input2Renderer = null!;
        public MeshRenderer outputRenderer = null!;

        public ParticleSystem input1ParticleSystem = null!;
        public ParticleSystem input2ParticleSystem = null!;
        public ParticleSystem outputParticleSystem = null!;

        public AudioSource audioSource = null!;
        public Transform explosionPosition = null!;

        public Sprite mixIcon = null!;
        public Sprite handIcon = null!;

        ChemistryIngredient? input1Ingredient;
        ChemistryIngredient? input2Ingredient;
        ChemistryIngredient? outputIngredient;

        ChemistryRecipe? currentlyMixingRecipe;

        bool mixing;

        const float defaultMixingTime = 10f;

        public void RegisterRecipe(ChemistryRecipe recipe)
        {
            if (registeredRecipies.Contains(recipe)) { return; }
            if (registeredRecipies.Any(x => (x.ingredientA == recipe.ingredientA && x.ingredientB == recipe.ingredientB) || (x.ingredientA == recipe.ingredientB && x.ingredientB == recipe.ingredientA))) { logger.LogError($"Failed to register recipe {recipe}"); return; }
            // TODO
        }

        public void Awake()
        {
            input1TriggerCollider = input1Trigger.GetComponent<Collider>();
            input2TriggerCollider = input2Trigger.GetComponent<Collider>();
            outputTriggerCollider = outputTrigger.GetComponent<Collider>();
        }

        public void Update()
        {
            input1TriggerCollider.enabled = input1Ingredient == null && localPlayer.currentlyHeldObjectServer != null;
            //input1Trigger.interactable = localPlayer.currentlyHeldObjectServer != null && !localPlayer.currentlyHeldObjectServer.itemProperties.twoHanded;

            input2TriggerCollider.enabled = input2Ingredient == null && localPlayer.currentlyHeldObjectServer != null;
            //input2Trigger.interactable = localPlayer.currentlyHeldObjectServer != null && !localPlayer.currentlyHeldObjectServer.itemProperties.twoHanded;

            outputTriggerCollider.enabled = (input1Ingredient != null && input2Ingredient != null) || outputIngredient != null;

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
            if (localPlayer.currentlyHeldObjectServer == null || localPlayer.currentlyHeldObjectServer.itemProperties.twoHanded) { return; }
            logger.LogDebug("Input1Trigger_Interact");
            InputIngredientRpc(localPlayer.currentlyHeldObjectServer.NetworkObject, 1);
        }

        public void Input2Trigger_Interact()
        {
            if (localPlayer.currentlyHeldObjectServer == null || localPlayer.currentlyHeldObjectServer.itemProperties.twoHanded) { return; }
            logger.LogDebug("Input2Trigger_Interact");
            InputIngredientRpc(localPlayer.currentlyHeldObjectServer.NetworkObject, 2);
        }

        public void OutputTrigger_Interact()
        {
            if ((input1Ingredient == null || input2Ingredient == null) && outputIngredient == null) { return; }
            OutputTrigger_InteractRpc(localPlayer.actualClientId);
        }

        public void SetFlaskColor(int flaskIndex, ChemistryLiquidAppearance color)
        {
            Material material = new(outputRenderer.materials[0]);

            material.color = color.liquidColor;
            material.SetColor("_EmissionColor", color.liquidColor);
            material.SetFloat("_EmissionIntensity", color.emissionIntensity);

            switch (flaskIndex)
            {
                case 1:
                    input1Renderer.enabled = true;
                    input1Renderer.material = material;
                    break;
                case 2:
                    input2Renderer.enabled = true;
                    input2Renderer.material = material;
                    break;
                case 3:
                    outputRenderer.enabled = true;
                    outputRenderer.material = material;
                    break;
                default:
                    break;
            }
        }

        [Rpc(SendTo.Everyone)]
        public void InputIngredientRpc(NetworkObjectReference netRef, int flaskInputIndex)
        {
            if (!netRef.TryGet(out NetworkObject netObj)) { return; }
            if (!netObj.TryGetComponent(out GrabbableObject item)) { return; }

            ChemistryIngredient? ingredient = null;
            bool despawningIngredientItem = false;

            if (item is IChemistryIngredient _ingredient)
                ingredient = _ingredient.GetInputIngredient();

            if (ingredient == null)
                despawningIngredientItem = true;

            ingredient ??= registeredIngredients.Where(x => x.item == item.itemProperties).FirstOrDefault();

            if (ingredient == null)
            {
                Color color = UnityEngine.Random.ColorHSV();
                ingredient = new ChemistryIngredient(item.itemProperties, new ChemistryLiquidAppearance(color, 0));
            }

            if (flaskInputIndex == 1)
            {
                input1Ingredient = ingredient;
                SetFlaskColor(1, ingredient.chemistryLiquidAppearance);
            }
            else if (flaskInputIndex == 2)
            {
                input2Ingredient = ingredient;
                SetFlaskColor(2, ingredient.chemistryLiquidAppearance);
            }

            if (localPlayer == item.playerHeldBy && despawningIngredientItem)
                localPlayer.DespawnHeldObject();
        }

        [Rpc(SendTo.Everyone)]
        public void OutputTrigger_InteractRpc(ulong clientId)
        {
            if (((input1Ingredient == null || input2Ingredient == null) && outputIngredient == null) || mixing) { return; }

            if (input1Ingredient != null && input2Ingredient != null && outputIngredient == null) // Mixing
            {
                currentlyMixingRecipe = registeredRecipies.Where(x => (x.ingredientA == input1Ingredient && x.ingredientB == input2Ingredient) || (x.ingredientA == input2Ingredient && x.ingredientB == input1Ingredient)).FirstOrDefault();

                mixing = true;
                MixIngredients();
            }
            else if (outputIngredient != null) // Taking ingredient
            {
                PlayerControllerB? player = PlayerFromId(clientId);
                if (player == null) { return; }

                if (IsServer && !(player.currentlyHeldObjectServer != null && player.currentlyHeldObjectServer is IChemistryOutputContainer container && container.ReceiveChemistryOutput(outputIngredient)))
                {
                    GrabbableObject? outputItem = Utils.SpawnItem(outputIngredient!.item.GetDawnInfo().TypedKey, player.transform.position);
                    if (outputItem != null)
                    {
                        IEnumerator sendSpawnOutputIngredient()
                        {
                            yield return new WaitUntil(() => outputItem.NetworkObject != null && outputItem.NetworkObject.IsSpawned);
                            SpawnOutputIngredientRpc(clientId, outputItem.NetworkObject, outputIngredient.specialInstructions);
                        }

                        StartCoroutine(sendSpawnOutputIngredient());
                    }
                }

                outputParticleSystem.Stop();
                outputRenderer.enabled = false;
                outputIngredient = null;
            }
        }

        [Rpc(SendTo.Everyone)]
        public void SpawnOutputIngredientRpc(ulong clientId, NetworkObjectReference netRef, string specialInstructions)
        {
            if (!netRef.TryGet(out NetworkObject netObj)) { return; }
            if (!netObj.TryGetComponent(out GrabbableObject item)) { return; }

            if (item is IChemistryIngredient ingredient)
                ingredient.OnOutputIngredient(specialInstructions);

            if (localPlayer.actualClientId == clientId)
                localPlayer.GrabGrabbableObject(item);
        }

        void MixIngredients()
        {
            IEnumerator mixIngredients()
            {
                yield return null;

                // TODO: Mix animations and sounds here
                input1ParticleSystem.Play();
                input2ParticleSystem.Play();
                audioSource.Play();

                float mixTime = currentlyMixingRecipe != null && currentlyMixingRecipe.mixTime > 0 ? currentlyMixingRecipe.mixTime : defaultMixingTime;

                yield return new WaitForSeconds(mixTime);

                if (currentlyMixingRecipe != null)
                {
                    outputIngredient = currentlyMixingRecipe.reaction.Invoke(input1Ingredient!, input2Ingredient!);
                    SetFlaskColor(3, outputIngredient.chemistryLiquidAppearance);
                }
                else
                {
                    Landmine.SpawnExplosion(explosionPosition.position, true, killRange: 0, nonLethalDamage: 5, physicsForce: 5f);
                }

                input1ParticleSystem.Stop();
                input2ParticleSystem.Stop();
                outputParticleSystem.Play();
                input1Ingredient = null;
                input2Ingredient = null;
                input1Renderer.enabled = false;
                input2Renderer.enabled = false;
                mixing = false;
            }

            StartCoroutine(mixIngredients());
        }
    }

    public class ChemistryRecipe(ChemistryIngredient ingredientA, ChemistryIngredient ingredientB, Func<ChemistryIngredient, ChemistryIngredient, ChemistryIngredient> reaction, float mixTime = -1)
    {
        public ChemistryIngredient ingredientA = ingredientA;
        public ChemistryIngredient ingredientB = ingredientB;

        public Func<ChemistryIngredient, ChemistryIngredient, ChemistryIngredient> reaction = reaction;
        public float mixTime = mixTime;

        public override string ToString()
        {
            return ingredientA + "|" + ingredientB; // TODO: Set this up better
        }

        public override bool Equals(object obj)
        {
            throw new System.NotImplementedException(); // TODO
        }
    }

    public class ChemistryFixedOutputReaction(ChemistryIngredient ingredientA, ChemistryIngredient ingredientB, ChemistryIngredient output, float mixTime = -1) : ChemistryRecipe(ingredientA, ingredientB, (ingredientA, ingredientB) => output, mixTime);

    public class ChemistryLiquidAppearance(Color liquidColor, float emissionIntensity)
    {
        public Color liquidColor = liquidColor;
        public float emissionIntensity = emissionIntensity;
    }

    public interface IChemistryIngredient
    {
        public ChemistryIngredient GetInputIngredient();
        public void OnOutputIngredient(string specialInstructions);
    }

    public interface IChemistryOutputContainer
    {
        public bool ReceiveChemistryOutput(ChemistryIngredient ingredient);
    }

    public class ChemistryIngredient(Item item, ChemistryLiquidAppearance chemistryLiquidAppearance, string specialInstructions = "")
    {
        public Item item = item;
        public ChemistryLiquidAppearance chemistryLiquidAppearance = chemistryLiquidAppearance;
        public string specialInstructions = specialInstructions;
    }

    internal class DistilleryStation : NetworkBehaviour
    {
        public static List<ChemistryIngredient> registeredIngredients = [];
        public static List<DistilleryRecipe> registeredRecipies = [];

        public InteractTrigger inputTrigger = null!;

        public Collider inputTriggerCollider = null!;
        public Collider outputTriggerCollider = null!;

        public MeshRenderer inputRenderer = null!;
        public MeshRenderer outputRenderer = null!;

        public ParticleSystem inputParticleSystem = null!;
        public ParticleSystem outputParticleSystem = null!;

        public AudioSource audioSource = null!;

        ChemistryIngredient? inputIngredient;
        ChemistryIngredient? outputIngredient;

        DistilleryRecipe? currentlyMixingRecipe;

        bool mixing;
        const float defaultMixingTime = 10f;

        public void Update()
        {
            inputTriggerCollider.enabled = inputIngredient == null && localPlayer.currentlyHeldObjectServer != null;
            inputTrigger.interactable = localPlayer.currentlyHeldObjectServer != null && !localPlayer.currentlyHeldObjectServer.itemProperties.twoHanded;

            outputTriggerCollider.enabled = outputIngredient != null;
        }

        public void InputTrigger_Interact()
        {
            if (localPlayer.currentlyHeldObjectServer == null || localPlayer.currentlyHeldObjectServer.itemProperties.twoHanded) { return; }
            InputIngredientRpc(localPlayer.currentlyHeldObjectServer.NetworkObject);
        }

        public void OutputTrigger_Interact()
        {
            if (inputIngredient == null && outputIngredient == null) { return; }
            OutputTrigger_InteractRpc(localPlayer.actualClientId);
        }

        public void SetFlaskColor(bool outputFlask, ChemistryLiquidAppearance color)
        {
            Material material = new(outputRenderer.materials[0]);

            material.color = color.liquidColor;
            material.SetColor("_EmissionColor", color.liquidColor);
            material.SetFloat("_EmissionIntensity", color.emissionIntensity);

            if (outputFlask)
            {
                outputRenderer.enabled = true;
                outputRenderer.material = material;
            }
            else
            {
                inputRenderer.enabled = true;
                inputRenderer.material = material;
            }
        }

        [Rpc(SendTo.Everyone)]
        public void InputIngredientRpc(NetworkObjectReference netRef)
        {
            if (!netRef.TryGet(out NetworkObject netObj)) { return; }
            if (!netObj.TryGetComponent(out GrabbableObject item)) { return; }

            ChemistryIngredient? ingredient = null;
            bool despawningIngredientItem = false;

            if (item is IChemistryIngredient _ingredient)
                ingredient = _ingredient.GetInputIngredient();

            if (ingredient == null)
                despawningIngredientItem = true;

            ingredient ??= registeredIngredients.Where(x => x.item == item.itemProperties).FirstOrDefault();

            if (ingredient == null)
            {
                Color color = UnityEngine.Random.ColorHSV();
                ingredient = new ChemistryIngredient(item.itemProperties, new ChemistryLiquidAppearance(color, 5f));
            }

            inputIngredient = ingredient;
            SetFlaskColor(outputFlask: false, ingredient.chemistryLiquidAppearance);

            currentlyMixingRecipe = registeredRecipies.Where(x => x.ingredient == inputIngredient).FirstOrDefault();

            if (localPlayer == item.playerHeldBy && despawningIngredientItem)
                localPlayer.DespawnHeldObject();

            mixing = true;
            MixIngredients();
        }

        [Rpc(SendTo.Everyone)]
        public void OutputTrigger_InteractRpc(ulong clientId)
        {
            if (outputIngredient == null || mixing) { return; }

            PlayerControllerB? player = PlayerFromId(clientId);
            if (player == null) { return; }

            if (IsServer && !(player.currentlyHeldObjectServer != null && player.currentlyHeldObjectServer is IChemistryOutputContainer container && container.ReceiveChemistryOutput(outputIngredient)))
            {
                GrabbableObject? outputItem = Utils.SpawnItem(outputIngredient!.item.GetDawnInfo().TypedKey, player.transform.position);
                if (outputItem != null)
                {
                    IEnumerator sendSpawnOutputIngredient()
                    {
                        yield return new WaitUntil(() => outputItem.NetworkObject != null && outputItem.NetworkObject.IsSpawned);
                        SpawnOutputIngredientRpc(clientId, outputItem.NetworkObject, outputIngredient.specialInstructions);
                    }

                    StartCoroutine(sendSpawnOutputIngredient());
                }
            }

            outputParticleSystem.Stop();
            outputRenderer.enabled = false;
            outputIngredient = null;
        }

        [Rpc(SendTo.Everyone)]
        public void SpawnOutputIngredientRpc(ulong clientId, NetworkObjectReference netRef, string specialInstructions)
        {
            if (!netRef.TryGet(out NetworkObject netObj)) { return; }
            if (!netObj.TryGetComponent(out GrabbableObject item)) { return; }

            if (item is IChemistryIngredient ingredient)
                ingredient.OnOutputIngredient(specialInstructions);

            if (localPlayer.actualClientId == clientId)
                localPlayer.GrabGrabbableObject(item);
        }

        void MixIngredients()
        {
            IEnumerator mixIngredients()
            {
                yield return null;

                // TODO: Mix animations and sounds here
                inputParticleSystem.Play();
                audioSource.Play();

                float mixTime = currentlyMixingRecipe != null && currentlyMixingRecipe.mixTime > 0 ? currentlyMixingRecipe.mixTime : defaultMixingTime;

                yield return new WaitForSeconds(mixTime);

                if (currentlyMixingRecipe != null)
                {
                    outputIngredient = currentlyMixingRecipe.reaction.Invoke(inputIngredient!);
                    SetFlaskColor(outputFlask: true, outputIngredient.chemistryLiquidAppearance);
                }

                inputParticleSystem.Stop();
                outputParticleSystem.Play();
                inputIngredient = null;
                inputRenderer.enabled = false;
                mixing = false;
            }

            StartCoroutine(mixIngredients());
        }
    }

    public class DistilleryRecipe(ChemistryIngredient ingredient, Func<ChemistryIngredient, ChemistryIngredient> reaction, float mixTime = -1)
    {
        public ChemistryIngredient ingredient = ingredient;

        public Func<ChemistryIngredient, ChemistryIngredient> reaction = reaction;
        public float mixTime = mixTime;
    }

    public class DistilleryFixedOutputReaction(ChemistryIngredient ingredient, ChemistryIngredient output, float mixTime = -1) : DistilleryRecipe(ingredient, (ingredient) => output, mixTime);

    internal class CottonSwabs : NetworkBehaviour
    {
        bool spawningCottonSwab;

        public void Interact()
        {
            if (spawningCottonSwab) { return; }
            spawningCottonSwab = true;
            SpawnCottonSwabServerRpc(localPlayer.actualClientId);
        }

        [Rpc(SendTo.Server)]
        public void SpawnCottonSwabServerRpc(ulong clientGrabbingId)
        {
            if (!IsServer) { return; }

            GrabbableObject? cottonSwab = Utils.SpawnItem(LethalDiseasesKeys.CottonSwab, transform.position);
            if (cottonSwab != null)
            {
                IEnumerator sendSpawnOutputIngredient()
                {
                    yield return new WaitUntil(() => cottonSwab.NetworkObject != null && cottonSwab.NetworkObject.IsSpawned);
                    SpawnCottonSwabClientRpc(clientGrabbingId, cottonSwab.NetworkObject);
                }

                StartCoroutine(sendSpawnOutputIngredient());
            }
        }

        [Rpc(SendTo.Everyone)]
        public void SpawnCottonSwabClientRpc(ulong clientGrabbingId, NetworkObjectReference netRef)
        {
            spawningCottonSwab = false;
            if (localPlayer.actualClientId != clientGrabbingId) { return; }
            if (!netRef.TryGet(out NetworkObject netObj)) { return; }
            if (!netObj.TryGetComponent(out GrabbableObject item)) { return; }

            localPlayer.GrabGrabbableObject(item);
        }
    }
}
