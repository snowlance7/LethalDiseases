using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using static LethalDiseases.Disease;

namespace LethalDiseases
{
    public static class DiseaseAPI
    {
        public static void Infect(this NetworkObject target)
        {
            Disease disease = Disease.CreateRandomDisease();
            disease.Infect(target);
        }

        public static void Infect(this NetworkObject target, Disease disease)
        {
            disease.Infect(target);
        }

        public static void TrySpread(this NetworkObject source, NetworkObject target, TransmissionType transmissionType)
        {
            foreach (Disease disease in source.GetDiseases())
            {
                disease.TrySpread(target);
            }
        }

        public static void TrySpreadBetween(NetworkObject source1, NetworkObject source2, TransmissionType transmissionType)
        {
            foreach (Disease disease in source1.GetDiseases())
            {
                disease.TrySpread(source2);
            }
            foreach (Disease disease in source2.GetDiseases())
            {
                disease.TrySpread(source1);
            }
        }

        public static bool HasDisease(NetworkObject target)
        {
            return target.GetDiseases().Count > 0;
        }

        public static List<Disease> GetDiseases(this NetworkObject target)
        {
            return target.GetHost().Diseases;
        }

        public static List<Disease> GetImmuneDiseases(this NetworkObject target)
        {
            return target.GetHost().ImmuneDiseases;
        }

        public static DiseaseHost GetHost(this NetworkObject target)
        {
            if (!target.TryGetComponent(out DiseaseHost host))
            {
                host = target.gameObject.AddComponent<DiseaseHost>();
            }

            return host;
        }
    }
}
