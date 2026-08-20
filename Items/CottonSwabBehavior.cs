using LethalDiseases.Unlockables;
using Unity.Netcode;
using UnityEngine;

namespace LethalDiseases.Items
{
    internal class CottonSwabBehavior : PhysicsProp, IChemistryIngredient
    {
        public string disease = "";

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0, 0, 0);
            itemProperties.rotationOffset = new Vector3(0, 0, 0);
            itemProperties.floorYOffset = 0;
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!buttonDown || disease != "") { return; }

            // TODO
        }

        [Rpc(SendTo.Everyone)]
        public void SetDiseaseRpc(string _disease)
        {
            disease = _disease;
        }

        ChemistryIngredient IChemistryIngredient.GetInputIngredient()
        {
            return new ChemistryIngredient(itemProperties, new ChemistryLiquidAppearance(Color.red, Color.red, 5f), disease);
        }

        void IChemistryIngredient.OnOutputIngredient(string specialInstructions)
        {
            disease = specialInstructions;
        }
    }
}