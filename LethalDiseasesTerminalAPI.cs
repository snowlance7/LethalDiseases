using BepInEx.Logging;
using SnowyCraftingCore.TerminalAdditions;
using TerminalApi;
using static TerminalApi.TerminalApi;
using TerminalApi.Classes;
using static LethalDiseases.Plugin;
using SnowyLib;
using TerminalApi.Events;
using System;

namespace LethalDiseases
{
    internal static class LethalDiseasesTerminalAPI // TODO
    {
        public static string previouslySubmittedText = "";

        [InitConfig]
        public static void Init()
        {
            TerminalApi.Events.Events.TerminalParsedSentence += TextSubmitted;
        }

        public static void SetTerminalCommands(Disease disease)
        {
            TerminalKeyword? diseaseKeyword = GetKeyword(disease.name);
            if (diseaseKeyword != null) { logger.LogDebug($"{disease.name} already analyzed"); return; }
            TerminalNode diseaseInfoNode = CreateTerminalNode(disease.GetTerminalDisplayText(), clearPreviousText: true);
            diseaseKeyword = CreateTerminalKeyword(disease.name);

            TerminalKeyword? getVerb = GetKeyword("get");
            if (getVerb == null)
            {
                getVerb = CreateTerminalKeyword("get", isVerb: true);
                getVerb = getVerb.AddCompatibleNoun(diseaseKeyword, diseaseInfoNode);
                diseaseKeyword.defaultVerb = getVerb;
                AddTerminalKeyword(getVerb);
            }
            else
            {
                getVerb = getVerb.AddCompatibleNoun(diseaseKeyword, diseaseInfoNode);
                diseaseKeyword.defaultVerb = getVerb;
                UpdateKeyword(getVerb);
            }

            TerminalNode diseaseRenameNode = CreateTerminalNode("Renaming success\n\n", clearPreviousText: true);
            TerminalKeyword? renameVerb = GetKeyword("rename");
            if (renameVerb == null)
            {
                renameVerb = CreateTerminalKeyword("rename", isVerb: true);
                renameVerb = renameVerb.AddCompatibleNoun(diseaseKeyword, diseaseRenameNode);
                AddTerminalKeyword(renameVerb, new CommandInfo()
                {
                    TriggerNode = diseaseRenameNode,
                    Title = "rename [diseaseName] [newDiseaseName]",
                    Category = "Other",
                    Description = "Renames a disease",
                    DisplayTextSupplier = () =>
                    {
                        string newName = previouslySubmittedText.Replace($"rename {disease.name} ", "");
                        logger.LogDebug($"Renaming {disease.name} to {newName}");
                        return $"Renamed {disease.name} to {newName}";
                    }
                });
            }
            else
            {
                renameVerb = renameVerb.AddCompatibleNoun(diseaseKeyword, diseaseRenameNode);
                UpdateKeyword(renameVerb);
            }

            //    TerminalKeyword? synthesizeVerb = GetKeyword("synthesize");
            //if (synthesizeVerb == null) { synthesizeVerb = CreateTerminalKeyword("synthesize", isVerb: true); }


            ////AddCommand($"result{resultCounter}", disease.GetTerminalDisplayText());

            //if (ApparatusPowerPort.Instance == null || SmallItemDispenser.Instance == null) { return; }

            //AddCommand($"synthesize result{resultCounter}", new CommandInfo()
            //{
            //    Title = "synthesize [diseaseName/result]",
            //    DisplayTextSupplier = () =>
            //    {
            //        if (!ApparatusPowerPort.Instance.IsApparatusInSlot) { return "Not enough power to synthesize, alternative power source required"; }
            //        if (!ApparatusPowerPort.Instance.UsePower(0.1f)) { return "Not enough power available, new alternative power source required"; }
            //        SmallItemDispenser.Instance.ItemDispenseOperation(LethalContent.Items[LethalDiseasesKeys.TestTube].Item, (item) =>
            //        {
            //            ((TestTubeBehavior)item).OnChemicalOutput(disease.ToString());
            //        }, 30f);
            //        return "Disease synthesized, 10% power used";
            //    },
            //    Category = "Other"
            //});

            //TerminalNode triggerNode = CreateTerminalNode($"Frank is not available right now.\n", true);
            //TerminalKeyword verbKeyword = CreateTerminalKeyword("get", true);
            //TerminalKeyword nounKeyword = CreateTerminalKeyword("frank");

            //verbKeyword = verbKeyword.AddCompatibleNoun(nounKeyword, triggerNode);
            //nounKeyword.defaultVerb = verbKeyword;

            //AddTerminalKeyword(verbKeyword);

            //// The second parameter passed in is a CommandInfo, if you want to have a callback.
            //AddTerminalKeyword(nounKeyword, new()
            //{
            //    TriggerNode = triggerNode,
            //    DisplayTextSupplier = () =>
            //    {
            //        //Logger.LogWarning("Put code here, and it will run when trigger node is loaded");
            //        return "This text will display";
            //    },
            //    Category = "Other",
            //    Description = "This is just a test command."
            //    // The above would look like '>FRANK\nThis is just a test command.' in Other
            //});
        }

        private static void TextSubmitted(object sender, Events.TerminalParseSentenceEventArgs e)
        {
            previouslySubmittedText = e.SubmittedText;
        }
    }
}
