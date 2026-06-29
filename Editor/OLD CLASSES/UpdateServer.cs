using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Theoden.Editor
{
      public class UpdateServer
      {
          
        private const string UpdateServerJsonFile = "Assets/Editor/UpdateServer.json";
        private static bool _assetBundles,
            _codexBase,
            _codexMain,
            _codexSide,
            _codexImages,
            _codexError,
            _collectibles,
            _creditsRoles,
            _creditsPeople,
            _qrScanner,
            _tutorialText;
        [MenuItem("UpdateServer/Start")]
        public static void Start()
        {
            Prepare();
            if(_assetBundles)
                Build();
            Upload();
        }

        private static void Prepare()
        {
            var fileContent = string.Join("\n", FileUtilities.ReadCompleteFile(UpdateServerJsonFile));
            var dataToUpdate = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(fileContent);
            _assetBundles = GetValue(dataToUpdate, "asset_bundles");
            _codexBase = GetValue(dataToUpdate, "codex_base");
            _codexMain = GetValue(dataToUpdate, "codex_main");
            _codexSide = GetValue(dataToUpdate, "codex_side");
            _codexImages = GetValue(dataToUpdate, "codex_images");
            _codexError = GetValue(dataToUpdate, "codex_error");
            _collectibles = GetValue(dataToUpdate, "collectibles");
            _creditsRoles = GetValue(dataToUpdate, "credits_roles");
            _creditsPeople = GetValue(dataToUpdate, "credits_people");
            _qrScanner = GetValue(dataToUpdate, "qr_scanner");
            _tutorialText = GetValue(dataToUpdate, "tutorial_text");
        }

        private static bool GetValue(List<Dictionary<string, string>>dataToUpdate, string name)
        {
            var tmp = dataToUpdate.Find(e => e["name"].Equals(name));
            if (tmp != null && tmp.ContainsKey("update"))
            {
                return tmp["update"].Equals("1");
            }
            Debug.LogWarning("Entry " + name + " not found in UpdateServer.json");
            return false;
        }

        private static void Build()
        {
            Debug.Log("Starting Build");
            CreateAssetBundles.BuildAllAssetBundles();
            Debug.Log("Starting Asset Bundles Json creation");
            CreateJsonFromAssetBundleManifest.CreateAllJson();
        }

        private static void Upload()
        {
            Debug.Log("Starting Upload To Server");
            /*
             * Login.GetToken();
            UploadLanguages.Start();
            if(_codexError)
                UploadCodex.UploadError();
            if(_creditsRoles)
                UploadCredits.UploadRoles();
            if(_creditsPeople)
                UploadCredits.UploadPeople();
            if(_tutorialText)
                UploadTutorial.UploadTutorialText();
            if (_codexBase)
                UploadCodex.UploadGenericStop();
            if (_assetBundles)
                UploadAssetBundlesAndJsonToServer.UploadAllAssetBundles();
            if (_codexMain)
                UploadCodex.UploadMainStop();
            if (_codexSide)
                UploadCodex.UploadSideStop();
            if (_codexImages)
                UploadCodex.UploadCodexImages();
            if (_collectibles)
                UploadCollectibles.UploadAll();
            if (_qrScanner)
                UploadQrToPrefabAssociations.Upload();
            Debug.Log("Full upload Completed");
             */
        }
    }  
}

