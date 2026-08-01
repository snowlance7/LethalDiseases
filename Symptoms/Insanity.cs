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
                networkHandler.AddSymptomAffectedObjectRpc("Insanity", disease.player.NetworkObject);
            return new OnRemoveActionEffect((effect) =>
            {
                if (disease.player == null) { return; }
                networkHandler.RemoveSymptomAffectedObjectRpc("Insanity", disease.player.NetworkObject);
            }, disease.id, "Insanity", disease.strengthTime, SetHighestDurationAndDeny);
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
