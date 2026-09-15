using Dawn;
using Dawn.Utils;
using SnowyLib;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Symptoms
{
    internal static partial class Symptoms
    {
        [Symptom("Anarchist", "Randomly summon landmines in front of you", Symptom.SymptomType.Bad, 50)]
        public static StatusEffect Anarchist(Disease disease)
        {
            logger.LogDebug("anarchist start");
            return new RandomIntervalActionEffect(new BoundedRange(120f, 450f), () =>
            {
                logger.LogDebug("anarchist start2");
                if (disease.hasActor)
                    networkHandler.SpawnMapObjectRpc(MapObjectKeys.Landmine, disease.networkObject.gameObject.transform.position + disease.networkObject.gameObject.transform.forward * 2);
            }, disease.ToString(), "Anarchist", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }
}