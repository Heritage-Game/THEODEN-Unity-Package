#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
/// <summary>
/// This class is used to apply a decorator - a help box that appears on top - on the fields of the templates that
/// have a necessity of being further of more clearly explained to the user.
/// </summary>
[CustomPropertyDrawer(typeof(HelpBoxAttribute))]
public class HelpBoxDrawer : DecoratorDrawer
{
    private const float HelpBoxHeight = 42f;
    private const float Spacing = 4f;

    public override float GetHeight()
    {
        return HelpBoxHeight+Spacing;
    }

    public override void OnGUI(Rect position)
    {
        var helpBox = (HelpBoxAttribute)attribute;

        position.height -= Spacing;

        EditorGUI.HelpBox(
            position,
            helpBox.Message,
            ToUnityMessageType(helpBox.Type)
        );
    }

    private MessageType ToUnityMessageType(HelpBoxType type)
    {
        return type switch
        {
            HelpBoxType.Warning => MessageType.Warning,
            HelpBoxType.Error => MessageType.Error,
            HelpBoxType.None => MessageType.None,
            _ => MessageType.Info
        };
    }
}
#endif