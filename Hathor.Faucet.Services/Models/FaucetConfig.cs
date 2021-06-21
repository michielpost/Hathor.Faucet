using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hathor.Faucet.Services.Models
{
    public class FaucetConfig
    {
        public HathorNetwork Network { get; set; }
        public int MaxPayoutCents { get; set; } = 2;
        public int TresholdAmountCents { get; set; } = 40;
        public string ExplorerUrl { get; set; } = "https://explorer.hathor.network/";

    }

    public enum HathorNetwork
    {
        Testnet,
        Mainnet
    }
}
