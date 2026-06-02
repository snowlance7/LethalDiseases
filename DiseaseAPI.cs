using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;

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

        public static void TrySpread(NetworkObject source, NetworkObject target)
        {
            foreach (Disease disease in source.GetDiseases())
            {
                disease.TrySpread(target);
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
