using BepInEx.Configuration;
using Dawn.Utils;
using SnowyLib;

namespace LethalDiseases
{
    internal static class Configs
    {
        [InitConfig]
        public static void Init()
        {
            Plugin p = Plugin.PluginInstance;
            Plugin.logger.LogDebug("INIT DISEASE CONFIGS");
            minSymptoms = p.Config.Bind("Other Options", "Min Symptoms", 1, "Minimum number of symptoms a disease can have");
            maxSymptoms = p.Config.Bind("Other Options", "Max Symptoms", 5, "Maximum number of symptoms a disease can have");

            strengthRange = p.Config.Bind("Disease Stat Ranges", "Strength Range", new BoundedRange(500, 2800), "How long (in seconds) the symptoms can persist once they take effect/activate.");
            stabilityRange = p.Config.Bind("Disease Stat Ranges", "Stability Range", new BoundedRange(500, 1400), "How long (in seconds) the disease can survive outside of a host (player or enemy).");
            latencyRange = p.Config.Bind("Disease Stat Ranges", "Latency Range", new BoundedRange(60, 1000), "How long (in seconds) it takes for the symptoms to take effect/activate after a host (player or enemy) is infected with a disease.");
            transmissibilityRange = p.Config.Bind("Disease Stat Ranges", "Transmissibility Range", new BoundedRange(0.1f, 1f), "Percent chance (from 0-1) that a disease can spread depending on the transmission type.");

            airborneTypeWeight = p.Config.Bind("Transmission Weights", "Airborne Type Weight", 100, "Weighted chance that a disease will have `Airborne` as its transmission type when created");
            contactTypeWeight = p.Config.Bind("Transmission Weights", "Contact Type Weight", 100, "Weighted chance that a disease will have `Contact` as its transmission type when created");
            bloodTypeWeight = p.Config.Bind("Transmission Weights", "Blood Type Weight", 100, "Weighted chance that a disease will have `Blood` as its transmission type when created");
            foodborneTypeWeight = p.Config.Bind("Transmission Weights", "Foodborne Type Weight", 100, "Weighted chance that a disease will have `Foodborne` as its transmission type when created");
            extraTransmissionTypeChances = p.Config.Bind("Transmission Weights", "Extra Transmission Type Chances", "0.5,0.25,0.1", "Chances that a disease will have extra transmission types. Keep in mind diseases will always have at least one transmission type. Using the default '0.5,0.25,0.1' means that when a disease is generated, there will be a 50% chance it will have 2 transmission types, 25% to have 3, and 10% to have 4. Leave blank to restrict it to only one transmission type.");

            airborneSpreadRange = p.Config.Bind("Other Options", "Airborne Spread Range", 10f, "Range for airborne spread");

            steamDiseaseChance = p.Config.Bind("Chances", "Steam Disease Chance", 0.1f, "0-1 chance that a steam valve (and steam) will have a random disease when a dungeon gets generated");
            monsterDiseaseChance = p.Config.Bind("Chances", "Monster Disease Chance", 0.25f, "0-1 chance that a monster will have a random disease when spawning from a vent");
            scrapDiseaseChance = p.Config.Bind("Chances", "Scrap Disease Chance", 0.1f, "0-1 chance for a scrap item to have a random disease when spawned during dungeon generation");
            
            accessibilityMode = p.Config.Bind("Client Options", "Accessibility Mode", false, "Enables accessibility features");
        }

#pragma warning disable CS8618
        public static ConfigEntry<int> minSymptoms { get; private set; }
        public static ConfigEntry<int> maxSymptoms { get; private set; }
        public static ConfigEntry<float> airborneSpreadRange { get; private set; }

        public static ConfigEntry<BoundedRange> strengthRange { get; private set; }
        public static ConfigEntry<BoundedRange> stabilityRange { get; private set; }
        public static ConfigEntry<BoundedRange> latencyRange { get; private set; }
        public static ConfigEntry<BoundedRange> transmissibilityRange { get; private set; }

        public static ConfigEntry<int> airborneTypeWeight { get; private set; }
        public static ConfigEntry<int> contactTypeWeight { get; private set; }
        public static ConfigEntry<int> bloodTypeWeight { get; private set; }
        public static ConfigEntry<int> foodborneTypeWeight { get; private set; }
        public static ConfigEntry<string> extraTransmissionTypeChances { get; private set; }

        public static ConfigEntry<float> steamDiseaseChance { get; private set; }
        public static ConfigEntry<float> monsterDiseaseChance { get; private set; }
        public static ConfigEntry<float> scrapDiseaseChance { get; private set; }

        public static ConfigEntry<bool> accessibilityMode { get; private set; }
#pragma warning restore CS8618
    }
}

//public static float insanityDecreaseRate => ContentHandler<SCP999ContentHandler>.Instance.SCP999!.GetConfig<float>("Insanity Decrease Rate").Value;