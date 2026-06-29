using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
//using OneSignalSDK;

namespace Downloader
{
    public class UiManager : MonoBehaviour
    {
        public GameObject languageButtonPrefab;
        public Transform languageButtonContainer;
        public GameObject downloadInProgressWindow;
        public GameObject choseLanguageWindow;
        public GameObject networkErrorWindow;
        public CheckAndDownload languageDownload;
        public CheckAndDownload[] nonLanguageDependentDownload;
        public CheckAndDownload[] languageDependentDownload;
        public Slider overallDownload;
        private readonly List<CheckAndDownload> _versionsToUpdateLanguageDependent = new List<CheckAndDownload>();
        private readonly List<CheckAndDownload> _versionsToUpdateNonLanguageDependent = new List<CheckAndDownload>();
        private bool _abortDownload;
        private bool _languageDependentCheckComplete;
        private bool _languageDependentDownloadComplete;
        private bool _nonLanguageDependentCheckComplete;
        private bool _nonLanguageDependentDownloadComplete;

        private void Start()
        {
            StartCoroutine(Manage());
        }

        private IEnumerator CheckDownload(GameObject download)
        {
            CheckAndDownload checkAndDownload;
            try
            {
                checkAndDownload = download.GetComponent<CheckAndDownload>();
                checkAndDownload.IsUpdateComplete();
                checkAndDownload.IsProcessAborted();
            }
            catch (NotSupportedException)
            {
                yield break;
            }

            //finche il download non è completo e non è fallito, aspetta
            yield return new WaitWhile(() =>
                !checkAndDownload.IsUpdateComplete() && !checkAndDownload.IsProcessAborted());
            if (!checkAndDownload.IsProcessAborted()) yield break;
            //se è fallito, stop a tutto
            StopAllDownloads();
        }

        private void StopAllDownloads()
        {
            if (_abortDownload) return;
            _abortDownload = true;
            downloadInProgressWindow.SetActive(false);
            choseLanguageWindow.SetActive(false);
            networkErrorWindow.SetActive(true);
            try
            {
                foreach (var download in nonLanguageDependentDownload) download.gameObject.SetActive(false);

                foreach (var download in languageDependentDownload) download.gameObject.SetActive(false);

                enabled = false;
            }
            catch (Exception)
            {
                //ignore
            }
        }


        private IEnumerator Manage()
        {
            SetupCommonVariables();
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                if (!string.IsNullOrEmpty(CommonVariables.Language))
                {
                    AssetBundleManager.LoadAllCachedAssetBundles();
                    ChangeScene.ChangeSceneTo("scenes/MainMenu");
                }
                else
                {
                    networkErrorWindow.SetActive(true);
                }
            }
            else
            {
                downloadInProgressWindow.SetActive(true);
                yield return new WaitForSeconds(0.3f);
                //OneSignal.Default.Initialize("2c0fd0ab-432b-42c8-9290-d7f9f9d193c7");
                var totalDownloads = (nonLanguageDependentDownload.Length + languageDependentDownload.Length)*2;
                overallDownload.maxValue = totalDownloads;
                overallDownload.minValue = 0;
                UpdateSliderValue.SetValueInCoroutine(0f, overallDownload, this);
                foreach (var checkAndDownload in nonLanguageDependentDownload)
                    StartCoroutine(CheckDownload(checkAndDownload.gameObject));
                foreach (var checkAndDownload in languageDependentDownload)
                    StartCoroutine(CheckDownload(checkAndDownload.gameObject));
                yield return new WaitForSeconds(0.3f);
                StartCoroutine(CheckNonLanguageDependentVersions());
                yield return new WaitWhile(() => !_nonLanguageDependentCheckComplete);
                yield return new WaitForSeconds(0.3f);
                StartCoroutine(UpdateNonLanguageDependentVersions());
                yield return new WaitWhile(() => !_nonLanguageDependentDownloadComplete);
                yield return new WaitForSeconds(0.3f);
                if (_abortDownload) yield break;
                if (string.IsNullOrEmpty(CommonVariables.Language))
                {
                    FillLanguageSelectionPanel();
                    yield return new WaitWhile(() => string.IsNullOrEmpty(CommonVariables.Language));
                    if (_abortDownload) yield break;
                }

                yield return new WaitForSeconds(0.3f);
                UpdateCodexStops();
                yield return new WaitForSeconds(0.3f);
                if (_abortDownload) yield break;
                StartCoroutine(CheckLanguageDependentVersions());
                yield return new WaitWhile(() => !_languageDependentCheckComplete);
                yield return new WaitForSeconds(0.3f);
                StartCoroutine(UpdateLanguageDependentVersions());
                yield return new WaitWhile(() => !_languageDependentDownloadComplete);
                if (_abortDownload) yield break;
                yield return new WaitForSeconds(0.3f);
                choseLanguageWindow.SetActive(false);
                downloadInProgressWindow.SetActive(true);
                if (!_abortDownload) ChangeScene.ChangeSceneTo("scenes/MainMenu");
            }
        }

        private static void SetupCommonVariables()
        {
            CommonVariables.Language = PlayerPrefs.GetString("language", string.Empty);
            CommonVariables.ThemeStatus = PlayerPrefs.GetInt("Theme", 1) == 1;
            CommonVariables.SFXStatus = PlayerPrefs.GetInt("SFX", 1) == 1;
        }

