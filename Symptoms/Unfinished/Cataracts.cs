//using SnowyLib;

//namespace LethalDiseases.Symptoms
//{
//    internal static partial class Symptoms
//    {
//        [Symptom("Cataracts", "Reduced vision", Symptom.SymptomType.Bad, 50)]
//        public static StatusEffect Cataracts(Disease disease)
//        {
//            return new IntervalActionEffect(0.5f, () =>
//            {
//                if (disease.player == null) { return; }
//                VignetteOverlay.Instance.SetIntensity(0.5f, 0.1f);
//            }, disease.ToString(), "Cataracts", disease.strengthTime, SetHighestDurationAndDeny, pauseInOrbit: false);
//        }
//    }
//}