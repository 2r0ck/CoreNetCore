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
        public int networkRecoveryInterval { get; set; }
        [Required]
        public Host host { get; set; }

        public MqArguments queue { get; set; }
        public MqArguments exchange { get; set; }

        public ushort? heartbeat { get; set; }

        public ushort? prefetch { get; set; }

        
    }

    public class Host
    {
        [Required]
        public string host { get; set; }
        [Required]
        public Mserv mserv { get; set; }
        [Required]
        public int port { get; set; }
    }

    public class Mserv
    {
        [Required]
        public string username { get; set; }
        [Required]
        public string password { get; set; }
    }

    public class MqArguments
    {
        public int? ttl { get; set; }
    }
}