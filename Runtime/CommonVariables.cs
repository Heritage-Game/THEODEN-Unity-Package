/*
 * still uncompleted
 * used to pass messages to other scenes/scripts
 * cannot remove
 */
/// <summary>
/// This class contains information that is universally needed in the code.
/// Save the URL to the remote server here, as well as the access tokens and any other field that needs
/// to be easily retrievable in the code.
/// </summary>
public static class CommonVariables
{
    public const string URL = "https://landofkeemar.keemar.it";
    public static string AccessToken = string.Empty;
    public static string PrefabName = string.Empty;
    public static string JsonString = string.Empty;
    public static string DeviceId = string.Empty;
    public static string Language = string.Empty;
    public static bool ThemeStatus = true;
    public static bool SFXStatus = true;
}