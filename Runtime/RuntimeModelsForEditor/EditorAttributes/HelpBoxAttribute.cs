using UnityEngine;
/// <summary>
/// This class is a customized unity PropertyAttribute that allows to display an HelpBox over unity Editor fields.
/// The available types for the HelpBox can be consulted in the script <see cref="HelpBoxType"/>
/// </summary>
public class HelpBoxAttribute : PropertyAttribute
{
    public readonly string Message;
    public readonly HelpBoxType Type;

    public HelpBoxAttribute(string message, HelpBoxType type = HelpBoxType.Info)
    {
        Message = message;
        Type = type;
    }
}
