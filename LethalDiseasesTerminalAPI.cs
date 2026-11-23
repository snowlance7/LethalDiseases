using Dawn;
using LethalDiseases.Items;
using LethalDiseases.Unlockables;
using SnowyCraftingCore;
using SnowyLib;
using System.Linq;
using UnityEngine;

namespace LethalDiseases
{
    internal static class LethalDiseasesTerminalAPI
    {
        [StaticInit]
        public static void Init()
        {
            TerminalAPI.RegisterTerminalCommand("dget", GetDisease, "Other", "DGET [diseaseName]", "Gets the analyzed information of a disease.");
            TerminalAPI.RegisterTerminalCommand("drename", RenameDisease, "Other", "DRENAME [diseaseName] [newDiseaseName]", "Renames a disease.");
            TerminalAPI.RegisterTerminalCommand("diseases", GetDiseases, "Other", "DISEASES", "Gets all previously analyzed disease names as a list.");
            if (TestTube.IsEnabled) { TerminalAPI.RegisterTerminalCommand("dsynth", SynthesizeDisease, "Other", "DSYNTH [diseaseName] [amount(optional)]", $"Synthesizes and orders a test tube containing a specified disease sample."); }
            if (Syringe.IsEnabled) { TerminalAPI.RegisterTerminalCommand("dsynths", SynthesizeSyringe, "Other", "DSYNTHS [diseaseName] [amount(optional)]", $"Synthesizes and orders syringes containing the specified disease."); }
            if (DartGunAmmo.IsEnabled) { TerminalAPI.RegisterTerminalCommand("dsyntha", SynthesizeDartGunAmmo, "Other", "DSYNTHA [diseaseName] [amount(optional)]", "Synthesizes and orders dart gun ammo containing the specified disease."); }
            TerminalAPI.RegisterTerminalCommand("ddelete", DeleteDisease, "Other", "DDELETE [diseaseName]", "Deletes a named disease.");
            if (ShipSanitizationUpgrade.IsEnabled) { TerminalAPI.RegisterTerminalCommand("dsanitize", Sanitize, "Other", "DSANITIZE", $"Sanitizes the ship of all diseases. Experimental. Requires the Ship Sanitization Upgrade to be installed{(ShipSanitizationUpgrade.PowerUsage > 0f ? $" and {ShipSanitizationUpgrade.PowerUsage * 100}% apparatus power" : "")}."); }
            if (AdvancedShipSanitizationUpgrade.IsEnabled) { TerminalAPI.RegisterTerminalCommand("dsafesanitize", SafeSanitize, "Other", "DSAFESANITIZE", $"Safetly sanitizes the ship of all diseases. Requires the Advanced Ship Sanitization Upgrade to be installed{(AdvancedShipSanitizationUpgrade.PowerUsage > 0f ? $" and {AdvancedShipSanitizationUpgrade.PowerUsage * 100}% apparatus power" : "")}."); }
        }

