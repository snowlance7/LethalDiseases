using LethalDiseases.Enemies;
using SnowyLib;
using static LethalDiseases.Plugin;
using static LethalDiseases.SymptomAffectedObjects;

namespace LethalDiseases.Symptoms
{
    internal static partial class Symptoms // TODO: Needs more testing
    {
        [Symptom("ItFollows", "???", Symptom.SymptomType.Bad, 10)]
        public static StatusEffect ItFollows(Disease disease)
        {
            logger?.LogDebug("Init symptom ItFollows");
            if (disease.hasActor)
            {

                NetworkHandler.Instance.AddSymptomAffectedObjectRpc("ItFollows", disease.networkObject);
            }
            return new OnRemoveActionEffect((effect) =>
            {
                if (!disease.hasActor) { return; }
                NetworkHandler.Instance.RemoveSymptomAffectedObjectRpc("ItFollows", disease.networkObject);
            }, disease.id, "ItFollows", disease.strengthTime, SetHighestDurationAndDeny);
        }

        [StaticUpdate]
        public static void ItFollowsUpdate()
        {
            if (!IsServerOrHost || ItFollowsEntity.Instance != null || StartOfRound.Instance.inShipPhase || StartOfRound.Instance.shipIsLeaving || symptomAffectedObjects["ItFollows"].Count <= 0) { return; }
            logger?.LogDebug("Spawning ItFollowsEntity");
            ItFollowsEntity.Init();
        }
    }
}