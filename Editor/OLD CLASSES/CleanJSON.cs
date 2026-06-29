using System;
using System.Linq;

//using Defective.JSON;

namespace Theoden.Editor
{
    public static class CleanJson
    {
        public static string GetString(JSONObject field)
        {
            return field.ToString().Trim('"');
        }

        public static float GetFloat(JSONObject field)
        {
            float.TryParse(GetString(field), out var parsed);
            return parsed;
        }

        public static JSONObject Filter(JSONObject json, Func<JSONObject, bool> expression)
        {
            var filtered = new JSONObject(JSONObject.Type.ARRAY);
            foreach (var field in json.list.Where(expression)) filtered.Add(field);
            return filtered;
        }
    }
}