        private static string GetDiseases(string[] args)
        {
            if (Disease.namedDiseases.Count == 0) { return "No analyzed diseases available"; }
            return "<u>Analyzed diseases</u>\n" + string.Join("\n", Disease.namedDiseases.Values.Select(x => "- " + x));
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

        private static TerminalResult DeleteDisease(string[] args)
        {
            if (args.Length == 1) { return new TerminalResult("Deletion request failed. No diseaseName specified."); }

            string diseaseName = args[1];

            Disease? disease = Disease.GetDiseaseFromName(diseaseName);
            if (disease == null) { return new TerminalResult($"Deletion request failed. Could not find disease with name: {diseaseName}"); }

            var callback1 = new TerminalCommand("confirm", (args) =>
            {
                NetworkHandler.Instance.DeleteNamedDiseaseRpc(disease.ToString());
                return $"Deleting {diseaseName} from database...";
            });
            var callback2 = new TerminalCommand("deny", (args) =>
            {
                return "Deletion cancelled.";
            });

            return new TerminalResult($"You have requested to delete {diseaseName}.\n\nPlease CONFIRM or DENY.", [callback1, callback2]);
        }

        private static TerminalResult SynthesizeDisease(string[] args)
        {
            if (args.Length == 1) { return new TerminalResult("Synthesis request failed, no diseaseName specified"); }
            if (args.Length > 3) { return new TerminalResult("Synthesis request failed, incorrect syntax (expected syntax: DSYNTH [diseaseName] [amount(optional)])"); }

            string diseaseName = args[1];

            Disease? disease = Disease.GetDiseaseFromName(diseaseName);
            if (disease == null) { return new TerminalResult($"Synthesis request failed, could not find disease with name: {diseaseName}"); }

            var dawnItem = LethalContent.Items[LethalDiseasesKeys.TestTube];
            var shopInfo = dawnItem.ShopInfo;
            if (shopInfo == null) { return new TerminalResult($"Synthesis request failed, this item is not available for purchase"); }

            int count = 1;
            if (args.Length == 3 && int.TryParse(args[2], out int _count))
            {
                count = Mathf.Min(1, _count);
            }

            int totalCost = count * (shopInfo.DawnPurchaseInfo.Cost.Provide() + disease.CalculateSynthesisCost());
            int newGroupCredits = Utils.terminal!.groupCredits - totalCost;

            if (newGroupCredits < 0) { return new TerminalResult($"You could not afford these items!\nYour balance is {Utils.terminal.groupCredits}. Total cost of these items is {totalCost}."); }

            var callback1 = new TerminalCommand("confirm", (args) =>
            {
                NetworkHandler.Instance.SynthesizeDiseaseRpc(LethalDiseasesKeys.TestTube, disease.ToString(), count);
                Utils.SyncGroupCredits(newGroupCredits);
                return $"Synthesis succeeded. Ordered {count} test tubes containing disease {diseaseName}. Your new balance is {newGroupCredits}.\n\nOur contractors enjoy fast, free shipping while on the job! Any purchased items will arrive hourly at your approximate location.";
            });
            var callback2 = new TerminalCommand("deny", (args) =>
            {
                return "Order cancelled.";
            });

            return new TerminalResult($"You have requested to order test tubes containing disease: {diseaseName}. Amount: {count}.\nTotal cost of items: {totalCost}.\n\nPlease CONFIRM or DENY.", [callback1, callback2]);
        }

        private static TerminalResult SynthesizeSyringe(string[] args)
        {
            if (args.Length == 1) { return new TerminalResult("Synthesis request failed, no diseaseName specified"); }
            if (args.Length > 3) { return new TerminalResult("Synthesis request failed, incorrect syntax (expected syntax: DSYNTHS [diseaseName] [amount(optional)])"); }

            string diseaseName = args[1];

            Disease? disease = Disease.GetDiseaseFromName(diseaseName);
            if (disease == null) { return new TerminalResult($"Synthesis request failed, could not find disease with name: {diseaseName}"); }

            var dawnItem = LethalContent.Items[LethalDiseasesKeys.Syringe];
            var shopInfo = dawnItem.ShopInfo;
            if (shopInfo == null) { return new TerminalResult($"Synthesis request failed, this item is not available for purchase"); }

            int count = 1;
            if (args.Length == 3 && int.TryParse(args[2], out int _count))
            {
                count = Mathf.Min(1, _count);
            }

            int totalCost = count * (shopInfo.DawnPurchaseInfo.Cost.Provide() + disease.CalculateSynthesisCost());
            int newGroupCredits = Utils.terminal!.groupCredits - totalCost;

            if (newGroupCredits < 0) { return new TerminalResult($"You could not afford these items!\nYour balance is {Utils.terminal.groupCredits}. Total cost of these items is {totalCost}."); }

            var callback1 = new TerminalCommand("confirm", (args) =>
            {
                NetworkHandler.Instance.SynthesizeDiseaseRpc(LethalDiseasesKeys.Syringe, disease.ToString(), count);
                Utils.SyncGroupCredits(newGroupCredits);
                return $"Synthesis succeeded. Ordered {count} syringes containing disease {diseaseName}. Your new balance is {newGroupCredits}.\n\nOur contractors enjoy fast, free shipping while on the job! Any purchased items will arrive hourly at your approximate location.";
            });
            var callback2 = new TerminalCommand("deny", (args) =>
            {
                return "Order cancelled.";
            });

            return new TerminalResult($"You have requested to order syringes containing disease: {diseaseName}. Amount: {count}.\nTotal cost of items: {totalCost}.\n\nPlease CONFIRM or DENY.", [callback1, callback2]);
        }

        public static TerminalResult SynthesizeDartGunAmmo(string[] args)
        {
            if (args.Length == 1) { return new TerminalResult("Synthesis request failed, no diseaseName specified"); }
            if (args.Length > 3) { return new TerminalResult("Synthesis request failed, incorrect syntax (expected syntax: DSYNTHA [diseaseName] [amount(optional)])"); }

            string diseaseName = args[1];

            Disease? disease = Disease.GetDiseaseFromName(diseaseName);
            if (disease == null) { return new TerminalResult($"Synthesis request failed, could not find disease with name: {diseaseName}"); }

            var dawnItem = LethalContent.Items[LethalDiseasesKeys.DartGunAmmo];
            var shopInfo = dawnItem.ShopInfo;
            if (shopInfo == null) { return new TerminalResult($"Synthesis request failed, this item is not available for purchase"); }

            int count = 1;
            if (args.Length == 3 && int.TryParse(args[2], out int _count))
            {
                count = Mathf.Min(1, _count);
            }

            int totalCost = count * (shopInfo.DawnPurchaseInfo.Cost.Provide() + (disease.CalculateSynthesisCost() * DartGunAmmo.MaxDartsInAmmo));
            int newGroupCredits = Utils.terminal!.groupCredits - totalCost;

            if (newGroupCredits < 0) { return new TerminalResult($"You could not afford these items!\nYour balance is {Utils.terminal.groupCredits}. Total cost of these items is {totalCost}."); }

            var callback1 = new TerminalCommand("confirm", (args) =>
            {
                NetworkHandler.Instance.SynthesizeDiseaseRpc(LethalDiseasesKeys.DartGunAmmo, disease.ToString(), count);
                Utils.SyncGroupCredits(newGroupCredits);
                return $"Synthesis succeeded. Ordered {count} dart gum ammo containing disease {diseaseName}. Your new balance is {newGroupCredits}.\n\nOur contractors enjoy fast, free shipping while on the job! Any purchased items will arrive hourly at your approximate location.";
            });
            var callback2 = new TerminalCommand("deny", (args) =>
            {
                return "Order cancelled.";
            });

            return new TerminalResult($"You have requested to order dart gun ammo containing disease: {diseaseName}. Amount: {count}.\nTotal cost of items: {totalCost}.\n\nPlease CONFIRM or DENY.", [callback1, callback2]);
        }

        public static TerminalResult Sanitize(string[] args)
        {
            if (ShipSanitizationUpgrade.Instance == null) { return new TerminalResult("Sanitization request denied. Ship Sanitization Upgrade is not installed."); }
            if (ShipSanitizationUpgrade.Instance.sanitizing || (AdvancedShipSanitizationUpgrade.Instance != null && AdvancedShipSanitizationUpgrade.Instance.sanitizing)) { return new TerminalResult("Sanitization request denied. Sanitization cycle is already in progress."); }
            if (!StartOfRound.Instance.shipHasLanded) { return new TerminalResult("Sanitization request denied. Ship must be landed to use this upgrade."); }
            if (StartOfRound.Instance.shipIsLeaving) { return new TerminalResult("Sanitization request denied. Ship is leaving."); }
            if (ShipSanitizationUpgrade.Instance.cooldown > 0) { return new TerminalResult($"Sanitization request denied. Ship Sanitization Upgrade is on cooldown ({ShipSanitizationUpgrade.Instance.cooldown} seconds remaining)."); }
            if (ShipSanitizationUpgrade.PowerUsage > 0f && ApparatusPowerPort.Instance != null && !ApparatusPowerPort.Instance.CanUsePower(ShipSanitizationUpgrade.PowerUsage)) { return new TerminalResult("Sanitization request denied. Not enough power available to use this upgrade. Alternative power source required."); }

            var callback1 = new TerminalCommand("confirm", (args) =>
            {
                if (ShipSanitizationUpgrade.PowerUsage > 0f && ApparatusPowerPort.Instance != null && !ApparatusPowerPort.Instance.UsePower(ShipSanitizationUpgrade.PowerUsage)) { return "Sanitization request denied. Not enough power available to use this upgrade. Alternative power source required."; }
                ShipSanitizationUpgrade.Instance.SanitizeRpc();
                return "Sanitization request confirmed. Sanitization will start in 10 seconds. Please leave the ship.";
            });
            var callback2 = new TerminalCommand("deny", (args) =>
            {
                return "Sanitization cancelled.";
            });

            return new TerminalResult($"You have requested to sanitize the ship.\nThis will use {ShipSanitizationUpgrade.PowerUsage * 100}% apparatus power.\n\nPlease CONFIRM or DENY.", [callback1, callback2]);
        }

        public static TerminalResult SafeSanitize(string[] args)
        {
            if (AdvancedShipSanitizationUpgrade.Instance == null) { return new TerminalResult("Sanitization request denied. Advanced Ship Sanitization Upgrade is not installed."); }
            if (AdvancedShipSanitizationUpgrade.Instance.sanitizing || (ShipSanitizationUpgrade.Instance != null && ShipSanitizationUpgrade.Instance.sanitizing)) { return new TerminalResult("Sanitization request denied. Sanitization cycle is already in progress."); }
            if (!StartOfRound.Instance.shipHasLanded) { return new TerminalResult("Sanitization request denied. Ship must be landed to use this upgrade."); }
            if (StartOfRound.Instance.shipIsLeaving) { return new TerminalResult("Sanitization request denied. Ship is leaving."); }
            if (AdvancedShipSanitizationUpgrade.Instance.cooldown > 0) { return new TerminalResult($"Sanitization request denied. Advanced Ship Sanitization Upgrade is on cooldown ({AdvancedShipSanitizationUpgrade.Instance.cooldown} seconds remaining)."); }
            if (AdvancedShipSanitizationUpgrade.PowerUsage > 0f && ApparatusPowerPort.Instance != null && !ApparatusPowerPort.Instance.CanUsePower(AdvancedShipSanitizationUpgrade.PowerUsage)) { return new TerminalResult("Sanitization request denied. Not enough power available to use this upgrade. Alternative power source required."); }

            var callback1 = new TerminalCommand("confirm", (args) =>
            {
                if (AdvancedShipSanitizationUpgrade.PowerUsage > 0f && ApparatusPowerPort.Instance != null && !ApparatusPowerPort.Instance.UsePower(AdvancedShipSanitizationUpgrade.PowerUsage)) { return "Sanitization request denied. Not enough power available to use this upgrade. Alternative power source required."; }
                AdvancedShipSanitizationUpgrade.Instance.SanitizeRpc();
                return "Sanitization request confirmed. Sanitization will start in 10 seconds.";
            });
            var callback2 = new TerminalCommand("deny", (args) =>
            {
                return "Sanitization cancelled.";
            });

            return new TerminalResult($"You have requested to sanitize the ship.\nThis will use {AdvancedShipSanitizationUpgrade.PowerUsage * 100}% apparatus power.\n\nPlease CONFIRM or DENY.", [callback1, callback2]);
        }
    }
}