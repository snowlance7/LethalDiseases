using BepInEx;
using BepInEx.Logging;
using Dawn;
using Dusk;
using GameNetcodeStuff;
using HarmonyLib;
using SnowyCraftingCore;
using SnowyLib;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

namespace LethalDiseases
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency(DawnLib.PLUGIN_GUID)]
    [BepInDependency(SnowyLib.MyPluginInfo.PLUGIN_GUID)]
    internal class Plugin : BaseUnityPlugin
    {
        public static Plugin PluginInstance { get; private set; } = null!;
        public static ManualLogSource logger { get; private set; } = null!;
        public static DuskMod Mod { get; private set; } = null!;

        private readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        public static PlayerControllerB localPlayer { get { return StartOfRound.Instance.localPlayerController; } }
        public static PlayerControllerB? PlayerFromId(ulong id) { return StartOfRound.Instance.allPlayerScripts.Where(x => x.actualClientId == id).FirstOrDefault(); }
        public static bool IsServerOrHost { get { return NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost; } }

        public readonly int playerEnemiesPropsMask = Utils.CreateMask("Props", "Enemies", "Player");
        public readonly int playerEnemiesPropsInteractableObjectMask = Utils.CreateMask("Props", "InteractableObject", "Enemies", "Player");

        private void Awake()
        {
            if (PluginInstance == null) PluginInstance = this;

            logger = PluginInstance.Logger;

            harmony.PatchAll();

            AssetBundle? mainBundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Info.Location), "lethaldiseases_mainassets"));
            Mod = DuskMod.RegisterMod(this, mainBundle);
            Mod.RegisterContentHandlers();

            InitializeNetworkBehaviours();

            // Finished
            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        }

        [StaticInit]
        public static void Init()
        {
            var testTubeIngredient = new ChemistryIngredient(LethalDiseasesKeys.TestTube);

            CraftingAPI.RegisterRecipe(new ChemistryRecipe(testTubeIngredient, testTubeIngredient, (ingredientA, ingredientB) =>
            {
                Disease? disease1 = Disease.GetDiseaseFromString(ingredientA.specialInstructions);
                Disease? disease2 = Disease.GetDiseaseFromString(ingredientB.specialInstructions);

                if (disease1 != null && disease2 != null)
                {
                    var array1 = disease1.symptoms;
                    var array2 = disease2.symptoms;

                    var result = array1.Except(array2).Concat(array2.Except(array1)).ToList();
                    Disease disease3 = Disease.MergeDiseases(disease1, disease2);
                    disease3.symptoms = result.ToHashSet();
                    return new ChemistryIngredient(LethalDiseasesKeys.TestTube, disease3.GetChemistryLiquidAppearance(), disease3.ToString());
                }
                else if (disease1 != null)
                {
                    return ingredientA;
                }
                else if (disease2 != null)
                {
                    return ingredientB;
                }
                else
                {
                    return null; // TODO
                }
            }));
        }

        private static void InitializeNetworkBehaviours()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
            logger?.LogDebug("Finished initializing network behaviours");
        }
    }
}

/*
Voice.ListenForPhrase("my cool phrase", (message) => {
    Plugin.logger.LogInfo("my cool voice phrase was said!");
});
https://github.com/LoafOrc/VoiceRecognitionAPI/wiki/For-Developers
*/