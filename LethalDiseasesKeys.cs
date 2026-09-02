using Dawn;

namespace LethalDiseases
{
    public static class LethalDiseasesKeys
    {
        public static readonly NamespacedKey<DawnEnemyInfo> ItFollowsEntity = NamespacedKey<DawnEnemyInfo>.From("lethal_diseases", "it_follows_entity");

        public static readonly NamespacedKey<DawnUnlockableItemInfo> CottonSwabs = NamespacedKey<DawnUnlockableItemInfo>.From("lethal_diseases", "cotton_swabs");

        public static readonly NamespacedKey<DawnItemInfo> Biosampler = NamespacedKey<DawnItemInfo>.From("lethal_diseases", "biosampler");
        public static readonly NamespacedKey<DawnItemInfo> CottonSwab = NamespacedKey<DawnItemInfo>.From("lethal_diseases", "cotton_swab");
        public static readonly NamespacedKey<DawnItemInfo> DiseaseAnalyzer = NamespacedKey<DawnItemInfo>.From("lethal_diseases", "disease_analyzer");
        public static readonly NamespacedKey<DawnItemInfo> DiseaseScanner = NamespacedKey<DawnItemInfo>.From("lethal_diseases", "disease_scanner");
        public static readonly NamespacedKey<DawnItemInfo> TestTube = NamespacedKey<DawnItemInfo>.From("lethal_diseases", "test_tube");
    }
}