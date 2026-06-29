using System;
using ContentLoading;
using UnityEngine;

public class RuntimeLoaderTest : MonoBehaviour
{
    private async void Start()
    {
        try
        {
            CodexMenu codex = await TheodenRuntimeContentLoader.LoadCodexAsync(LanguageList.ENG);

            Debug.Log($"Codex loaded. Language: {codex.language}, Items: {codex.items.Count}");
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }
    }
}
