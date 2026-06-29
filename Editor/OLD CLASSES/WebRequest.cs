using UnityEngine.Networking;

namespace Theoden.Editor
{
    public static class WebRequest
    {
        public static string username, password, token;

        public static UnityWebRequest Post(string url, string json)
        { 
            return UnityWebRequest.PostWwwForm(url, json);
        }
    
    }
}