using Dawn;
using LethalDiseases.Items;
using LethalDiseases.Unlockables;
using SnowyCraftingCore.TerminalAdditions;
using SnowyLib;
using UnityEngine;
using UnityEngine.UIElements;
using static LethalDiseases.Plugin;

namespace LethalDiseases
{
    internal static class LethalDiseasesTerminalAPI
    {
        public static DispensableItem testTubeDispensable = new DispensableItem(LethalContent.Items[LethalDiseasesKeys.TestTube].Item, new Vector3(0, 0, 0), new Vector3(0, 180, 0));
        public static DispensableItem cottonSwabDispensable = new DispensableItem(LethalContent.Items[LethalDiseasesKeys.CottonSwab].Item, new Vector3(0.04f, 0f, 0f), new Vector3(0, 180, 0));
        public static DispensableItem syringeDispensable = new DispensableItem(LethalContent.Items[LethalDiseasesKeys.Syringe].Item, new Vector3(0.035f, -0.01f, 0f), new Vector3(0, 0, 0));

        [StaticInit]
        public static void Init()
        {
            TerminalAPI.RegisterTerminalCommand("dget", GetDisease, "Other", "DGET [diseaseName]", "Gets the analyzed information of a disease");
            TerminalAPI.RegisterTerminalCommand("drename", RenameDisease, "Other", "DRENAME [diseaseName] [newDiseaseName]", "Renames a disease");
            TerminalAPI.RegisterTerminalCommand("diseases", GetDiseases, "Other", "DISEASES", "Gets all previously analyzed disease names as a list.");
            if (ApparatusPowerPort.IsEnabled && SmallItemDispenser.IsEnabled && TestTube.IsEnabled) { TerminalAPI.RegisterTerminalCommand("dsynth", SynthesizeDisease, "Other", "DSYNTH [diseaseName]", $"Synthesizes and dispenses a test tube sample of a disease.{(Configs.SynthesizePowerUsage > 0f ? $" Requires {Configs.SynthesizePowerUsage * 100}% apparatus power to synthesize." : "")}"); }
            if (ApparatusPowerPort.IsEnabled && SmallItemDispenser.IsEnabled) { TerminalAPI.RegisterTerminalCommand("danalyze", AnalyzeDisease, "Other", "DANALYZE", $"Analyzes a test tube or cotton swab sample.{(Configs.AnalyzeDiseasePowerUsage > 0f ? $" Requires {Configs.AnalyzeDiseasePowerUsage * 100}% apparatus power to synthesize." : "")}"); }
            if (Syringe.IsEnabled && ApparatusPowerPort.IsEnabled && SmallItemDispenser.IsEnabled) { TerminalAPI.RegisterTerminalCommand("dsynths", SynthesizeSyringe, "Other", "DSYNTHS [diseaseName]", $"Synthesizes and dispenses a syringe containing the specified disease. Requires an empty syringe{(Configs.SynthesizeSyringePowerUsage > 0f ? $" and {Configs.SynthesizeSyringePowerUsage * 100}% apparatus power to synthesize" : "")}."); }
            if (DartGunAmmo.IsEnabled) { TerminalAPI.RegisterTerminalCommand("dsyntha", SynthesizeDartGunAmmo, "Other", "DSYNTHA [diseaseName] [amount(optional)]", "Synthesizes and orders dart gun ammo containing the specified disease."); }
            TerminalAPI.RegisterTerminalCommand("ddelete", DeleteDisease, "Other", "DDELETE [diseaseName]", "Deletes a named disease");
            if (ShipSanitizationUpgrade.IsEnabled) { TerminalAPI.RegisterTerminalCommand("dsanitize", Sanitize, "Other", "DSANITIZE", $"Sanitizes the ship of all diseases. Experimental. Requires the Ship Sanitization Upgrade to be installed{(ShipSanitizationUpgrade.PowerUsage > 0f ? $" and {ShipSanitizationUpgrade.PowerUsage * 100}% apparatus power" : "")}."); }
            if (AdvancedShipSanitizationUpgrade.IsEnabled) { TerminalAPI.RegisterTerminalCommand("dsafesanitize", SafeSanitize, "Other", "DSAFESANITIZE", $"Safetly sanitizes the ship of all diseases. Requires the Advanced Ship Sanitization Upgrade to be installed{(AdvancedShipSanitizationUpgrade.PowerUsage > 0f ? $" and {AdvancedShipSanitizationUpgrade.PowerUsage * 100}% apparatus power" : "")}."); }
        }

        private static string GetDiseases(string[] args)
        {
            if (Disease.namedDiseases.Count == 0) { return "No analyzed diseases available"; }
            return string.Join("\n", Disease.namedDiseases.Values) + "";
        }

        private static string GetDisease(string[] args)
        {
            if (args.Length == 1) { return "Get failed, no diseaseName specified"; }

            if (args.Length > 2) { return "Get failed, disease should only be one word"; }

            string diseaseName = args[1];
            Disease? disease = Disease.GetDiseaseFromName(diseaseName);
            if (disease == null) { return $"Get failed, could not find disease with name: {diseaseName}"; }

            return disease.GetTerminalDisplayText();
        }

