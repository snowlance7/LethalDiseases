using Dawn.Utils;
using SnowyLib;
using static LethalDiseases.NetworkHandler;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Symptoms
{
    internal static partial class Symptoms
    {
        [Symptom("Anitidaephobia", "You hallucinate duck sounds", Symptom.SymptomType.Neutral, 50)]
        public static StatusEffect Anitidaephobia(Disease disease)
        {
            return new RandomIntervalActionEffect(new BoundedRange(30f, 60f), () =>
            {
                if (disease.player == null) { return; }
                var clips = audioLibrary.GetClips(SoundEffect.Duck.ToString());
                var pos = RoundManager.Instance.GetRandomPositionInRadius(localPlayer.transform.position, 1f, 5f);
                Utils.PlaySoundAtPosition(pos, clips, max3DDistance: 20);
            }, disease.ToString(), "Anitidaephobia", disease.strengthTime, SetHighestDurationAndDeny);
        }
    }
}
