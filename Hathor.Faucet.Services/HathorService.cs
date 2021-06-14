using Hathor.Faucet.Services.Models;
using Microsoft.Extensions.Options;
using System;

namespace Hathor.Faucet.Services
{
    public class HathorService
    {
        private readonly HathorConfig hathorConfig;

        public HathorService(IOptions<HathorConfig> hathorConfig)
        {
            this.hathorConfig = hathorConfig.Value;
        }
    }
}
