using Dawn.Utils;
using SnowyLib;

namespace LethalDiseases.Symptoms
{
    internal static partial class Symptoms
    {
        [Symptom("Shroombiosis", "You grow shrooms on your body which heal players when eaten", Symptom.SymptomType.Good, 50)]
        public static StatusEffect Shroombiosis(Disease disease)
        {
            return new RandomIntervalActionEffect(new BoundedRange(30f, 120f), () =>
            {
                if (disease.player == null) { return; }
                AttachableObjectManager.TrySpawnItemOnPlayer(LethalDiseasesKeys.HealingMushroom, disease.player);
            }, disease.ToString(), "Shroombiosis", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }
}