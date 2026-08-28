// THIS FILE BELONGS IN AN EDITOR FOLDER (e.g. Assets/Editor)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

/// <summary>
/// Editor utility that performs Addressables content builds and, when remote loading is enabled,
/// uploads changed/new Addressables files to a remote server.
/// </summary>
/// <remarks>
/// This utility supports two build modes:
///
/// 1. Local mode:
/// If the active Addressables profile does not define a valid RemoteLoadPath,
/// the tool performs a normal local Addressables build and skips upload.
/// In this mode, Addressables bundles are built as local content and included with the app build.
///
/// 2. Remote mode:
/// If RemoteLoadPath is configured, the tool builds Addressables content, detects changed files
/// using SHA256 hashes, uploads only changed/new files to the configured server endpoint,
/// and stores an upload manifest so future uploads can be incremental.
///
/// Platform separation should be handled in the Addressables profile paths using tokens such as:
/// [BuildTarget]
///
/// Example:
/// RemoteLoadPath = https://example.com/theoden/[BuildTarget]
/// </remarks>
public static class UpdateServerAddressables
{
    /// <summary>
    /// Menu item: THEODEN → Build & Upload Changed Content to server.
    /// </summary>
    /// <remarks>
    /// If RemoteLoadPath is empty, this method builds local Addressables content only and skips upload.
    ///
    /// If RemoteLoadPath is configured, this method builds remote Addressables content and uploads
    /// changed files to the server.
    /// </remarks>
    //[MenuItem("THEODEN/6.Build & Upload Changed Content to server %#u")]
    public static void BuildAndUploadChangedContent()
    {
        try
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

            if (settings == null)
            {
                Debug.LogError("[AddrIncUpload] Addressables not configured in this project.");
                return;
            }

            bool hasRemoteLoadPath = HasRemoteLoadPath(settings);

            if (!hasRemoteLoadPath)
            {
                Debug.Log(
                    "[AddrIncUpload] RemoteLoadPath is empty. " +
                    "Building local Addressables content only. Upload will be skipped."
                );

                BuildLocalAddressablesContent();
                return;
            }

            Debug.Log("[AddrIncUpload] RemoteLoadPath detected. Building remote Addressables content.");

            string buildPath = ResolveProfilePath(settings, "RemoteBuildPath");

            if (string.IsNullOrWhiteSpace(buildPath))
            {
                Debug.LogError("[AddrIncUpload] Could not resolve RemoteBuildPath. Check Addressables Profiles.");
                return;
            }

            buildPath = MakeAbsolutePath(buildPath);

            Directory.CreateDirectory(buildPath);

            bool contentUpdateSucceeded = TryBuildContentUpdate(settings, buildPath);

            if (!contentUpdateSucceeded)
            {
                bool fullBuildSucceeded = BuildFullAddressablesContent();

                if (!fullBuildSucceeded)
                    return;
            }

            UploadChangedFiles(buildPath);
        }
        catch (Exception ex)
        {
            Debug.LogError("[AddrIncUpload] Exception: " + ex.Message + "\n" + ex.StackTrace);
        }
    }

    // ============================================================
    // BUILD MODES
    // ============================================================

    /// <summary>
    /// Builds Addressables content for local app usage.
    /// </summary>
    /// <remarks>
    /// This mode is used when the active Addressables profile does not have a RemoteLoadPath.
    ///
    /// Addressables bundles may still be generated, but they are built as local content and should be
    /// included with the application build instead of uploaded to a remote server.
    /// </remarks>
    private static void BuildLocalAddressablesContent()
    {
        Debug.Log("[AddrIncUpload] Performing local Addressables content build...");

        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult buildResult);

        if (buildResult != null && !string.IsNullOrEmpty(buildResult.Error))
        {
            Debug.LogError("[AddrIncUpload] Local Addressables build failed: " + buildResult.Error);
            return;
        }

        Debug.Log("[AddrIncUpload] Local Addressables content build finished.");
    }

    /// <summary>
    /// Builds all Addressables content from scratch.
    /// </summary>
    /// <returns>
    /// True if the build completed successfully; otherwise false.
    /// </returns>
    private static bool BuildFullAddressablesContent()
    {
        Debug.Log("[AddrIncUpload] Performing full Addressables content build...");

        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult buildResult);

        if (buildResult != null && !string.IsNullOrEmpty(buildResult.Error))
        {
            Debug.LogError("[AddrIncUpload] Full content build failed: " + buildResult.Error);
            return false;
        }

        Debug.Log("[AddrIncUpload] Full content build finished.");
        return true;
    }

    /// <summary>
    /// Attempts to build an Addressables content update using a previous content state file.
    /// </summary>
    /// <param name="settings">
    /// Active Addressables settings.
    /// </param>
    /// <param name="buildPath">
    /// Absolute remote build path where Addressables output is generated.
    /// </param>
    /// <returns>
    /// True if the content update build succeeded; otherwise false.
    /// </returns>
    private static bool TryBuildContentUpdate(
        AddressableAssetSettings settings,
        string buildPath)
    {
        string contentStateFile = Path.Combine(buildPath, "addressables_content_state.bin");

        if (!File.Exists(contentStateFile))
        {
            Debug.Log("[AddrIncUpload] No previous content state found. Full build will be used.");
            return false;
        }

        Debug.Log("[AddrIncUpload] Previous content state found. Attempting Content Update build...");

        try
        {
            object result = ContentUpdateScript.BuildContentUpdate(settings, contentStateFile);

            if (result == null)
            {
                Debug.LogWarning("[AddrIncUpload] Content Update Build returned null. Falling back to full build.");
                return false;
            }

            string error = TryGetBuildResultError(result);

            if (!string.IsNullOrWhiteSpace(error))
            {
                Debug.LogWarning(
                    "[AddrIncUpload] Content Update Build reported error: " +
                    error +
                    " Falling back to full build."
                );

                return false;
            }

            Debug.Log("[AddrIncUpload] Content Update Build finished.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning(
                "[AddrIncUpload] Content Update Build failed: " +
                ex.Message +
                " Falling back to full build."
            );

            return false;
        }
    }

    // ============================================================
    // UPLOAD FLOW
    // ============================================================

    /// <summary>
    /// Detects changed files in the build folder and uploads them to the configured remote endpoint.
    /// </summary>
    /// <param name="buildPath">
    /// Absolute path to the Addressables remote build output folder.
    /// </param>
    private static void UploadChangedFiles(string buildPath)
    {
        string manifestPath = Path.Combine(buildPath, "upload_manifest.json");

        Dictionary<string, string> previousManifest = LoadManifest(manifestPath);

        string[] allFiles = Directory.GetFiles(buildPath, "*", SearchOption.AllDirectories)
            .Where(file => !file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
            .Where(file => !file.EndsWith("upload_manifest.json", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        List<string> changedFiles = new List<string>();

        Dictionary<string, string> newManifest =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (string file in allFiles)
        {
            string relativePath = MakeRelativePath(buildPath, file).Replace('\\', '/');
            string hash = ComputeSHA256(file);

            newManifest[relativePath] = hash;

            bool isNewFile = !previousManifest.TryGetValue(relativePath, out string oldHash);
            bool hasChanged = !string.Equals(oldHash, hash, StringComparison.OrdinalIgnoreCase);

            if (isNewFile || hasChanged)
                changedFiles.Add(file);
        }

        if (changedFiles.Count == 0)
        {
            Debug.Log("[AddrIncUpload] No changed files detected. Nothing to upload.");
            return;
        }

        Debug.Log("[AddrIncUpload] Changed/new files detected: " + changedFiles.Count);

        string uploadEndpoint = CommonVariables.URL;
        string authToken = CommonVariables.AccessToken;

        if (string.IsNullOrWhiteSpace(uploadEndpoint))
        {
            Debug.LogError("[AddrIncUpload] Upload endpoint is empty. Check CommonVariables.URL.");
            return;
        }

        bool uploadOk = UploadFilesHttpMultipart(
            uploadEndpoint,
            changedFiles,
            buildPath,
            authToken
        ).GetAwaiter().GetResult();

        if (!uploadOk)
        {
            Debug.LogError("[AddrIncUpload] Upload failed. Manifest not updated.");
            return;
        }

        SaveManifest(manifestPath, newManifest);

        Debug.Log("[AddrIncUpload] Upload complete and manifest updated.");
    }

    /// <summary>
    /// Uploads a set of files to the specified upload URL using multipart/form-data POST.
    /// </summary>
    /// <param name="uploadUrl">
    /// HTTP endpoint that accepts multipart uploads.
    /// </param>
    /// <param name="files">
    /// Absolute file paths to upload.
    /// </param>
    /// <param name="baseFolder">
    /// Base folder used to compute relative names for uploaded files.
    /// </param>
    /// <param name="authToken">
    /// Optional bearer auth token.
    /// </param>
    /// <returns>
    /// True if the server responded with a success status code; otherwise false.
    /// </returns>
    private static async Task<bool> UploadFilesHttpMultipart(
        string uploadUrl,
        List<string> files,
        string baseFolder,
        string authToken = null)
    {
        try
        {
            using HttpClient client = new HttpClient();

            if (!string.IsNullOrWhiteSpace(authToken))
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", authToken);

            using MultipartFormDataContent multipartContent = new MultipartFormDataContent();

            foreach (string file in files)
            {
                string relativePath = MakeRelativePath(baseFolder, file).Replace('\\', '/');

                byte[] bytes = File.ReadAllBytes(file);
                ByteArrayContent byteContent = new ByteArrayContent(bytes);

                byteContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                {
                    Name = "\"files\"",
                    FileName = "\"" + relativePath + "\""
                };

                multipartContent.Add(byteContent, "files", relativePath);
            }

            Debug.Log("[AddrIncUpload] Uploading " + files.Count + " files to " + uploadUrl + " ...");

            HttpResponseMessage response = await client.PostAsync(uploadUrl, multipartContent);

            if (!response.IsSuccessStatusCode)
            {
                string responseText = await response.Content.ReadAsStringAsync();

                Debug.LogError(
                    "[AddrIncUpload] Upload failed: " +
                    response.StatusCode +
                    " - " +
                    responseText
                );

                return false;
            }

            Debug.Log("[AddrIncUpload] Server response: " + response.StatusCode);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("[AddrIncUpload] Upload exception: " + ex.Message);
            return false;
        }
    }

    // ============================================================
    // PROFILE / PATH HELPERS
    // ============================================================

    /// <summary>
    /// Checks whether the active Addressables profile has a valid RemoteLoadPath.
    /// </summary>
    /// <param name="settings">
    /// Active Addressables settings.
    /// </param>
    /// <returns>
    /// True if RemoteLoadPath is configured; otherwise false.
    /// </returns>
    private static bool HasRemoteLoadPath(AddressableAssetSettings settings)
    {
        string remoteLoadPath = ResolveProfilePath(settings, "RemoteLoadPath");

        if (string.IsNullOrWhiteSpace(remoteLoadPath))
            return false;

        if (remoteLoadPath.Contains("[RemoteLoadPath]"))
            return false;

        return true;
    }

    /// <summary>
    /// Resolves a profile variable value from the active Addressables profile.
    /// </summary>
    /// <param name="settings">
    /// AddressableAssetSettings instance.
    /// </param>
    /// <param name="variableName">
    /// Name of the profile variable, for example RemoteBuildPath or RemoteLoadPath.
    /// </param>
    /// <returns>
    /// Evaluated string value, or null if it cannot be resolved.
    /// </returns>
    private static string ResolveProfilePath(
        AddressableAssetSettings settings,
        string variableName)
    {
        try
        {
            AddressableAssetProfileSettings profileSettings = settings.profileSettings;
            string profileId = settings.activeProfileId;

            string evaluated = profileSettings.EvaluateString(
                profileId,
                "[" + variableName + "]"
            );

            if (string.IsNullOrWhiteSpace(evaluated) ||
                evaluated.Contains("[") ||
                evaluated.Contains("]"))
            {
                evaluated = profileSettings.GetValueByName(profileId, variableName);
            }

            return evaluated;
        }
        catch (Exception ex)
        {
            Debug.LogWarning(
                "[AddrIncUpload] Could not resolve profile variable " +
                variableName +
                ": " +
                ex.Message
            );

            return null;
        }
    }

    /// <summary>
    /// Converts a path to an absolute path if it is currently relative.
    /// </summary>
    /// <param name="path">
    /// Input path.
    /// </param>
    /// <returns>
    /// Absolute path.
    /// </returns>
    private static string MakeAbsolutePath(string path)
    {
        if (Path.IsPathRooted(path))
            return path;

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(projectRoot, path);
    }

    /// <summary>
    /// Creates a relative path string from a base folder to a full path.
    /// </summary>
    /// <param name="baseFolder">
    /// Base folder path.
    /// </param>
    /// <param name="fullPath">
    /// Full file path.
    /// </param>
    /// <returns>
    /// Relative path string.
    /// </returns>
    private static string MakeRelativePath(
        string baseFolder,
        string fullPath)
    {
        Uri baseUri = new Uri(
            baseFolder.EndsWith(Path.DirectorySeparatorChar.ToString())
                ? baseFolder
                : baseFolder + Path.DirectorySeparatorChar
        );

        Uri fullUri = new Uri(fullPath);

        return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString());
    }

    // ============================================================
    // MANIFEST / HASH HELPERS
    // ============================================================

    /// <summary>
    /// Loads the upload manifest saved from the previous successful upload.
    /// </summary>
    /// <param name="manifestPath">
    /// Absolute path to the manifest file.
    /// </param>
    /// <returns>
    /// Dictionary of relative file paths to SHA256 hashes.
    /// </returns>
    private static Dictionary<string, string> LoadManifest(string manifestPath)
    {
        try
        {
            if (!File.Exists(manifestPath))
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            string json = File.ReadAllText(manifestPath);

            Dictionary<string, string> dictionary =
                JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            return dictionary ??
                   new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[AddrIncUpload] Failed to read manifest: " + ex.Message);
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Saves the upload manifest to disk.
    /// </summary>
    /// <param name="manifestPath">
    /// Absolute path to write the manifest file.
    /// </param>
    /// <param name="manifest">
    /// Dictionary of relative file paths to SHA256 hashes.
    /// </param>
    private static void SaveManifest(
        string manifestPath,
        Dictionary<string, string> manifest)
    {
        try
        {
            string json = JsonConvert.SerializeObject(manifest, Formatting.Indented);
            File.WriteAllText(manifestPath, json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[AddrIncUpload] Failed to write manifest: " + ex.Message);
        }
    }

    /// <summary>
    /// Computes the SHA256 hex digest of the specified file.
    /// </summary>
    /// <param name="filePath">
    /// Path to the file to hash.
    /// </param>
    /// <returns>
    /// Lowercase hex string representing the SHA256 of the file contents.
    /// </returns>
    private static string ComputeSHA256(string filePath)
    {
        using SHA256 sha = SHA256.Create();
        using FileStream stream = File.OpenRead(filePath);

        byte[] hashBytes = sha.ComputeHash(stream);

        StringBuilder builder = new StringBuilder(64);

        foreach (byte hashByte in hashBytes)
            builder.Append(hashByte.ToString("x2"));

        return builder.ToString();
    }

    /// <summary>
    /// Attempts to read the Error property from an Addressables build result object.
    /// </summary>
    /// <param name="buildResult">
    /// Build result object returned by Addressables.
    /// </param>
    /// <returns>
    /// Error text if available; otherwise null.
    /// </returns>
    private static string TryGetBuildResultError(object buildResult)
    {
        if (buildResult == null)
            return null;

        System.Reflection.PropertyInfo errorProperty =
            buildResult.GetType().GetProperty("Error");

        if (errorProperty == null)
            return null;

        return errorProperty.GetValue(buildResult) as string;
    }
}