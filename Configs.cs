using Dawn.Utils;
using SnowyLib;
using System.Runtime.CompilerServices;
using static LethalDiseases.Plugin;

namespace LethalDiseases
{
    internal static class Configs
    {
        private static string DefaultTransmissableFoodborneItems => "melaniemeliciouscooked:meatzero,melaniemeliciouscooked:carrotzero,melaniemeliciouscooked:soupveggiezero,melaniemeliciouscooked:soupmeatzero,melaniemeliciouscooked:loafmeatzero,melaniemeliciouscooked:potatozero,melaniemeliciouscooked:wheatzero,melaniemeliciouscooked:breadzero,melaniemeliciouscooked:piezero,melaniemeliciouscooked:sliderzero,melaniemeliciouscooked:burgerzero,melaniemeliciouscooked:meatone,melaniemeliciouscooked:sandwichcheesezero,melaniemeliciouscooked:grapezero,melaniemeliciouscooked:juicezero,melaniemeliciouscooked:grapeone,melaniemeliciouscooked:juiceone,melaniemeliciouscooked:alcoholzero,melaniemeliciouscooked:alcoholone,melaniemeliciouscooked:alcoholtwo,melaniemeliciouscooked:alcoholthree,melaniemeliciouscooked:alcoholfour,melaniemeliciouscooked:juicetwo,melaniemeliciouscooked:juicethree,melaniemeliciouscooked:burstberryzero,melaniemeliciouscooked:burstberryone,melaniemeliciouscooked:pieone,melaniemeliciouscooked:beezero,melaniemeliciouscooked:honeyzero,melaniemeliciouscooked:waxzero,melaniemeliciouscooked:hivezero,melaniemeliciouscooked:alcoholfive,melaniemeliciouscooked:hiveone,melaniemeliciouscooked:fisherzero,melaniemeliciouscooked:fishzero,melaniemeliciouscooked:fishone,melaniemeliciouscooked:tomatozero,melaniemeliciouscooked:tomatoone,melaniemeliciouscooked:pietwo,melaniemeliciouscooked:cookiezero,melaniemeliciouscooked:bruschettazero,melaniemeliciouscooked:kabobzero,melaniemeliciouscooked:popsiclezero,melaniemeliciouscooked:popsicleone,melaniemeliciouscooked:popsicletwo,melaniemeliciouscooked:fishtwo,melaniemeliciouscooked:fishthree,melaniemeliciouscooked:fishstickszero,melaniemeliciouscooked:fishfour,melaniemeliciouscooked:fishfive,melaniemeliciouscooked:mandrakezero,melaniemeliciouscooked:anglowberryzero,melaniemeliciouscooked:meattwo,melaniemeliciouscooked:cheesezero,melaniemeliciouscooked:milkzero,melaniemeliciouscooked:eggzero,melaniemeliciouscooked:jamzero,melaniemeliciouscooked:onionzero,melaniemeliciouscooked:garliczero,melaniemeliciouscooked:breadone";
        private static string DefaultTransmissableFoodborneTags => "food,edible,organic,consumable,meat,produce,ingredient,snack,perishable,corpse,garbage,ration,snack,candy,waste,carcass,rat_food,rat,ratfood";

        [StaticInit]
        public static void Init()
        {
            _ = MinSymptoms;
            _ = MaxSymptoms;

            _ = StrengthRange;
            _ = StabilityRange;
            _ = LatencyRange;
            _ = TransmissibilityRange;

            _ = AirborneTypeWeight;
            _ = ContactTypeWeight;
            _ = BloodTypeWeight;
            _ = FoodborneTypeWeight;
            _ = ExtraTransmissionTypeChances;

            _ = AirborneSpreadRange;

            _ = SteamDiseaseChance;
            _ = MonsterDiseaseChance;
            _ = ScrapDiseaseChance;

            _ = AccessibilityMode;

            _ = SynthesizePowerUsage;
            _ = AnalyzeDiseasePowerUsage;
            _ = SynthesizeSyringePowerUsage;

            _ = TransmissableFoodborneItems;
            _ = TransmissableFoodborneTags;
        }

        public static int MinSymptoms => PluginInstance.Config.Bind("Other Options", "Min Symptoms", 1, "Minimum number of symptoms a disease can have").Value;
        public static int MaxSymptoms => PluginInstance.Config.Bind("Other Options", "Max Symptoms", 5, "Maximum number of symptoms a disease can have").Value;

