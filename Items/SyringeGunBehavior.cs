using SnowyCraftingCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items
{
    internal class SyringeGunBehavior : PhysicsProp
    {
        public Animator animator = null!;
        public AudioSource audioSource = null!;
        public SkinnedMeshRenderer syringeFluidRenderer = null!;
        public AudioClip reloadSFX = null!;
        public AudioClip fireSFX = null!;
        public AudioClip clickSFX = null!;

        public bool IsLoaded => !string.IsNullOrWhiteSpace(storedDisease);

        int mask;

        public string storedDisease = "";
        private float maxDistance;

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(-0.04f, 0.26f, 0.07f);
            itemProperties.rotationOffset = new Vector3(10, -90, 90);
            itemProperties.floorYOffset = 0;
            mask = LayerMask.GetMask("Player", "Enemies", "Room", "Terrain");
        }

        public void SetFluidColor(ChemistryLiquidAppearance color)
        {
            syringeFluidRenderer.material.color = color.liquidColor;
            syringeFluidRenderer.material.SetColor("_EmissiveColor", color.liquidColor);
            syringeFluidRenderer.material.SetFloat("_EmissiveIntensity", color.emissionIntensity);
        }

        public override void EquipItem()
        {
            base.EquipItem();
            playerHeldBy.equippedUsableItemQE = true;
        }

        public override void DiscardItem()
        {
            playerHeldBy.equippedUsableItemQE = false;
            base.DiscardItem();
        }

        public override void PocketItem()
        {
            playerHeldBy.equippedUsableItemQE = false;
            base.PocketItem();
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!buttonDown) { return; }
            Fire();
        }

        public override void ItemInteractLeftRight(bool right)
        {
            base.ItemInteractLeftRight(right);
            if (!right) { return; }
            Reload();
        }

        public void Fire()
        {
            if (!IsLoaded)
            {
                audioSource.PlayOneShot(clickSFX);
                return;
            }

            // scp4666 knife 
            // TODO: Figure this out later when im hyperfocused
        }

        Vector3 GetEndPoint()
        {
            Ray ray = new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, StartOfRound.Instance.collidersAndRoomMask))
            {
                //float offset = Vector3.Distance(knifeTip.position, transform.position);
                //return hit.point - transform.forward * offset;
            }

            logger.LogDebug("Couldnt find wall");
            return ray.GetPoint(maxDistance);
        }

        public void Reload()
        {
            int ammoSlot = FindAmmoInInventory();
            if (ammoSlot == -1)
            {
                audioSource.PlayOneShot(clickSFX);
                return;
            }

            string diseaseId = ((SyringeBehavior)localPlayer.ItemSlots[ammoSlot]).storedDisease;
            localPlayer.DestroyItemInSlotAndSync(ammoSlot);
            ReloadRpc(diseaseId);
        }

        private int FindAmmoInInventory()
        {
            for (int i = 0; i < playerHeldBy.ItemSlots.Length; i++)
            {
                if (!(playerHeldBy.ItemSlots[i] == null))
                {
                    SyringeBehavior? syringe = playerHeldBy.ItemSlots[i] as SyringeBehavior;
                    if (syringe != null && syringe.isFilled)
                    {
                        return i;
                    }
                }
            }
            if (playerHeldBy.ItemOnlySlot != null)
            {
                SyringeBehavior? syringe = playerHeldBy.ItemOnlySlot as SyringeBehavior;
                if (syringe != null && syringe.isFilled)
                {
                    return 50;
                }
            }
            return -1;
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void ReloadRpc(string diseaseId)
        {
            Disease? disease = Disease.GetDiseaseFromString(diseaseId);
            if (disease == null) { logger.LogError("Failed to parse disease from disease id"); return; }
            storedDisease = diseaseId;
            SetFluidColor(disease.GetChemistryLiquidAppearance());
            animator.SetTrigger("reload");
        }
    }
}