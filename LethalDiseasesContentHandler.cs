using Dusk;
using UnityEngine;

namespace LethalDiseases
{
    public class LethalDiseasesContentHandler : ContentHandler<LethalDiseasesContentHandler>
    {
        public class DiseaseAssetsAssets(DuskMod mod, string filePath) : AssetBundleLoader<DiseaseAssetsAssets>(mod, filePath)
        {
            [LoadFromBundle("LethalDiseasesNetworkHandler.prefab")]
            public GameObject NetworkHandlerPrefab { get; private set; } = null!;

            [LoadFromBundle("DiseaseScanNode.prefab")]
            public GameObject DiseaseScanNodePrefab { get; private set; } = null!;
        }
        public DiseaseAssetsAssets? DiseaseAssets;

        public class DiseaseScannerAssets(DuskMod mod, string filePath) : AssetBundleLoader<DiseaseScannerAssets>(mod, filePath) { }
        public DiseaseScannerAssets? DiseaseScanner;

        public class DiseaseAnalyzerAssets(DuskMod mod, string filePath) : AssetBundleLoader<DiseaseAnalyzerAssets>(mod, filePath) { }
        public DiseaseAnalyzerAssets? DiseaseAnalyzer;

        public LethalDiseasesContentHandler(DuskMod mod) : base(mod)
        {
            RegisterContent("disease_assets", out DiseaseAssets);
            RegisterContent("disease_scanner", out DiseaseScanner);
            RegisterContent("disease_analyzer", out DiseaseAnalyzer);
        }
    }
}