using Dawn;
using LethalDiseases.Items;
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
            TerminalAPI.RegisterTerminalCommand(new TerminalCommand("dget", GetDisease, "Other", "DGET [diseaseName]", "Gets the analyzed information of a disease"));
            TerminalAPI.RegisterTerminalCommand(new TerminalCommand("drename", RenameDisease, "Other", "DRENAME [diseaseName] [newDiseaseName]", "Renames a disease"));
            TerminalAPI.RegisterTerminalCommand(new TerminalCommand("diseases", GetDiseases, "Other", "DISEASES", "Gets all previously analyzed disease names as a list."));
            if (ApparatusPowerPort.IsEnabled && SmallItemDispenser.IsEnabled && TestTubeBehavior.IsEnabled) { TerminalAPI.RegisterTerminalCommand(new TerminalCommand("dsynth", SynthesizeDisease, "Other", "DSYNTH [diseaseName]", "Synthesizes and dispenses a test tube sample of a disease. Requires 10% apparatus power to synthesize.")); }
            if (ApparatusPowerPort.IsEnabled && SmallItemDispenser.IsEnabled) { TerminalAPI.RegisterTerminalCommand(new TerminalCommand("danalyze", AnalyzeDisease, "Other", "DANALYZE", "Analyzes a test tube or cotton swab sample. Requires 5% apparatus power to analyze.")); }
            if (SyringeBehavior.IsEnabled && ApparatusPowerPort.IsEnabled && SmallItemDispenser.IsEnabled) { TerminalAPI.RegisterTerminalCommand(new TerminalCommand("dsynths", SynthesizeSyringe, "Other", "DSYNTHS [diseaseName]", "Synthesizes and dispenses a syringe containing the specified disease. Requires an empty syringe and 10% apparatus power to synthesize.")); }
            if (DartGunAmmo.IsEnabled) { TerminalAPI.RegisterTerminalCommand(new TerminalCommand("dsyntha", SynthesizeDartGunAmmo, "Other", "DSYNTHA [diseaseName] [amount(optional)]", "Synthesizes and orders dart gun ammo containing the specified disease.")); }
        }

        private static string GetDiseases(string[] args)
        {
            if (Disease.namedDiseases.Count == 0) { return "No analyzed diseases available"; }
            return string.Join("\n", Disease.namedDiseases.Values) + "\n\n";
        }

        private static string GetDisease(string[] args)
        {
            if (args.Length == 1) { return "Get failed, no diseaseName specified\n\n"; }

            if (args.Length > 2) { return "Get failed, disease should only be one word\n\n"; }

            string diseaseName = args[1];
            Disease? disease = Disease.GetDiseaseFromName(diseaseName);
            if (disease == null) { return $"Get failed, could not find disease with name: {diseaseName}\n\n"; }

            return disease.GetTerminalDisplayText();
        }

        private static string RenameDisease(string[] args)
        {
            if (args.Length == 1) { return "Rename failed, no diseaseName specified\n\n"; }
            if (args.Length == 2) { return "Rename failed, new disease name was not specified\n\n"; }
            if (args.Length > 3) { return "Rename failed, new disease name must have no spaces, use underscore instead\n\n"; }

            string diseaseName = args[1];
            string newDiseaseName = args[2];

            if (newDiseaseName.Length > 8) { return "Rename failed, new disease name can only be 8 characters long"; }

            Disease? disease = Disease.GetDiseaseFromName(diseaseName);
            if (disease == null) { return $"Rename failed, could not find disease with name: {diseaseName}\n\n"; }
            if (Disease.GetDiseaseFromName(newDiseaseName) != null) { return $"Rename failed, there is already a disease with the name: {newDiseaseName}\n\n"; }
            NetworkHandler.Instance.RenameDiseaseRpc(disease.ToString(), newDiseaseName);
            return $"Rename succeeded, changed {diseaseName} to {newDiseaseName}\n\n";
        }

        private static string SynthesizeDisease(string[] args)
        {
            if (ApparatusPowerPort.Instance == null) { return "Synthesis failed, requires apparatus port module, which is not installed"; }
            if (SmallItemDispenser.Instance == null) { return "Synthesis failed, requires small item dispenser module, which is not installed"; }
            if (args.Length == 1) { return "Synthesis failed, no diseaseName specified\n\n"; }
            if (args.Length > 2) { return "Synthesis failed, incorrect syntax (expected syntax: DSYNTH [diseaseName])"; }

            string diseaseName = args[1];

            Disease? disease = Disease.GetDiseaseFromName(diseaseName);
            if (disease == null) { return $"Synthesis failed, could not find disease with name: {diseaseName}\n\n"; }

            if (SmallItemDispenser.Instance.IsBeingUsed) { return "Synthesis failed, small item dispenser is already in use"; }

            if (!ApparatusPowerPort.Instance.IsApparatusInSlot) { return "Synthesis failed, not enough power available for synthesis, alternative power source required"; }

            if (!ApparatusPowerPort.Instance.UsePower(Configs.SynthesizePowerUsage.Value)) { return "Synthesis failed, not enough power available for synthesis, new alternative power source required"; }

            NetworkHandler.Instance.SynthesizeDiseaseRpc(disease.ToString());

            return $"Synthesis succeeded, dispensing {diseaseName}";
        }

        private static string AnalyzeDisease(string[] args)
        {
            if (ApparatusPowerPort.Instance == null) { return "Analysis failed, requires apparatus port module, which is not installed"; }
            if (SmallItemDispenser.Instance == null) { return "Analysis failed, requires small item dispenser module, which is not installed"; }

            if (SmallItemDispenser.Instance.IsBeingUsed) { return "Analysis failed, small item dispenser is already in use"; }

            if (!ApparatusPowerPort.Instance.IsApparatusInSlot) { return "Analysis failed, not enough power available for analysis, alternative power source required"; }

            if (!ApparatusPowerPort.Instance.UsePower(Configs.AnalyzeDiseasePowerUsage.Value)) { return "Analysis failed, not enough power available for analysis, new alternative power source required"; }

            NetworkHandler.Instance.AnalyzeDiseaseRpc(localPlayer.actualClientId);

            return $"Analysis ready, please input sample in item port";
        }

        private static string SynthesizeSyringe(string[] args)
        {
            if (ApparatusPowerPort.Instance == null) { return "Synthesis failed, requires apparatus port module, which is not installed"; }
            if (SmallItemDispenser.Instance == null) { return "Synthesis failed, requires small item dispenser module, which is not installed"; }
            if (args.Length == 1) { return "Synthesis failed, no diseaseName specified\n\n"; }
            if (args.Length > 2) { return "Synthesis failed, incorrect syntax (expected syntax: DSYNTHS [diseaseName])"; }

            string diseaseName = args[1];

            Disease? disease = Disease.GetDiseaseFromName(diseaseName);
            if (disease == null) { return $"Synthesis failed, could not find disease with name: {diseaseName}\n\n"; }

            if (SmallItemDispenser.Instance.IsBeingUsed) { return "Synthesis failed, small item dispenser is already in use"; }

            if (!ApparatusPowerPort.Instance.IsApparatusInSlot) { return "Synthesis failed, not enough power available for synthesis, alternative power source required"; }

            if (!ApparatusPowerPort.Instance.UsePower(Configs.SynthesizeSyringePowerUsage.Value)) { return "Synthesis failed, not enough power available for synthesis, new alternative power source required"; }

            NetworkHandler.Instance.SynthesizeDiseaseSyringeRpc(disease.ToString());

            return "Synthesis starting, please insert Syringe";
        }

        public static string SynthesizeDartGunAmmo(string[] args)
        {
            if (args.Length == 1) { return "Synthesis failed, no diseaseName specified\n\n"; }
            if (args.Length > 3) { return "Synthesis failed, incorrect syntax (expected syntax: DSYNTHA [diseaseName] [amount(optional)])"; }

            string diseaseName = args[1];

            Disease? disease = Disease.GetDiseaseFromName(diseaseName);
            if (disease == null) { return $"Synthesis failed, could not find disease with name: {diseaseName}\n\n"; }

            var dawnItem = LethalContent.Items[LethalDiseasesKeys.DartGunAmmo];
            if (dawnItem == null || dawnItem.ShopInfo == null) { return $"Synthesis failed, dart gun ammo is not available for purchase"; }

            int count = 1;
            if (args.Length == 3 && int.TryParse(args[2], out int _count))
                count = Mathf.Min(1, _count);

            int totalCost = count * (int)(dawnItem.ShopInfo.DawnPurchaseInfo.Cost.Provide() * (dawnItem.ShopInfo.GetSalePercentage() / 100f));
            int newGroupCredits = Utils.terminal!.groupCredits - totalCost;

            if (newGroupCredits < 0) { return $"Synthesis failed. You could not afford these items!\nYour balance is {Utils.terminal.groupCredits}. Total cost of these items is {totalCost}.\n\n"; }

            Utils.BuyItem(LethalDiseasesKeys.DartGunAmmo, count, true, newGroupCredits);
            NetworkHandler.Instance.SynthesizeDartGunAmmoRpc(disease.ToString(), count);

            return $"Synthesis succeeded. Ordered {count} dart gun ammo. Your new balance is {newGroupCredits}.\n\nOur contractors enjoy fast, free shipping while on the job! Any purchased items will arrive hourly at your approximate location.\n\n";
        }
    }
}