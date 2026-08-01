//using GameNetcodeStuff;
//using HarmonyLib;
//using SnowyLib;
//using System;
//using static LethalDiseases.Plugin;
//using static LethalDiseases.SymptomAffectedObjects;

//namespace LethalDiseases.Symptoms
//{
//    internal static partial class Symptoms
//    {
//        [Symptom("Coulrophobia", "Dont go inside", Symptom.SymptomType.Bad, 5)] // TODO: Make the jester spawn and target the player when they go inside
//        public static StatusEffect Coulrophobia(Disease disease)
//        {
//            if (disease.player != null)
//                localPlayerAffected["Coulrophobia"] = true;
//            return new OnRemoveActionEffect((effect) =>
//            {
//                if (disease.player == null) { return; }
//                localPlayerAffected["Coulrophobia"] = false;
//            }, disease.id, "Coulrophobia", disease.strengthTime, SetHighestDurationAndDeny);
//        }
//    }

//    [HarmonyPatch]
//    internal static class CoulrophobiaSymptomPatches
//    {
//        [HarmonyPostfix, HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.CheckLineOfSightForPlayer))]
//        static void CheckLineOfSightForPlayerPostFix(EnemyAI __instance, ref PlayerControllerB __result, float width, int range, int proximityAwareness)
//        {
//            try
//            {
//                return;
//            }
//            catch (Exception e)
//            {
//                logger.LogError(e);
//                return;
//            }
//        }
//    }
//}
