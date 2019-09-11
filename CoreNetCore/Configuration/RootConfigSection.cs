using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace CoreNetCore.Configuration
{
    public class CfgRuSection : CfgSectionBase
    {
        [Required]
        public CfgSpinosaSection spinosa { get; set; }
    }

    public class CfgSpinosaSection : CfgSectionBase
    {
        public CfgAuthSection auth { get; set; }

        [Required]
        public CfgStarterSection starter { get; set; }

        [Required]
        public CfgMqSection mq { get; set; }

    }

    public class CfgAuthSection : CfgSectionBase
    {
        public CfgCryptoSection crypto { get; set; }
    }

    public class CfgCryptoSection : CfgSectionBase
    {
        public string SALT { get; set; }
        public string IV { get; set; }
    }

}

