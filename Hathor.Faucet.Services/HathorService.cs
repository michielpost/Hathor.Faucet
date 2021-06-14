using Hathor.Faucet.Services.Models;
using Hathor.Models.Requests;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace Hathor.Faucet.Services
{
    public class HathorService
    {
        private readonly HathorConfig hathorConfig;
        private readonly IHathorWalletApi client;

        private const string WALLET_ID = "faucet-wallet";


        public HathorService(IOptions<HathorConfig> hathorConfigOptions)
        {
            this.hathorConfig = hathorConfigOptions.Value;

            if (string.IsNullOrEmpty(hathorConfig.BaseUrl))
                throw new ArgumentNullException(nameof(hathorConfig.BaseUrl));
            if (string.IsNullOrEmpty(hathorConfig.ApiKey))
                throw new ArgumentNullException(nameof(hathorConfig.ApiKey));

            client = HathorClient.GetWalletClient(hathorConfig.BaseUrl, WALLET_ID, hathorConfig.ApiKey);
        }

        public async Task StartWallet()
        {
            var status = await client.GetStatus();
            if (!status.Success)
            {
                var req = new StartRequest(WALLET_ID, "default");
                var response = await client.Start(req);
            }
        }

        public async Task<string> GetAddressAsync()
        {
            await StartWallet();

            var result = await client.GetAddress(0);

            return result.Address;
        }

        public async Task<int> GetCurrentFundsAsync()
        {
            var result = await client.GetBalance();

            return result.Available ?? 0;
        }
    }
}