        private static string RenameDisease(string[] args)
        {
            if (args.Length == 1) { return "Rename failed, no diseaseName specified"; }
            if (args.Length == 2) { return "Rename failed, new disease name was not specified"; }
            if (args.Length > 3) { return "Rename failed, new disease name must have no spaces, use underscore instead"; }

            string diseaseName = args[1];
            string newDiseaseName = args[2];

            if (newDiseaseName.Length > 8) { return "Rename failed, new disease name can only be 8 characters long"; }

            Disease? disease = Disease.GetDiseaseFromName(diseaseName);
            if (disease == null) { return $"Rename failed, could not find disease with name: {diseaseName}"; }
            
            if (Disease.GetDiseaseFromName(newDiseaseName) != null) { return $"Rename failed, there is already a disease with the name: {newDiseaseName}"; }
            
            NetworkHandler.Instance.RenameDiseaseRpc(disease.ToString(), newDiseaseName);

            return $"Rename succeeded, changed {diseaseName} to {newDiseaseName}";
        }

        private static string DeleteDisease(string[] args)
        {
            if (args.Length == 1) { return "Delete failed, no diseaseName specified"; }

            string diseaseName = args[1];

            Disease? disease = Disease.GetDiseaseFromName(diseaseName);
            if (disease == null) { return $"Delete failed, could not find disease with name: {diseaseName}"; }

            NetworkHandler.Instance.DeleteNamedDiseaseRpc(disease.ToString());

            return $"Deleting {diseaseName}...";
        }

        private static string SynthesizeDisease(string[] args)
        {
            if (ApparatusPowerPort.Instance == null) { return "Synthesis failed, requires apparatus port module, which is not installed"; }
            if (SmallItemDispenser.Instance == null) { return "Synthesis failed, requires small item dispenser module, which is not installed"; }
            if (args.Length == 1) { return "Synthesis failed, no diseaseName specified"; }
            if (args.Length > 2) { return "Synthesis failed, incorrect syntax (expected syntax: DSYNTH [diseaseName])"; }

            string diseaseName = args[1];

            Disease? disease = Disease.GetDiseaseFromName(diseaseName);
            if (disease == null) { return $"Synthesis failed, could not find disease with name: {diseaseName}"; }

            if (SmallItemDispenser.Instance.IsBeingUsed) { return "Synthesis failed, small item dispenser is already in use"; }

            if (!ApparatusPowerPort.Instance.IsApparatusInSlot) { return "Synthesis failed, not enough power available for synthesis, alternative power source required"; }

            if (!ApparatusPowerPort.Instance.UsePower(Configs.SynthesizePowerUsage)) { return "Synthesis failed, not enough power available for synthesis, new alternative power source required"; }

            NetworkHandler.Instance.SynthesizeDiseaseRpc(disease.ToString());

            return $"Synthesis succeeded, dispensing {diseaseName}";
        }

        private static string AnalyzeDisease(string[] args)
        {
            if (ApparatusPowerPort.Instance == null) { return "Analysis failed, requires apparatus port module, which is not installed"; }
            if (SmallItemDispenser.Instance == null) { return "Analysis failed, requires small item dispenser module, which is not installed"; }

            if (SmallItemDispenser.Instance.IsBeingUsed) { return "Analysis failed, small item dispenser is already in use"; }

            if (!ApparatusPowerPort.Instance.IsApparatusInSlot) { return "Analysis failed, not enough power available for analysis, alternative power source required"; }

            if (!ApparatusPowerPort.Instance.UsePower(Configs.AnalyzeDiseasePowerUsage)) { return "Analysis failed, not enough power available for analysis, new alternative power source required"; }

            NetworkHandler.Instance.AnalyzeDiseaseRpc(localPlayer.actualClientId);

            return $"Analysis ready, please input sample in item port";
        }

        private static string SynthesizeSyringe(string[] args)
        {
            if (ApparatusPowerPort.Instance == null) { return "Synthesis failed, requires apparatus port module, which is not installed"; }
            if (SmallItemDispenser.Instance == null) { return "Synthesis failed, requires small item dispenser module, which is not installed"; }
            if (args.Length == 1) { return "Synthesis failed, no diseaseName specified"; }
            if (args.Length > 2) { return "Synthesis failed, incorrect syntax (expected syntax: DSYNTHS [diseaseName])"; }

            string diseaseName = args[1];

            Disease? disease = Disease.GetDiseaseFromName(diseaseName);
            if (disease == null) { return $"Synthesis failed, could not find disease with name: {diseaseName}"; }

            if (SmallItemDispenser.Instance.IsBeingUsed) { return "Synthesis failed, small item dispenser is already in use"; }

            if (!ApparatusPowerPort.Instance.IsApparatusInSlot) { return "Synthesis failed, not enough power available for synthesis, alternative power source required"; }

            if (!ApparatusPowerPort.Instance.UsePower(Configs.SynthesizeSyringePowerUsage)) { return "Synthesis failed, not enough power available for synthesis, new alternative power source required"; }

            NetworkHandler.Instance.SynthesizeDiseaseSyringeRpc(disease.ToString());

            return "Synthesis starting, please insert Syringe";
        }

