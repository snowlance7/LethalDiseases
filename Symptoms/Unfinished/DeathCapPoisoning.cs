using SnowyLib;
using System;
using System.Collections.Generic;
using System.Text;
using static LethalDiseases.SymptomAffectedObjects;

namespace LethalDiseases.Symptoms;

// - DeathCapPoisoning
internal static partial class Symptoms
{
    [Symptom("Death Cap Poisoning", "You ate something you shouldnt have eaten", Symptom.SymptomType.Bad, 0)]
    public static StatusEffect DeathCapPoisoning(Disease disease) // TODO
    {
        throw new NotImplementedException();
    }
}