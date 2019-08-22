using Newtonsoft.Json;
using System.Collections.Generic;

namespace CoreNetCore.Models
{
    public class CacheItem
    {
        public const string SELF_TOKEN = "self_link";
        [JsonProperty("namespace")]
        public string Namespace { get; set; }

        public string service { get; set; }

        public int? version { get; set; }

        public int? sub_version { get; set; }
        public bool self_link { get; set; }

        public Dictionary<string, string> links { get; }

        public CacheItem()
        {
            links = new Dictionary<string, string>();
        }

        public void AddLink(string key, string value)
        {
            if (links.ContainsKey(key))
            {
                links[key] = value;
            }
            else
            {
                links.Add(key, value);
            }
        }
    }
}