using Dawn.Utils;
using LethalDiseases.Items.Mushrooms;
using SnowyLib;

namespace LethalDiseases.Symptoms
{
    internal static partial class Symptoms
    {
        [Symptom("Shroombiosis", "You grow shrooms on your body", Symptom.SymptomType.Good, 50)]
        public static StatusEffect Shroombiosis(Disease disease)
        {
            return new RandomIntervalActionEffect(new BoundedRange(60f, 120f), () =>
            {
                if (disease.player == null) { return; }
                AttachableObjectManager.TrySpawnItemOnPlayer(Mushroom.mushroomKeys.GetRandom(), disease.player);
            }, disease.ToString(), "Shroombiosis", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }
}