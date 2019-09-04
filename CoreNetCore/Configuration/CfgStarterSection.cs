using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace CoreNetCore.Configuration
{
    public class CfgStarterSection : CfgSectionBase
    {
        public string requestexchangename { get; set; }
        public string responseexchangename { get; set; }
        public string requestdispatcherexchangename { get; set; }
        public int? cacheEntryTTL_sec { get; set; }
        public int? cachecapacity { get; set; }
        public int? pingperiod_ms { get; set; }
        public int? cache_renew_period_ms { get; set; }

        private CfgThis This { get; set; }

        public CfgThis _this => This;

        public override bool Validate()
        {
            return ValidateChild(_this) && base.Validate();
        }
    }

    public class CfgThis : CfgSectionBase
    {
        public string _namespace => Namespace;

        [Required]
        private string Namespace { get; set; }

        [Required]
        public string servicename { get; set; }

        [Required]
        public int? majorversion { get; set; }

        
        public string subversion { get; set; }
        public string ttl { get; set; }
 
    }
}