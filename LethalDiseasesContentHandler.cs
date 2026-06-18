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

        public class ScannerAssets(DuskMod mod, string filePath) : AssetBundleLoader<ScannerAssets>(mod, filePath) { }
        public ScannerAssets? Scanner;

        public LethalDiseasesContentHandler(DuskMod mod) : base(mod)
        {
            RegisterContent("networkhandler", out NetworkHandler);
            //RegisterContent("it_follows_entity", out ItFollowsEntity);
            RegisterContent("scanner", out Scanner);
        }
    }
}