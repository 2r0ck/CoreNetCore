using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;

namespace CoreNetCore.Utils
{
    public static class Extensions
    {

        public static bool? GetBooleanValue(this IConfiguration config, string key, bool assert = false)
        {
            var value = GetStrValue(config, key, assert);
            bool res =false;

            if (bool.TryParse(value, out res))
            {
                return res;
            }

            if (assert)
            {
                throw new CoreException($"Config key parse error [${key}]");
            }
            return null;
        }

        public static string GetStrValue(this IConfiguration config, string key, bool assert = false)
        {
            var value = config.GetValue<string>(key);
            if (assert && string.IsNullOrEmpty(value))
            {
                throw new CoreException($"Config key [${key}] not declared.");
            }
            return value;
        }

        public static int? GetIntValue(this IConfiguration config, string key, bool assert = false)
        {
            var value = GetStrValue(config, key, assert);
            int res = 0;

            if (int.TryParse(value, out res))
            {
                return res;
            }

            if (assert)
            {
                throw new CoreException($"Config key parse error [${key}]");
            }
            return null;
        }

        public static string ToJson<T>(this T obj,bool assert=false) where T : class
        {
            try
            {
                if (obj == null) return string.Empty;
                return Newtonsoft.Json.JsonConvert.SerializeObject(obj,new JsonSerializerSettings(){NullValueHandling = NullValueHandling.Ignore });
            }
            catch (Exception ex)
            {
                var msg = "Object to Json serialize error";
                if (assert)
                {
                    throw new CoreException(msg, ex);
                }
                return string.Empty;
            }
        }

        public static T FromJson<T>(this string str, bool assert = false) where T : class
        {
            try
            {
                if (str == null) return null;
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(str);
            }
            catch (Exception ex)
            {
                var msg = "Object to Json serialize error";
                if (assert)
                {
                    throw new CoreException(msg, ex);
                }
                return null;
            }
        }
    }
}