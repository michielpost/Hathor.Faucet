using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hathor.Faucet.Services.Models
{
    public class HathorConfig
    {
        public string? BaseUrl { get; set; }
        public string? ApiKey { get; set; }
        public string? Seed { get; set; }

        public string? FullNodeBaseUrl { get; set; }

    }
}
