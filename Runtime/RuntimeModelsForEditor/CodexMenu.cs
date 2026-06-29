using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

/// <summary>
/// Represents the codex menu data for a specific language.
/// </summary>
[Serializable]
public class CodexMenu
{
    public LanguageList language;
    public List<CodexItem> items = new List<CodexItem>();
}

/// <summary>
/// Represents one item inside the codex menu.
/// </summary>
[Serializable]
public class CodexItem
{
    /// <summary>
    /// Display name shown in the codex UI.
    /// </summary>
    public string name;

    /// <summary>
    /// Action performed when the codex item is selected.
    /// </summary>
    public MenuActionType actionType;

    /// <summary>
    /// Action parameter. For normal POIs this points to the directions JSON target.
    /// </summary>
    public string parameter;

    /// <summary>
    /// Unique id of the Point of Interest associated with this codex item.
    /// </summary>
    [FormerlySerializedAs("poiId")] public string poiId;
}