        public static string SynthesizeDartGunAmmo(string[] args)
        {
            if (args.Length == 1) { return "Synthesis failed, no diseaseName specified"; }
            if (args.Length > 3) { return "Synthesis failed, incorrect syntax (expected syntax: DSYNTHA [diseaseName] [amount(optional)])"; }

            string diseaseName = args[1];

            Disease? disease = Disease.GetDiseaseFromName(diseaseName);
            if (disease == null) { return $"Synthesis failed, could not find disease with name: {diseaseName}"; }

            var dawnItem = LethalContent.Items[LethalDiseasesKeys.DartGunAmmo];
            if (dawnItem == null || dawnItem.ShopInfo == null) { return $"Synthesis failed, dart gun ammo is not available for purchase"; }

            int count = 1;
            if (args.Length == 3 && int.TryParse(args[2], out int _count))
                count = Mathf.Min(1, _count);

            int totalCost = count * (int)(dawnItem.ShopInfo.DawnPurchaseInfo.Cost.Provide() * (dawnItem.ShopInfo.GetSalePercentage() / 100f));
            int newGroupCredits = Utils.terminal!.groupCredits - totalCost;

            if (newGroupCredits < 0) { return $"Synthesis failed. You could not afford these items!\nYour balance is {Utils.terminal.groupCredits}. Total cost of these items is {totalCost}."; }

            Utils.BuyItem(LethalDiseasesKeys.DartGunAmmo, count, true, newGroupCredits);
            NetworkHandler.Instance.SynthesizeDartGunAmmoRpc(disease.ToString(), count);

            return $"Synthesis succeeded. Ordered {count} dart gun ammo. Your new balance is {newGroupCredits}.Our contractors enjoy fast, free shipping while on the job! Any purchased items will arrive hourly at your approximate location.";
        }

        public static string Sanitize(string[] args)
        {
            if (ShipSanitizationUpgrade.Instance == null) { return "Sanitization failed, Ship Sanitization Upgrade is not installed"; }
            if (ShipSanitizationUpgrade.Instance.sanitizing || (AdvancedShipSanitizationUpgrade.Instance != null && AdvancedShipSanitizationUpgrade.Instance.sanitizing)) { return "Sanitization failed, sanitization cycle is already in progress"; }
            if (!StartOfRound.Instance.shipHasLanded) { return "Sanitization failed, ship must be landed to use this upgrade"; }
            if (StartOfRound.Instance.shipIsLeaving) { return "Sanitization failed, ship is leaving"; }
            if (ShipSanitizationUpgrade.Instance.cooldown > 0) { return $"Sanitization failed, Ship Sanitization Upgrade is on cooldown ({ShipSanitizationUpgrade.Instance.cooldown} seconds left)"; }
            if (ShipSanitizationUpgrade.PowerUsage > 0f && ApparatusPowerPort.IsEnabled && ApparatusPowerPort.Instance != null && !ApparatusPowerPort.Instance.UsePower(ShipSanitizationUpgrade.PowerUsage)) { return "Sanitization failed, not enough power available, alternative power source required"; }

            ShipSanitizationUpgrade.Instance.SanitizeRpc();
            return "Sanitization starting in 10 seconds";
        }

        public static string SafeSanitize(string[] args)
        {
            if (AdvancedShipSanitizationUpgrade.Instance == null) { return "Sanitization failed, Advanced Ship Sanitization Upgrade is not installed"; }
            if (AdvancedShipSanitizationUpgrade.Instance.sanitizing || (ShipSanitizationUpgrade.Instance != null && ShipSanitizationUpgrade.Instance.sanitizing)) { return "Sanitization failed, sanitization cycle is already in progress"; }
            if (!StartOfRound.Instance.shipHasLanded) { return "Sanitization failed, ship must be landed to use this upgrade"; }
            if (StartOfRound.Instance.shipIsLeaving) { return "Sanitization failed, ship is leaving"; }
            if (AdvancedShipSanitizationUpgrade.Instance.cooldown > 0) { return $"Sanitization failed, Advanced Ship Sanitization Upgrade is on cooldown ({AdvancedShipSanitizationUpgrade.Instance.cooldown} seconds left)"; }
            if (AdvancedShipSanitizationUpgrade.PowerUsage > 0f && ApparatusPowerPort.IsEnabled && ApparatusPowerPort.Instance != null && !ApparatusPowerPort.Instance.UsePower(AdvancedShipSanitizationUpgrade.PowerUsage)) { return "Sanitization failed, not enough power available, alternative power source required"; }

            AdvancedShipSanitizationUpgrade.Instance.SanitizeRpc();
            return "Sanitization starting in 10 seconds";
        }
    }
}