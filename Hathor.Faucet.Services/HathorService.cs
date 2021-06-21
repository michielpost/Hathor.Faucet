﻿using Hathor.Faucet.Services.Models;
using Hathor.Models.Requests;
using Hathor.Models.Responses;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hathor.Faucet.Services
{
    /// <summary>
    /// Responsible for interacting with Hathor node
    /// </summary>
    public class HathorService
    {
        private readonly HathorConfig hathorConfig;
        private readonly FaucetConfig faucetConfig;
        private readonly IHathorWalletApi client;
        private readonly IHathorNodeApi nodeClient;
        private readonly IMemoryCache memoryCache;
        private const string CACHE_KEY_ADDRESS = "address";
        private const string CACHE_KEY_FUNDS = "funds";

        private const string WALLET_ID = "faucet-wallet";

        public HathorService(IOptions<HathorConfig> hathorConfigOptions, IOptions<FaucetConfig> faucetConfigOptions, IMemoryCache memoryCache)
        {
            this.hathorConfig = hathorConfigOptions.Value;
            this.faucetConfig = faucetConfigOptions.Value;

            if (string.IsNullOrEmpty(hathorConfig.BaseUrl))
                throw new ArgumentNullException(nameof(hathorConfig.BaseUrl));
            if (string.IsNullOrEmpty(hathorConfig.ApiKey))
                throw new ArgumentNullException(nameof(hathorConfig.ApiKey));
            if (string.IsNullOrEmpty(hathorConfig.FullNodeBaseUrl))
                throw new ArgumentNullException(nameof(hathorConfig.FullNodeBaseUrl));

            client = HathorClient.GetWalletClient(hathorConfig.BaseUrl, WALLET_ID, hathorConfig.ApiKey);
            nodeClient = HathorClient.GetNodeClient(hathorConfig.FullNodeBaseUrl);

            this.memoryCache = memoryCache;
        }

        /// <summary>
        /// Check if address is empty
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<bool> CheckIsAddressEmptyAsync(string address)
        {
            var balance = await nodeClient.GetBalanceForAddress(address);

            //User already had transactions, not allowed to use the faucet
            if (balance.TotalTransactions > 0)
                return false;

            //User already had funds, not allowed to use the faucet
            if (balance.TokensData.Where(x => x.Value.Received > 0).Any())
                return false;

            return true;
        }

        /// <summary>
        /// Send HTR to address
        /// </summary>
        /// <param name="address"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public async Task<SendTransactionResponse> SendHathorAsync(string address, int amount)
        {
            if (amount > faucetConfig.MaxPayoutCents || amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), $"Amount must be > 0 and <= {faucetConfig.MaxPayoutCents}");

            await StartWalletAsync();

            var req = new SendTransactionSimpleRequest(address, amount);

            var result = await client.SendTransaction(req);

            //Invalidate funds cache
            memoryCache.Remove(CACHE_KEY_FUNDS);
            memoryCache.Remove(WalletTransactionService.CACHE_KEY_HISTORY);

            return result;
        }

        /// <summary>
        /// Start Hathor headless wallet
        /// </summary>
        /// <returns></returns>
        public async Task StartWalletAsync()
        {
            var status = await client.GetStatus();
            if (!status.Success)
            {
                var req = new StartRequest(WALLET_ID, "default");
                var response = await client.Start(req);

                //Wait untill wallet is started
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        /// <summary>
        /// Get faucet wallet and cache it
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetAddressAsync()
        {
            var result = await memoryCache.GetOrCreateAsync<AddressResponse>(CACHE_KEY_ADDRESS, async (cache) =>
            {
                await StartWalletAsync();

                var addressResult = await client.GetAddress(0);

                if (string.IsNullOrEmpty(addressResult.Address))
                    cache.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(15);
                else
                    cache.SlidingExpiration = TimeSpan.FromHours(1);

                return addressResult;
            });

            return result.Address;
        }

        /// <summary>
        /// Get current funds in faucet
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetCurrentFundsAsync()
        {
            var result = await memoryCache.GetOrCreateAsync<BalanceResponse>(CACHE_KEY_FUNDS, async (cache) =>
            {
                var balanceResult = await client.GetBalance();

                cache.SlidingExpiration = TimeSpan.FromHours(1);

                return balanceResult;
            });

            return result.Available ?? 0;
        }

        /// <summary>
        /// Get amount faucet will pay based on current funds in faucet
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetCurrentPayoutAsync()
        {
            var funds = await GetCurrentFundsAsync();
            if (funds > 150)
                return faucetConfig.MaxPayoutCents;
            else if (funds > 0)
                return Math.Min(1, faucetConfig.MaxPayoutCents);
            else
                return 0;
        }

        /// <summary>
        /// Check if address is valid
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<bool> IsAddressValidAsync(string address)
        {
            var result = await nodeClient.ValidateAddress(address);

            return result.Valid;
        }
    }
}