        private IEnumerator CheckNonLanguageDependentVersions()
        {
            foreach (var checkAndDownload in nonLanguageDependentDownload)
                try
                {
                    if (!checkAndDownload.IsLastVersion())
                    {
                        _versionsToUpdateNonLanguageDependent.Add(checkAndDownload);
                        UpdateSliderValue.SetValueInCoroutine(overallDownload.value + 1, overallDownload, this);
                    }
                    else
                    {
                        UpdateSliderValue.SetValueInCoroutine(overallDownload.value + 2, overallDownload, this);
                    }
                }
                catch (Exception)
                {
                    StopAllDownloads();
                }

            _nonLanguageDependentCheckComplete = true;
            yield return null;
        }

        private IEnumerator UpdateNonLanguageDependentVersions()
        {
            foreach (var checkAndDownload in _versionsToUpdateNonLanguageDependent)
                try
                {
                    checkAndDownload.UpdateToCurrentVersion();
                    UpdateSliderValue.SetValueInCoroutine(overallDownload.value + 1, overallDownload, this);

                }
                catch (Exception)
                {
                    StopAllDownloads();
                }


            _nonLanguageDependentDownloadComplete = true;


            yield break;
        }

        private IEnumerator CheckLanguageDependentVersions()
        {
            yield return new WaitWhile(() => string.IsNullOrEmpty(CommonVariables.Language));
            try
            {
                foreach (var checkAndDownload in languageDependentDownload)
                    if (!checkAndDownload.IsLastVersion())
                    {
                        _versionsToUpdateLanguageDependent.Add(checkAndDownload);
                        UpdateSliderValue.SetValueInCoroutine(overallDownload.value + 1, overallDownload, this);

                    }
                    else
                    {
                        UpdateSliderValue.SetValueInCoroutine(overallDownload.value + 2, overallDownload, this);

                    }

                _languageDependentCheckComplete = true;
            }
            catch (Exception)
            {
                StopAllDownloads();
            }
        }

        private IEnumerator UpdateLanguageDependentVersions()
        {
            yield return new WaitWhile(() => string.IsNullOrEmpty(CommonVariables.Language));
            try
            {
                foreach (var checkAndDownload in _versionsToUpdateLanguageDependent)
                {
                    checkAndDownload.UpdateToCurrentVersion();
                    UpdateSliderValue.SetValueInCoroutine(overallDownload.value + 1, overallDownload, this);

                }

                _languageDependentDownloadComplete = true;
            }
            catch (Exception)
            {
                StopAllDownloads();
            }
        }

        private void FillLanguageSelectionPanel()
        {
            try
            {
                var languageJsonString = JSONDownloadManager.LoadJsonFromLocalStorage(languageDownload.SavePath());
                if (languageJsonString == null)
                {
                    choseLanguageWindow.SetActive(false);
                    downloadInProgressWindow.SetActive(false);
                    networkErrorWindow.SetActive(true);
                    return;
                }

                var languageList = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(languageJsonString);
                //var languageClick = choseLanguageWindow.AddComponent<LanguageClick>();
                var assetBundle = AssetBundleManager.LoadLocalAssetBundle("fonts");
                //languageClick.selectedLanguageFont = assetBundle == null
                   // ? Font.CreateDynamicFontFromOSFont("Arial", 14)
                    //: assetBundle.LoadAsset<Font>("Font Libro");
                foreach (var languageElement in languageList)
                    //LanguagesCallback.AddLanguageButtonToContainer(languageButtonPrefab, languageButtonContainer,
                       // languageElement, languageClick);

                downloadInProgressWindow.SetActive(false);
                choseLanguageWindow.SetActive(true);
            }
            catch (Exception)
            {
                StopAllDownloads();
            }
        }

        private void UpdateCodexStops()
        {
            try
            {
                var codexJsonString = JSONDownloadManager.LoadJsonFromLocalStorage("codex/all");
                if (string.IsNullOrEmpty(codexJsonString) || codexJsonString == "[]" || codexJsonString == "{}")
                {
                    //impossible, only called after all non language dependent downloads are complete
                    Debug.LogError("Codex JSON is empty");
                    return;
                }

                var codexJson =
                    JsonConvert.DeserializeObject<List<Codex>>(codexJsonString);

                foreach (var codexStop in codexJson)
                {
                    var go1 = new GameObject("CollectibleUpdater_" + codexStop.id);
                    var script = go1.AddComponent<CheckAndDownloadJSONCollectibles>();
                    script.codexId = codexStop.id.ToString();
                    var go2 = new GameObject("CollectibleUpdaterImagesFalse_" + codexStop.id);
                    var codexImagesScript = go2.AddComponent<CheckAndDownloadCodexImages>();
                    codexImagesScript.codexId = codexStop.id;
                    codexImagesScript.storyType = false;
                    var go3 = new GameObject("collectibleUpdaterImagesTrue_" + codexStop.id);
                    var codexImagesScript2 = go3.AddComponent<CheckAndDownloadCodexImages>();
                    codexImagesScript.codexId = codexStop.id;
                    codexImagesScript.storyType = true;
                    if (!script.IsLastVersion())
                        _versionsToUpdateLanguageDependent.Add(script);
                    if (!codexImagesScript.IsLastVersion())
                        _versionsToUpdateLanguageDependent.Add(codexImagesScript);
                    if (!codexImagesScript2.IsLastVersion())
                        _versionsToUpdateLanguageDependent.Add(codexImagesScript2);
                }
            }
            catch (Exception)
            {
                StopAllDownloads();
            }
        }
    }
}