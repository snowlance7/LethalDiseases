using Dusk;
using UnityEngine;

namespace LethalDiseases
{
    public class LethalDiseasesContentHandler : ContentHandler<LethalDiseasesContentHandler>
    {
        public class NetworkHandlerAssets(DuskMod mod, string filePath) : AssetBundleLoader<NetworkHandlerAssets>(mod, filePath)
        {
            [LoadFromBundle("LethalDiseasesNetworkHandler.prefab")]
            public GameObject NetworkHandlerPrefab { get; private set; } = null!;
        }
        public NetworkHandlerAssets? NetworkHandler;

        /*public class ItFollowsEntityAssets(DuskMod mod, string filePath) : AssetBundleLoader<ItFollowsEntityAssets>(mod, filePath) { }
        public ItFollowsEntityAssets? ItFollowsEntity;*/

        public class DiseaseScannerAssets(DuskMod mod, string filePath) : AssetBundleLoader<DiseaseScannerAssets>(mod, filePath) { }
        public DiseaseScannerAssets? DiseaseScanner;

        public class DiseaseAnalyzerAssets(DuskMod mod, string filePath) : AssetBundleLoader<DiseaseAnalyzerAssets>(mod, filePath) { }
        public DiseaseAnalyzerAssets? DiseaseAnalyzer;

        public LethalDiseasesContentHandler(DuskMod mod) : base(mod)
        {
            RegisterContent("networkhandler", out NetworkHandler);
            //RegisterContent("it_follows_entity", out ItFollowsEntity);
            RegisterContent("disease_scanner", out DiseaseScanner);
            RegisterContent("disease_analyzer", out DiseaseAnalyzer);
        }
    }
}