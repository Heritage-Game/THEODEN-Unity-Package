using System.Linq;
using Core.Models;
using Newtonsoft.Json;

/// <summary>
/// Converts exported codex JSON data into the runtime codex model used by the Codex UI.
/// </summary>
public static class CodexJsonTranslator
{
    /// <summary>
    /// Converts an exported codex JSON string into a runtime <see cref="CodexModel"/>.
    /// </summary>
    /// <param name="json">
    /// Exported codex JSON string.
    /// </param>
    /// <returns>
    /// Runtime codex model with initialized item states.
    /// </returns>
    public static CodexModel FromJson(string json)
    {
        CodexMenu root = JsonConvert.DeserializeObject<CodexMenu>(json);

        if (root == null || root.items == null)
        {
            return new CodexModel
            {
                menuTitle = "",
                items = new()
            };
        }

        var model = new CodexModel
        {
            menuTitle = root.language.ToString(),
            items = root.items
                .Select((jsonItem, index) => new CodexItemDefinition
                {
                    levelTitle = jsonItem.name,
                    levelSubTitle = "",
                    actionType = jsonItem.actionType,
                    target = jsonItem.parameter,
                    poiId = jsonItem.poiId,
                    state = index == 0
                        ? CodexItemState.Directions
                        : CodexItemState.Locked
                })
                .ToList()
        };

        return model;
    }
}