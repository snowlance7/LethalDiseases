using SnowyLib;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Symptoms
{
    internal static partial class Symptoms
    {
        static float weaknessPreviousBaseWeight;
        static float weaknessPreviousWeight;

        [Symptom("Weakness", "Your carry weight is doubled", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Weakness(Disease disease) // TODO: test
        {
            if (disease.player != null)
            {
                weaknessPreviousBaseWeight = disease.player.carryWeight;
                weaknessPreviousWeight = weaknessPreviousBaseWeight * 2;
                disease.player.carryWeight = weaknessPreviousWeight;
                StartOfRound.Instance.ChangedCarryWeight += OnWeightChangedLocalClient;
            }
            return new OnRemoveActionEffect((effect) =>
            {
                if (disease.player == null) { return; }
                StartOfRound.Instance.ChangedCarryWeight -= OnWeightChangedLocalClient;
                disease.player.carryWeight = weaknessPreviousBaseWeight;
            }, disease.ToString(), "Weakness", disease.strengthTime, SetHighestDurationAndDeny);
        }

        static void OnWeightChangedLocalClient()
        {
            float addedWeight = localPlayer.carryWeight - weaknessPreviousWeight;
            weaknessPreviousBaseWeight += addedWeight;
            localPlayer.carryWeight = weaknessPreviousBaseWeight * 2;
            weaknessPreviousWeight = localPlayer.carryWeight;
        }
    }
}
