using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items
{
    internal class SyringeGunBehavior : PhysicsProp
    {
        public GameObject syringeObj = null!;
        public Animator animator = null!;
        public AudioSource audioSource = null!;
        public MeshRenderer syringeFluidRenderer = null!;
        public GameObject syringePrefab = null!;

        public string storedDisease = "";
        public int gunCompatibleAmmoID = 0115;
        private int ammoSlotToUse;
        bool isReloading;

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


        }

        public override void ItemInteractLeftRight(bool right)
        {
            base.ItemInteractLeftRight(right);
            if (!right) { return; }


        }

        public void Reload()
        {
            if (!ReloadedGun()) { return; }
            isReloading = true;

        }

        private int FindAmmoInInventory()
        {
            for (int i = 0; i < playerHeldBy.ItemSlots.Length; i++)
            {
                if (!(playerHeldBy.ItemSlots[i] == null))
                {
                    GunAmmo? gunAmmo = playerHeldBy.ItemSlots[i] as GunAmmo;
                    if (gunAmmo != null && gunAmmo.ammoType == gunCompatibleAmmoID)
                    {
                        return i;
                    }
                }
            }
            if (playerHeldBy.ItemOnlySlot != null)
            {
                GunAmmo? gunAmmo = playerHeldBy.ItemOnlySlot as GunAmmo;
                if (gunAmmo != null && gunAmmo.ammoType == gunCompatibleAmmoID)
                {
                    return 50;
                }
            }
            return -1;
        }

        private bool ReloadedGun()
        {
            int num = FindAmmoInInventory();
            if (num == -1)
            {
                Debug.Log("not reloading");
                return false;
            }
            Debug.Log("reloading!");
            ammoSlotToUse = num;
            return true;
        }
    }
}
