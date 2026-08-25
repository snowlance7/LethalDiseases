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
            Material material = outputRenderer.materials[0];

            material.color = color.liquidColor;
            material.SetColor("_EmissionColor", color.emissionColor);
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
                ingredient = new ChemistryIngredient(item.itemProperties, new ChemistryLiquidAppearance(color, color, 5f));
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
}
