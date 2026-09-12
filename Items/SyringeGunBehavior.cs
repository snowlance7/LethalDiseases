using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

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
    }
}
