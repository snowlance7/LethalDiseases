using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace LethalDiseases
{
    public class DiseaseHost : NetworkBehaviour
    {
        public List<Disease> Diseases = [];
        public List<Disease> ImmuneDiseases = [];
        public List<string> DiseaseIds = [];
        public List<string> ImmuneDiseaseIds = [];

        void Update()
        {
            foreach (var disease in Diseases.ToList())
            {
                disease.Update(Time.deltaTime);
            }
        }
    }
}
