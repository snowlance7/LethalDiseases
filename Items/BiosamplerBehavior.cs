using UnityEngine;

namespace LethalDiseases.Items
{
    internal class BiosamplerBehavior : PhysicsProp
    {
        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(-0.25f, 0.14f, -0.05f);
            itemProperties.rotationOffset = new Vector3(90, 80, 0);
            itemProperties.floorYOffset = 0;
            itemProperties.grabAnim = "HoldKnife";
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);

            //playerHeldBy.playerBodyAnimator.SetTrigger("UseHeldItem1");
        }
    }
}