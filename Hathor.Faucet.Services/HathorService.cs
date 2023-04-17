using Hathor.Faucet.Services.Models;
using Hathor.Models.Requests;
using Hathor.Models.Responses;
using Hathor.Wallet;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
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
        private const string CACHE_KEY_TX = "tx";

        private const string WALLET_ID = "faucet-wallet-v2";

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
            var txHistory = await nodeClient.GetAddressHistory(address);

            //User already had transactions, not allowed to use the faucet
            if (txHistory.History.Any())
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
            req.Token = faucetConfig.Token;
            req.ChangeAddress = await this.GetAddressAsync();

            var result = await client.SendTransaction(req);

            //Invalidate funds cache
            memoryCache.Remove(CACHE_KEY_FUNDS);
            memoryCache.Remove(CACHE_KEY_TX);
            memoryCache.Remove(nameof(GetTotalPayoutAsync));
            memoryCache.Remove(WalletTransactionService.CACHE_KEY_HISTORY);
            memoryCache.Remove(WalletTransactionService.CACHE_KEY_TX_HISTORY);

            return result;
        }

        /// <summary>
        /// Start Hathor headless wallet
        /// </summary>
        /// <returns></returns>
        public async Task StartWalletAsync()
        {
            var status = await client.GetStatus();
            if (!status.Success || status.StatusCode != 3)
            {
                var req = new StartRequest(WALLET_ID, null, hathorConfig.Seed);
                var response = await client.Start(req);

                //Wait untill wallet is started
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        public async Task StartWalletCached()
        {
            var startResult = await memoryCache.GetOrCreateAsync<bool>("start-wallet", async (cache) =>
            {
                cache.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
                try
                {
                    await StartWalletAsync();
                }
                catch {
                    return false;
                }
                return true;
            });
        }

        /// <summary>
        /// Get faucet wallet and cache it
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetAddressAsync()
        {

            if (!string.IsNullOrEmpty(hathorConfig.Seed))
            {
                var network = faucetConfig.Network == Models.HathorNetwork.Mainnet ? Wallet.HathorNetwork.Mainnet : Wallet.HathorNetwork.Testnet;
                var wallet = new HathorWallet(HathorClient.GetNodeClient("https://node.explorer.hathor.network/v1a/"), network, hathorConfig.Seed);
                return wallet.GetAddress(0);
            }

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

            return result?.Address ?? string.Empty;
        }

        public async Task<List<Transaction>> GetLastTransactionsAsync()
        {
            var currentAddress = await GetAddressAsync();
            var result = await memoryCache.GetOrCreateAsync(CACHE_KEY_TX, async (cache) =>
            {
                var txResult = await client.GetTxHistory(12);

                var sent = txResult.Where(x => x.Inputs.Where(x => x.Decoded?.Address == currentAddress).Any()).ToList();

                cache.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);

                return sent;
            });

            return result ?? new();
        }

        public async Task<int> GetTotalPayoutAsync()
        {
            var currentAddress = await GetAddressAsync();
            var result = await memoryCache.GetOrCreateAsync(nameof(GetTotalPayoutAsync), async (cache) =>
            {
                var infoResult = await client.GetAddressInfo(currentAddress, faucetConfig.Token);

                cache.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);

                return infoResult.TotalAmountReceived - infoResult.TotalAmountSent;
            });

            return result;
        }

        /// <summary>
        /// Get current funds in faucet
        /// </summary>
        /// <returns></returns>
        public async Task<int?> GetCurrentFundsAsync()
        {
            var result = await memoryCache.GetOrCreateAsync<BalanceResponse>(CACHE_KEY_FUNDS, async (cache) =>
            {
                var balanceResult = await client.GetBalance(faucetConfig.Token);

                cache.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);

                return balanceResult;
            });

            return result?.Available;
        }

        /// <summary>
        /// Get amount faucet will pay based on current funds in faucet
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetCurrentPayoutAsync()
        {
            var funds = await GetCurrentFundsAsync();
            if (funds == null)
                return 0;

            int del = 1000;
            if (faucetConfig.Network == Models.HathorNetwork.Testnet)
                del = 20;

            var payout = funds.Value / del;
            if (payout <= 0)
                payout = 1;

            if (funds > 150)
                return Math.Min(payout, faucetConfig.MaxPayoutCents);
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
