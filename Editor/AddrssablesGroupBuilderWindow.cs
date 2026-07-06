using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Theoden.Editor
{
    /// <summary>
    /// Editor window che permette di creare facilmente label Addressables per tutti gli asset contenuti
    /// in una cartella del progetto (es. "Level 1" o "POI 1").
    /// 
    /// Funzionalità principali:
    /// - Seleziona una cartella (DefaultAsset) nel Project window
    /// - Genera una label suggerita (es. poi_level_1) o usa quella inserita dall'utente
    /// - Crea/recupera un Addressables Group denominato "POI_{label}" e aggiunge tutte le risorse della cartella
    /// - Imposta l'indirizzo (address) e la label su ogni entry
    /// - Salva le modifiche alle Addressable settings
    /// 
    /// Nota: questo script usa le API editor di Addressables e deve risiedere in una cartella "Editor/".
    /// </summary>
    public class AddrssablesGroupBuilderWindow : EditorWindow
    {
        private DefaultAsset _folderAsset;
        private string _folderPath;

        private string _suggestedLabel;
        private string _labelName;

        private Vector2 _scroll;

        private List<string> _assetPreview = new();

        [MenuItem("THEODEN/5.Create Addressables Group/Create Group from a folder")]
        public static void ShowWindow()
        {
            var window = GetWindow<AddrssablesGroupBuilderWindow>("Create Addressable Group from a folder");
            window.minSize = new Vector2(520, 320);
        }

        private void OnEnable()
        {
            RefreshFromSelection();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Create Addressables group", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _folderAsset = (DefaultAsset)EditorGUILayout.ObjectField(
                "Folder",
                _folderAsset,
                typeof(DefaultAsset),
                false);

            if (EditorGUI.EndChangeCheck())
                RefreshFromSelection();

            using (new EditorGUI.DisabledScope(_folderAsset == null))
            {
                EditorGUILayout.LabelField("Detected Path", _folderPath);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Suggested Label", _suggestedLabel);
                _labelName = EditorGUILayout.TextField("Label", _labelName);

                EditorGUILayout.Space();

                if (GUILayout.Button("Create / Update Addressables"))
                {
                    AssignFolderAsAddressable(_folderPath, _labelName);
                }

                if (GUILayout.Button("Refresh Preview"))
                {
                    RefreshPreview();
                }

                EditorGUILayout.Space();

                DrawPreview();
            }
        }

        void DrawPreview()
        {
            EditorGUILayout.LabelField($"Assets in folder ({_assetPreview.Count})");

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(160));

            foreach (var p in _assetPreview)
            {
                EditorGUILayout.LabelField(Path.GetFileName(p));
            }

            EditorGUILayout.EndScrollView();
        }

        void RefreshFromSelection()
        {
            if (_folderAsset == null)
                return;

            var path = AssetDatabase.GetAssetPath(_folderAsset);

            if (!AssetDatabase.IsValidFolder(path))
                return;

            _folderPath = path;

            GenerateSuggestedLabel();

            RefreshPreview();
        }

        void GenerateSuggestedLabel()
        {
            var folderName = Path.GetFileName(_folderPath)
                .ToLowerInvariant()
                .Replace(" ", "_");

            if (!folderName.StartsWith("poi_"))
                _suggestedLabel = $"poi_{folderName}";
            else
                _suggestedLabel = folderName;

            if (string.IsNullOrEmpty(_labelName))
                _labelName = _suggestedLabel;
        }

        void RefreshPreview()
        {
            _assetPreview.Clear();

            var guids = AssetDatabase.FindAssets("", new[] { _folderPath });

            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);

                if (AssetDatabase.IsValidFolder(path))
                    continue;

                _assetPreview.Add(path);
            }
        }

        static void AssignFolderAsAddressable(string folderPath, string label)
        {
            if (string.IsNullOrEmpty(folderPath) || string.IsNullOrEmpty(label))
            {
                Debug.LogError("Folder or label missing.");
                return;
            }

            var settings = AddressableAssetSettingsDefaultObject.Settings;

            if (settings == null)
            {
                Debug.LogError("Addressables settings not found.");
                return;
            }

            var groupName = $"GROUP_{label}";

            var group = settings.FindGroup(groupName);

            if (group == null)
            {
                group = settings.CreateGroup(
                    groupName,
                    false,
                    false,
                    false,
                    new List<AddressableAssetGroupSchema>(),
                    typeof(BundledAssetGroupSchema),
                    typeof(ContentUpdateGroupSchema));

                Debug.Log($"Created group {groupName}");
            }

            if (!settings.GetLabels().Contains(label))
            {
                settings.AddLabel(label);
            }

            string guid = AssetDatabase.AssetPathToGUID(folderPath);

            var entry = settings.CreateOrMoveEntry(guid, group);

            entry.SetLabel(label, true);

            entry.address = Path.GetFileName(folderPath);

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);

            EditorUtility.SetDirty(settings);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Folder '{folderPath}' now Addressable with label '{label}'");
        }
    }
}
