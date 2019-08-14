using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace CoreNetCore.Utils
{
    public static class Extensions
    {
        public static string GetStrValue(this IConfiguration config, string key, bool assert = false)
        {
            var value = config.GetValue<string>(key);
            if (assert && string.IsNullOrEmpty(value))
            {
                throw new CoreException($"Config key [${key}] not declared.");
            }
            return value;
        }

        public static string  ToJson<T>(this T obj) where T:class
        {
            try
            {
                if (obj == null) return string.Empty;
                return  Newtonsoft.Json.JsonConvert.SerializeObject(obj);
            }
            catch
            {
                return "[Object to Json serialize error]";
            }
        }

    }
}
