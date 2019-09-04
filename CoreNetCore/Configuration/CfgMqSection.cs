using System.ComponentModel.DataAnnotations;

namespace CoreNetCore.Configuration
{
    public class CfgMqSection : CfgSectionBase
    {
        
        public bool? autodelete { get; set; }
        public bool? durable { get; set; }
        [Required]
        public int healthcheckPort { get; set; }
        [Required]
        public int maxRecoveryCount { get; set; }
        [Required]
        public string networkRecoveryInterval { get; set; }
        [Required]
        public Host host { get; set; }
    }

    public class Host
    {
        [Required]
        public string host { get; set; }
        [Required]
        public Mserv mserv { get; set; }
        [Required]
        public string port { get; set; }
    }

    public class Mserv
    {
        [Required]
        public string username { get; set; }
        [Required]
        public string password { get; set; }
    }
}