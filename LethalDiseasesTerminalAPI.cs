using Dawn;
using HarmonyLib;
using LethalDiseases.Items;
using SnowyCraftingCore.TerminalAdditions;
using SnowyLib;
using System;
using static LethalDiseases.Plugin;

namespace LethalDiseases
{
    internal static class LethalDiseasesTerminalAPI
    {
        [StaticInit]
        public static void Init()
        {
            TerminalAPI.RegisterTerminalCommand(new TerminalCommand("diseaseget", GetDisease, "Other", "DISEASEGET [diseaseName]", "Gets the analyzed information of a disease"));
            TerminalAPI.RegisterTerminalCommand(new TerminalCommand("diseaserename", RenameDisease, "Other", "DISEASERENAME [diseaseName] [newDiseaseName]", "Renames a disease"));
            TerminalAPI.RegisterTerminalCommand(new TerminalCommand("diseasesynthesize", SynthesizeDisease, "Other", "DISEASESYNTHESIZE [diseaseName] (10% apparatus power)", "Synthesizes and dispenses a test tube sample of a disease. Requires 10% apparatus power to synthesize."));
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

            Disease? disease = Disease.GetDiseaseFromName(diseaseName);
            if (disease == null) { return $"Rename failed, could not find disease with name: {diseaseName}\n\n"; }
            if (Disease.GetDiseaseFromName(newDiseaseName) != null) { return $"Rename failed, there is already a disease with the name: {newDiseaseName}\n\n"; }
            disease.name = newDiseaseName;
            return $"Rename succeeded, changed {diseaseName} to {newDiseaseName}\n\n";
        }

        private static string SynthesizeDisease(string[] args)
        {
            if (ApparatusPowerPort.Instance == null) { return "Synthesis failed, requires apparatus port module, which is not installed"; }
            if (SmallItemDispenser.Instance == null) { return "Synthesis failed, requires small item dispenser module, which is not installed"; }
            if (args.Length == 1) { return "Synthesis failed, no diseaseName specified\n\n"; }
            if (args.Length > 2) { return "Synthesis failed, incorrect syntax (expected syntax: DISEASESYNTHESIZE [diseaseName])"; }

            string diseaseName = args[1];

            Disease? disease = Disease.GetDiseaseFromName(diseaseName);
            if (disease == null) { return $"Synthesis failed, could not find disease with name: {diseaseName}\n\n"; }

            if (SmallItemDispenser.Instance.IsBeingUsed) { return "Synthesis failed, small item dispenser is already in use"; }

            if (!ApparatusPowerPort.Instance.IsApparatusInSlot) { return "Synthesis failed, not enough power available for synthesis, alternative power source required"; }

            if (!ApparatusPowerPort.Instance.UsePower(0.1f)) { return "Synthesis failed, not enough power available for synthesis, new alternative power source required"; }

            SmallItemDispenser.Instance.ItemDispenseOperation(LethalContent.Items[LethalDiseasesKeys.TestTube].Item, (item) => ((TestTubeBehavior)item).OnChemicalOutput(disease.ToString()), 30f);

            return $"Synthesis succeeded, dispensing {diseaseName}";
        }

        private static string AnalyzeDisease(string[] args)
        {
            if (ApparatusPowerPort.Instance == null) { return "Analysis failed, requires apparatus port module, which is not installed"; }
            if (SmallItemDispenser.Instance == null) { return "Analysis failed, requires small item dispenser module, which is not installed"; }

            if (SmallItemDispenser.Instance.IsBeingUsed) { return "Analysis failed, small item dispenser is already in use"; }

            if (!ApparatusPowerPort.Instance.IsApparatusInSlot) { return "Analysis failed, not enough power available for analysis, alternative power source required"; }

            if (!ApparatusPowerPort.Instance.UsePower(0.05f)) { return "Analysis failed, not enough power available for analysis, new alternative power source required"; }

            string diseaseName = "";

            // TODO

            return $"Analysis succeeded, use 'DISEASEGET {diseaseName}' to see the results";
        }
    }
}