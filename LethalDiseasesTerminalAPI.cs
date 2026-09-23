using Dawn;
using SnowyCraftingCore.TerminalAdditions;
using SnowyLib;
using UnityEngine;
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
            TerminalAPI.RegisterTerminalCommand(new TerminalCommand("dsynth", SynthesizeDisease, "Other", "DSYNTH [diseaseName]", "Synthesizes and dispenses a test tube sample of a disease. Requires 10% apparatus power to synthesize."));
            TerminalAPI.RegisterTerminalCommand(new TerminalCommand("dsynths", SynthesizeDiseaseSyringe, "Other", "DSYNTHS [diseaseName]", "Synthesizes and dispenses a syringe containing the specified disease. Requires an empty syringe and 10% apparatus power to synthesize."));
            TerminalAPI.RegisterTerminalCommand(new TerminalCommand("danalyze", AnalyzeDisease, "Other", "DANALYZE", "Analyzes a test tube or cotton swab sample. Requires 5% apparatus power to analyze."));
            TerminalAPI.RegisterTerminalCommand(new TerminalCommand("diseases", GetDiseases, "Other", "DISEASES", "Gets all previously analyzed disease names as a list."));
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

            if (!ApparatusPowerPort.Instance.UsePower(Configs.SynthesizeDiseasePowerUsage.Value)) { return "Synthesis failed, not enough power available for synthesis, new alternative power source required"; }

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

        private static string SynthesizeDiseaseSyringe(string[] args)
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

            if (!ApparatusPowerPort.Instance.UsePower(Configs.SynthesizeDiseaseSyringePowerUsage.Value)) { return "Synthesis failed, not enough power available for synthesis, new alternative power source required"; }

            NetworkHandler.Instance.SynthesizeDiseaseSyringeRpc(disease.ToString());

            return $"Synthesis succeeded, dispensing {diseaseName}";
        }

        private static string SynthesizeDiseaseDartGunAmmo(string[] args) // TODO
        {
            throw new System.NotImplementedException();
            if (ApparatusPowerPort.Instance == null) { return "Synthesis failed, requires apparatus port module, which is not installed"; }
            if (SmallItemDispenser.Instance == null) { return "Synthesis failed, requires small item dispenser module, which is not installed"; }
            if (args.Length == 1) { return "Synthesis failed, no diseaseName specified\n\n"; }
            if (args.Length > 2) { return "Synthesis failed, incorrect syntax (expected syntax: DSYNTHS [diseaseName])"; }

            string diseaseName = args[1];

            Disease? disease = Disease.GetDiseaseFromName(diseaseName);
            if (disease == null) { return $"Synthesis failed, could not find disease with name: {diseaseName}\n\n"; }

            if (SmallItemDispenser.Instance.IsBeingUsed) { return "Synthesis failed, small item dispenser is already in use"; }

            if (!ApparatusPowerPort.Instance.IsApparatusInSlot) { return "Synthesis failed, not enough power available for synthesis, alternative power source required"; }

            if (!ApparatusPowerPort.Instance.UsePower(Configs.SynthesizeDiseaseDartGunAmmoPowerUsage.Value)) { return "Synthesis failed, not enough power available for synthesis, new alternative power source required"; }

            NetworkHandler.Instance.SynthesizeDiseaseSyringeRpc(disease.ToString());

            return $"Synthesis succeeded, dispensing {diseaseName}";
        }
    }
}