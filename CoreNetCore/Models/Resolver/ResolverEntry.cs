using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CoreNetCore.Models
{
    public class ResolverEntry
    {
        [JsonProperty("namespace")]
        public string Namespace { get; set; }

        public string service { get; set; }

        public int? version { get; set; }

        public int? sub_version { get; set; }

        public string type { get; set; }

        public LinkEntry[] link { get; set; }

        public bool result { get; set; }

        public string error { get; set; }
    }

    public class LinkEntry
    {
        public string name { get; set; }
        public string type { get; set; }
    }
}
