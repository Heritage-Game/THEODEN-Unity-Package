using System.Collections.Generic;
using Newtonsoft.Json;

namespace Theoden.Editor
{
    public class Login
    {
        public static void GetToken()
        {
            var request = WebRequest.Post(CommonVariables.URL + "/login",
                "{\"username\": \""+WebRequest.username+"\", \"password\": \""+WebRequest.password+"\"}");
            var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(request.downloadHandler.text);
            WebRequest.token = dict["token"];
        }
    }
}