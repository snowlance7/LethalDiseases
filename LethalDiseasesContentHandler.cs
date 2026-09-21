using Dusk;
using SnowyLib;
using UnityEngine;

namespace LethalDiseases
{
    public class LethalDiseasesContentHandler : ContentHandler<LethalDiseasesContentHandler>
    {
        public class BioSamplerAssets(DuskMod mod, string filePath) : AssetBundleLoader<BioSamplerAssets>(mod, filePath) { }
        public BioSamplerAssets? BioSampler;

        public class CottonSwabsAssets(DuskMod mod, string filePath) : AssetBundleLoader<CottonSwabsAssets>(mod, filePath) { }
        public CottonSwabsAssets? CottonSwabs;

        public class DiseaseAnalyzerAssets(DuskMod mod, string filePath) : AssetBundleLoader<DiseaseAnalyzerAssets>(mod, filePath) { }
        public DiseaseAnalyzerAssets? DiseaseAnalyzer;

        public class DiseaseAssetsAssets(DuskMod mod, string filePath) : AssetBundleLoader<DiseaseAssetsAssets>(mod, filePath)
        {
            [LoadFromBundle("NetworkHandler.prefab")]
            public GameObject NetworkHandlerPrefab { get; private set; } = null!;

            [LoadFromBundle("DiseaseScanNode.prefab")]
            public GameObject DiseaseScanNodePrefab { get; private set; } = null!;

            [LoadFromBundle("PukeProjector.prefab")]
            public GameObject PukeProjectorPrefab { get; private set; } = null!;

            [LoadFromBundle("AudioLibrary.asset")]
            public AudioLibrary AudioLibrary { get; private set; } = null!;

            [LoadFromBundle("ItFollowsEntity.prefab")]
            public GameObject ItFollowsEntityPrefab { get; private set; } = null!;
        }
        public DiseaseAssetsAssets? DiseaseAssets;

        public class DiseaseScannerAssets(DuskMod mod, string filePath) : AssetBundleLoader<DiseaseScannerAssets>(mod, filePath) { }
        public DiseaseScannerAssets? DiseaseScanner;

        public class SyringeAssets(DuskMod mod, string filePath) : AssetBundleLoader<SyringeAssets>(mod, filePath) { }
        public SyringeAssets? Syringe;

        public class SyringeGunAssets(DuskMod mod, string filePath) : AssetBundleLoader<SyringeGunAssets>(mod, filePath) { }
        public SyringeGunAssets? SyringeGun;

        public LethalDiseasesContentHandler(DuskMod mod) : base(mod)
        {
            RegisterContent("biosampler", out BioSampler);
            RegisterContent("cotton_swabs", out CottonSwabs);
            RegisterContent("disease_analyzer", out DiseaseAnalyzer);
            RegisterContent("disease_assets", out DiseaseAssets);
            RegisterContent("disease_scanner", out DiseaseScanner);
            RegisterContent("syringe", out Syringe);
            RegisterContent("syringe_gun", out SyringeGun);
        }
    }
}