using SnowyLib;
using static LethalDiseases.SymptomAffectedObjects;

namespace LethalDiseases.Symptoms
{
    internal static partial class Symptoms
    {
        [Symptom("Insanity", "Sets insanity to max", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Insanity(Disease disease)
        {
            if (disease.player != null)
                AddSymptomAffectedObject("Insanity", disease.player.NetworkObject);
            return new OnRemoveActionEffect((effect) =>
            {
                if (disease.player == null) { return; }
                RemoveSymptomAffectedObject("Insanity", disease.player.NetworkObject);
            }, disease.ToString(), "Insanity", disease.strengthTime, SetHighestDurationAndDeny);
        }

        [StaticUpdate]
        public static void InsanityUpdate()
        {
            foreach (var player in symptomAffectedPlayers["Insanity"])
            {
                if (player == null) { continue; }
                player.insanityLevel = 50;
            }
        }
    }
}
