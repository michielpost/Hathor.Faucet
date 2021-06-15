using Hathor.Faucet.Services.Models;
using Hathor.Models.Requests;
using Hathor.Models.Responses;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace Hathor.Faucet.Services
{
    public class HathorService
    {
        private readonly HathorConfig hathorConfig;
        private readonly IHathorWalletApi client;
        private const int MAX_PAYOUT_CENTS = 2;

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

        public Task<SendTransactionResponse> SendHathorAsync(string address, int amount)
        {
            if (amount > MAX_PAYOUT_CENTS || amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), $"Amount must be > 0 and <= {MAX_PAYOUT_CENTS}");

            var req = new SendTransactionSimpleRequest(address, amount);

            return client.SendTransaction(req);
        }

        public async Task StartWalletAsync()
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
            await StartWalletAsync();

            var result = await client.GetAddress(0);

            return result.Address;
        }

        public async Task<int> GetCurrentFundsAsync()
        {
            var result = await client.GetBalance();

            return result.Available ?? 0;
        }

        public async Task<int> GetCurrentPayoutAsync()
        {
            var funds = await GetCurrentFundsAsync();
            if (funds > 150)
                return MAX_PAYOUT_CENTS;
            else if (funds > 0)
                return 1;
            else
                return 0;
        }
    }
}
