using Dawn;
using LethalDiseases.Items;
using LethalDiseases.Unlockables;
using SnowyCraftingCore.TerminalAdditions;
using SnowyLib;
using System;
using System.Linq;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases
{
    internal static class LethalDiseasesTerminalAPI
    {
        public static DawnItemInfo? confirmOrderItem;
        public static int confirmOrderAmount;
        public static int confirmOrderCost;
        public static string confirmOrderDiseaseString = "";

        [StaticInit]
        public static void Init()
        {
            TerminalAPI.RegisterTerminalCommand("dget", GetDisease, "Other", "DGET [diseaseName]", "Gets the analyzed information of a disease");
            TerminalAPI.RegisterTerminalCommand("drename", RenameDisease, "Other", "DRENAME [diseaseName] [newDiseaseName]", "Renames a disease");
            TerminalAPI.RegisterTerminalCommand("diseases", GetDiseases, "Other", "DISEASES", "Gets all previously analyzed disease names as a list.");
            if (TestTube.IsEnabled) { TerminalAPI.RegisterTerminalCommand("dsynth", SynthesizeDisease, "Other", "DSYNTH [diseaseName] [amount(optional)]", $"Synthesizes and orders a test tube containing a specified disease sample."); }
            if (Syringe.IsEnabled) { TerminalAPI.RegisterTerminalCommand("dsynths", SynthesizeSyringe, "Other", "DSYNTHS [diseaseName] [amount(optional)]", $"Synthesizes and orders syringes containing the specified disease."); }
            if (DartGunAmmo.IsEnabled) { TerminalAPI.RegisterTerminalCommand("dsyntha", SynthesizeDartGunAmmo, "Other", "DSYNTHA [diseaseName] [amount(optional)]", "Synthesizes and orders dart gun ammo containing the specified disease."); }
            TerminalAPI.RegisterTerminalCommand("ddelete", DeleteDisease, "Other", "DDELETE [diseaseName]", "Deletes a named disease");
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

        private static string DeleteDisease(string[] args)
        {
            if (args.Length == 1) { return "Delete failed, no diseaseName specified"; }

            string diseaseName = args[1];

            Disease? disease = Disease.GetDiseaseFromName(diseaseName);
            if (disease == null) { return $"Delete failed, could not find disease with name: {diseaseName}"; }

            NetworkHandler.Instance.DeleteNamedDiseaseRpc(disease.ToString());

            return $"Deleting {diseaseName} from database...";
        }

        private static string SynthesizeDisease(string[] args)
        {
            if (args.Length == 1) { return "Synthesis request failed, no diseaseName specified"; }
            if (args.Length > 3) { return "Synthesis request failed, incorrect syntax (expected syntax: DSYNTH [diseaseName] [amount(optional)])"; }

            string diseaseName = args[1];

            Disease? disease = Disease.GetDiseaseFromName(diseaseName);
            if (disease == null) { return $"Synthesis request failed, could not find disease with name: {diseaseName}"; }

            var dawnItem = LethalContent.Items[LethalDiseasesKeys.TestTube];
            var shopInfo = dawnItem.ShopInfo;
            if (shopInfo == null) { return $"Synthesis request failed, this item is not available for purchase"; }

            int count = 1;
            if (args.Length == 3 && int.TryParse(args[2], out int _count))
            {
                count = Mathf.Min(1, _count);
            }

            int totalCost = count * (shopInfo.DawnPurchaseInfo.Cost.Provide() + disease.CalculateSynthesisCost());
            int newGroupCredits = Utils.terminal!.groupCredits - totalCost;

            if (newGroupCredits < 0) { return $"You could not afford these items!\nYour balance is {Utils.terminal.groupCredits}. Total cost of these items is {totalCost}."; }

            NetworkHandler.Instance.SynthesizeDiseaseRpc(LethalDiseasesKeys.TestTube, disease.ToString(), count);
            Utils.SyncGroupCredits(newGroupCredits);

            return $"Synthesis succeeded. Ordered {count} test tubes containing disease {diseaseName}. Your new balance is {newGroupCredits}. Our contractors enjoy fast, free shipping while on the job! Any purchased items will arrive hourly at your approximate location.";
        }

        private static string SynthesizeSyringe(string[] args)
        {

        }

        public static string SynthesizeDartGunAmmo(string[] args)
        {

        }

        public static TerminalCommand[] GetBuyConfirmCallbacks()
        {
            var confirmCommand = new TerminalCommand("confirm", (args1) =>
            {
                if ()
            });
        }

        public static string Sanitize(string[] args)
        {
            if (ShipSanitizationUpgrade.Instance == null) { return "Sanitization failed, Ship Sanitization Upgrade is not installed"; }
            if (ShipSanitizationUpgrade.Instance.sanitizing || (AdvancedShipSanitizationUpgrade.Instance != null && AdvancedShipSanitizationUpgrade.Instance.sanitizing)) { return "Sanitization failed, sanitization cycle is already in progress"; }
            if (!StartOfRound.Instance.shipHasLanded) { return "Sanitization failed, ship must be landed to use this upgrade"; }
            if (StartOfRound.Instance.shipIsLeaving) { return "Sanitization failed, ship is leaving"; }
            if (ShipSanitizationUpgrade.Instance.cooldown > 0) { return $"Sanitization failed, Ship Sanitization Upgrade is on cooldown ({ShipSanitizationUpgrade.Instance.cooldown} seconds left)"; }
            if (ShipSanitizationUpgrade.PowerUsage > 0f && ApparatusPowerPort.Instance != null && !ApparatusPowerPort.Instance.UsePower(ShipSanitizationUpgrade.PowerUsage)) { return "Sanitization failed, not enough power available, alternative power source required"; }

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
            if (AdvancedShipSanitizationUpgrade.PowerUsage > 0f && ApparatusPowerPort.Instance != null && !ApparatusPowerPort.Instance.UsePower(AdvancedShipSanitizationUpgrade.PowerUsage)) { return "Sanitization failed, not enough power available, alternative power source required"; }

            AdvancedShipSanitizationUpgrade.Instance.SanitizeRpc();
            return "Sanitization starting in 10 seconds";
        }
    }
}