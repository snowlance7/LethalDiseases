using Dawn;
using Dawn.Interfaces;
using Newtonsoft.Json.Linq;
using SnowyCraftingCore;
using SnowyLib;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items
{
    internal class DartGunAmmo : DiseaseStorable // TODO: Lots of bugs omgah
    {
        public static bool IsEnabled => LethalContent.Items[LethalDiseasesKeys.DartGunAmmo] != null;

        [SerializeField]
        MeshRenderer[] dartRenderers = null!;

        int dartsToShow = 0;

        public int AmmoLeft { get; private set; } = 0;

        public static int MaxDartsInAmmo => PluginInstance.Config.Bind("Dart Gun Options", "Dart Gun | Max Darts", 10, "The max amount of darts in a clip").Value;

        bool itemMeshEnabled = true;

        [StaticInit]
        public static void InitConfigs()
        {
            _ = MaxDartsInAmmo;
        }

        public override void EnableItemMeshes(bool enable)
        {
            base.EnableItemMeshes(enable);
            itemMeshEnabled = enable;
            SetAmmoVisual();
        }

        public void UseAmmo()
        {
            SetAmmo(AmmoLeft - 1);
        }

        public void SetAmmo(int ammoAmount)
        {
            AmmoLeft = ammoAmount;
            dartsToShow = Mathf.CeilToInt((float)AmmoLeft / MaxDartsInAmmo * 5);
            SetAmmoVisual();
        }

        public void SetAmmoVisual()
        {
            for (int i = 0; i < 5; i++)
            {
                dartRenderers[i].enabled = i < dartsToShow && itemMeshEnabled;
            }
        }

        public override void Fill(string fluidString, ChemistryLiquidAppearance color, bool doAnimation = false)
        {
            base.Fill(fluidString, color, doAnimation);
            SetAmmo(MaxDartsInAmmo);
        }

        public void Fill(string fluidString, ChemistryLiquidAppearance color, int ammoCount)
        {
            base.Fill(fluidString, color, false);
            SetAmmo(ammoCount);
        }

        [System.Serializable]
        struct DartGunAmmoSaveData(ChemistryIngredient? ingredient, int ammoCount)
        {
            public ChemistryIngredient? ingredient = ingredient;
            public int ammoCount = ammoCount;
        }

        public override JToken GetDawnDataToSave()
        {
            return JToken.FromObject(new DartGunAmmoSaveData(GetIngredient(), AmmoLeft));
        }

        public override void LoadDawnSaveData(JToken saveData) // TODO: Test this
        {
            DartGunAmmoSaveData data = saveData.Value<DartGunAmmoSaveData>()!;

            if (data.ingredient == null) { return; }

            Fill(data.ingredient.specialInstructions, data.ingredient.chemistryLiquidAppearance, data.ammoCount);
        }
    }
}
