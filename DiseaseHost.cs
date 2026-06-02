using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace LethalDiseases
{
    public class DiseaseHost : NetworkBehaviour
    {
        public List<Disease> Diseases = [];
        public List<Disease> ImmuneDiseases = [];

        void Update()
        {
            foreach (var disease in Diseases)
            {
                disease.Update(Time.deltaTime);
            }
        }
    }
}