        public static BoundedRange StrengthRange => PluginInstance.Config.Bind("Disease Stat Ranges", "Strength Range", new BoundedRange(500, 2800), "How long (in seconds) the symptoms can persist once they take effect/activate.").Value;
        public static BoundedRange StabilityRange => PluginInstance.Config.Bind("Disease Stat Ranges", "Stability Range", new BoundedRange(500, 1400), "How long (in seconds) the disease can survive outside of a host (player or enemy).").Value;
        public static BoundedRange LatencyRange => PluginInstance.Config.Bind("Disease Stat Ranges", "Latency Range", new BoundedRange(60, 1000), "How long (in seconds) it takes for the symptoms to take effect/activate after a host (player or enemy) is infected with a disease.").Value;
        public static BoundedRange TransmissibilityRange => PluginInstance.Config.Bind("Disease Stat Ranges", "Transmissibility Range", new BoundedRange(0.1f, 1f), "Percent chance (from 0-1) that a disease can spread depending on the transmission type.").Value;

        public static int AirborneTypeWeight => PluginInstance.Config.Bind("Transmission Weights", "Airborne Type Weight", 100, "Weighted chance that a disease will have `Airborne` as its transmission type when created").Value;
        public static int ContactTypeWeight => PluginInstance.Config.Bind("Transmission Weights", "Contact Type Weight", 100, "Weighted chance that a disease will have `Contact` as its transmission type when created").Value;
        public static int BloodTypeWeight => PluginInstance.Config.Bind("Transmission Weights", "Blood Type Weight", 100, "Weighted chance that a disease will have `Blood` as its transmission type when created").Value;
        public static int FoodborneTypeWeight => PluginInstance.Config.Bind("Transmission Weights", "Foodborne Type Weight", 100, "Weighted chance that a disease will have `Foodborne` as its transmission type when created").Value;
        public static string ExtraTransmissionTypeChances => PluginInstance.Config.Bind("Transmission Weights", "Extra Transmission Type Chances", "0.5,0.25,0.1", "Chances that a disease will have extra transmission types. Keep in mind diseases will always have at least one transmission type. Using the default '0.5,0.25,0.1' means that when a disease is generated, there will be a 50% chance it will have 2 transmission types, 25% to have 3, and 10% to have 4. Leave blank to restrict it to only one transmission type.").Value;

        public static float AirborneSpreadRange => PluginInstance.Config.Bind("Other Options", "Airborne Spread Range", 10f, "Range for airborne spread").Value;

        public static float SteamDiseaseChance => PluginInstance.Config.Bind("Chances", "Steam Disease Chance", 0.1f, "0-1 chance that a steam valve (and steam) will have a random disease when a dungeon gets generated").Value;
        public static float MonsterDiseaseChance => PluginInstance.Config.Bind("Chances", "Monster Disease Chance", 0.25f, "0-1 chance that a monster will have a random disease when spawning from a vent").Value;
        public static float ScrapDiseaseChance => PluginInstance.Config.Bind("Chances", "Scrap Disease Chance", 0.1f, "0-1 chance for a scrap item to have a random disease when spawned during dungeon generation").Value;

        public static bool AccessibilityMode => PluginInstance.Config.Bind("Client Options", "Accessibility Mode", false, "Enables accessibility features").Value;

        public static float SynthesizePowerUsage => PluginInstance.Config.Bind("Terminal Commands", "Synthesize Power Usage", 0.1f, "The amount of apparatus power this command uses").Value;
        public static float AnalyzeDiseasePowerUsage => PluginInstance.Config.Bind("Terminal Commands", "Analyze Disease Power Usage", 0.05f, "The amount of apparatus power this command uses").Value;
        public static float SynthesizeSyringePowerUsage => PluginInstance.Config.Bind("Terminal Commands", "Synthesize Syringe Power Usage", 0.1f, "The amount of apparatus power this command uses").Value;

        public static string[] TransmissableFoodborneItems => PluginInstance.Config.Bind("Other Options", "Transmissable Foodborne Items", DefaultTransmissableFoodborneItems, "Dawnlib item `NamespacedKey`s that can transmit diseases with the foodborne transmission type").Value.Replace(" ", "").Split(",");
        public static string[] TransmissableFoodborneTags => PluginInstance.Config.Bind("Other Options", "Transmissable Foodborne Item Tags", DefaultTransmissableFoodborneTags, "Dawnlib tags for items that can transmit diseases with the foodborne transmission type").Value.Replace(" ", "").Split(",");
    }
}