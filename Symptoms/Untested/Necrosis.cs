using Dawn.Utils;
using SnowyLib;

namespace LethalDiseases.Symptoms
{
    internal static partial class Symptoms
    {
        [Symptom("Necrosis", "Randomly take damage until death", Symptom.SymptomType.Bad, 50)] // TODO: Body parts that fall off and you can sell
        public static StatusEffect Necrosis(Disease disease)
        {
            if (disease.player != null)
            {
                return new RandomIntervalActionEffect(new BoundedRange(15f, 60f), () =>
                {
                    disease.player.DamagePlayer(1);
                }, disease.ToString(), "Necrosis", disease.strengthTime, SetHighestDurationAndDeny);
            }
            else
            {
                return new RandomIntervalActionEffect(new BoundedRange(30f, 90f), () =>
                {
                    if (disease.enemy != null)
                    {
                        disease.enemy.HitEnemyOnLocalClient(1);
                    }
                }, disease.ToString(), "Necrosis", disease.strengthTime, SetHighestDurationAndDeny);
            }
        }
    }
}
