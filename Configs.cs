using BepInEx.Configuration;
using Dawn.Utils;
using SnowyLib;

namespace LethalDiseases
{
    internal static class Configs
    {
        [StaticInit]
        public static void Init()
        {
            Plugin p = Plugin.PluginInstance;
            Plugin.logger.LogDebug("INIT DISEASE CONFIGS");
            MinSymptoms = p.Config.Bind("Other Options", "Min Symptoms", 1, "Minimum number of symptoms a disease can have");
            MaxSymptoms = p.Config.Bind("Other Options", "Max Symptoms", 5, "Maximum number of symptoms a disease can have");

            StrengthRange = p.Config.Bind("Disease Stat Ranges", "Strength Range", new BoundedRange(500, 2800), "How long (in seconds) the symptoms can persist once they take effect/activate.");
            StabilityRange = p.Config.Bind("Disease Stat Ranges", "Stability Range", new BoundedRange(500, 1400), "How long (in seconds) the disease can survive outside of a host (player or enemy).");
            LatencyRange = p.Config.Bind("Disease Stat Ranges", "Latency Range", new BoundedRange(60, 1000), "How long (in seconds) it takes for the symptoms to take effect/activate after a host (player or enemy) is infected with a disease.");
            TransmissibilityRange = p.Config.Bind("Disease Stat Ranges", "Transmissibility Range", new BoundedRange(0.1f, 1f), "Percent chance (from 0-1) that a disease can spread depending on the transmission type.");

            AirborneTypeWeight = p.Config.Bind("Transmission Weights", "Airborne Type Weight", 100, "Weighted chance that a disease will have `Airborne` as its transmission type when created");
            ContactTypeWeight = p.Config.Bind("Transmission Weights", "Contact Type Weight", 100, "Weighted chance that a disease will have `Contact` as its transmission type when created");
            BloodTypeWeight = p.Config.Bind("Transmission Weights", "Blood Type Weight", 100, "Weighted chance that a disease will have `Blood` as its transmission type when created");
            FoodborneTypeWeight = p.Config.Bind("Transmission Weights", "Foodborne Type Weight", 100, "Weighted chance that a disease will have `Foodborne` as its transmission type when created");
            ExtraTransmissionTypeChances = p.Config.Bind("Transmission Weights", "Extra Transmission Type Chances", "0.5,0.25,0.1", "Chances that a disease will have extra transmission types. Keep in mind diseases will always have at least one transmission type. Using the default '0.5,0.25,0.1' means that when a disease is generated, there will be a 50% chance it will have 2 transmission types, 25% to have 3, and 10% to have 4. Leave blank to restrict it to only one transmission type.");

            AirborneSpreadRange = p.Config.Bind("Other Options", "Airborne Spread Range", 10f, "Range for airborne spread");

            SteamDiseaseChance = p.Config.Bind("Chances", "Steam Disease Chance", 0.1f, "0-1 chance that a steam valve (and steam) will have a random disease when a dungeon gets generated");
            MonsterDiseaseChance = p.Config.Bind("Chances", "Monster Disease Chance", 0.25f, "0-1 chance that a monster will have a random disease when spawning from a vent");
            ScrapDiseaseChance = p.Config.Bind("Chances", "Scrap Disease Chance", 0.1f, "0-1 chance for a scrap item to have a random disease when spawned during dungeon generation");
            
            AccessibilityMode = p.Config.Bind("Client Options", "Accessibility Mode", false, "Enables accessibility features");

            SynthesizeDiseasePowerUsage = p.Config.Bind("Terminal Commands", "Synthesize Disease Power Usage", 0.1f, "The amount of apparatus power this command uses");
            AnalyzeDiseasePowerUsage = p.Config.Bind("Terminal Commands", "Analyze Disease Power Usage", 0.05f, "The amount of apparatus power this command uses");
            SynthesizeDiseaseSyringePowerUsage = p.Config.Bind("Terminal Commands", "Synthesize Disease Syringe Power Usage", 0.1f, "The amount of apparatus power this command uses");
            SynthesizeDiseaseDartGunAmmoPowerUsage = p.Config.Bind("Terminal Commands", "Synthesize Disease Dart Gun Ammo Power Usage", 0.5f, "The amount of apparatus power this command uses");
        }

#pragma warning disable CS8618
        public static ConfigEntry<int> MinSymptoms { get; private set; }
        public static ConfigEntry<int> MaxSymptoms { get; private set; }
        public static ConfigEntry<float> AirborneSpreadRange { get; private set; }

        public static ConfigEntry<BoundedRange> StrengthRange { get; private set; }
        public static ConfigEntry<BoundedRange> StabilityRange { get; private set; }
        public static ConfigEntry<BoundedRange> LatencyRange { get; private set; }
        public static ConfigEntry<BoundedRange> TransmissibilityRange { get; private set; }

        public static ConfigEntry<int> AirborneTypeWeight { get; private set; }
        public static ConfigEntry<int> ContactTypeWeight { get; private set; }
        public static ConfigEntry<int> BloodTypeWeight { get; private set; }
        public static ConfigEntry<int> FoodborneTypeWeight { get; private set; }
        public static ConfigEntry<string> ExtraTransmissionTypeChances { get; private set; }

        public static ConfigEntry<float> SteamDiseaseChance { get; private set; }
        public static ConfigEntry<float> MonsterDiseaseChance { get; private set; }
        public static ConfigEntry<float> ScrapDiseaseChance { get; private set; }

        public static ConfigEntry<bool> AccessibilityMode { get; private set; }

        public static ConfigEntry<float> SynthesizeDiseasePowerUsage { get; private set; }
        public static ConfigEntry<float> AnalyzeDiseasePowerUsage { get; private set; }
        public static ConfigEntry<float> SynthesizeDiseaseSyringePowerUsage { get; private set; }
        public static ConfigEntry<float> SynthesizeDiseaseDartGunAmmoPowerUsage { get; private set; }
#pragma warning restore CS8618
    }
}

//public static float insanityDecreaseRate => ContentHandler<SCP999ContentHandler>.Instance.SCP999!.GetConfig<float>("Insanity Decrease Rate").Value;