using Hathor.Faucet.Services.Exceptions;
using Hathor.Faucet.Services.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Whois;

namespace Hathor.Faucet.Services
{
    public class FaucetService
    {
        private readonly HathorService hathorService;
        private readonly WalletTransactionService walletTransactionService;
        private readonly FaucetConfig faucetConfig;

        public FaucetService(HathorService hathorService, WalletTransactionService walletTransactionService, IOptions<FaucetConfig> faucetConfigOptions)
        {
            this.hathorService = hathorService;
            this.walletTransactionService = walletTransactionService;
            this.faucetConfig = faucetConfigOptions.Value;
        }

        public async Task<Guid> SendHathorAsync(string address, string ip)
        {
            address = address.Trim();
            if (string.IsNullOrEmpty(address))
                throw new FaucetException("Empty address. Please provide a valid Hathor address.");

            //Check if address is valid
            bool isAddressValid = await hathorService.IsAddressValidAsync(address);
            if (!isAddressValid)
            {
                if(faucetConfig.Network == HathorNetwork.Testnet)
                    throw new FaucetException("Please provide a valid Hathor TESTNET address.");
             
                throw new FaucetException("Please provide a valid Hathor address.");
            }

            //Check if IP is already in database
            bool existingIp = await walletTransactionService.IpHasTransactionsAsync(ip);
            if (existingIp && faucetConfig.Network != HathorNetwork.Testnet)
                throw new FaucetException("This IP has already uses the faucet.");

            //Check if IP is on blocklist (Azure / Amazon / TOR etc)
            string whoisOrganization = await GetWhoisInfoAsync(ip);
            string reverseDns = await ReverseLookup(ip);

            bool blocked = IsOrganizationBlocked(whoisOrganization);
            if (blocked && faucetConfig.Network != HathorNetwork.Testnet)
                throw new FaucetException("This IP is blocked from using faucet.");

            int lastHourAmount = await walletTransactionService.GetLastHourAmountAsync();
            if (lastHourAmount > faucetConfig.TresholdAmountCents)
                throw new FaucetException("Maximum payout reached. Please try again later.");

            int amount = await hathorService.GetCurrentPayoutAsync();

            //Check if Hathor address is empty
            if (faucetConfig.Network != HathorNetwork.Testnet)
            {
                bool isEmpty = await hathorService.CheckIsAddressEmptyAsync(address);
                if (!isEmpty)
                    amount = 0;
            }

            //Save transaction in database
            var dbTx = await walletTransactionService.SaveTransactionAsync(address, amount, ip, reverseDns, whoisOrganization);

            //Send hathor
            if (amount > 0)
            {
                var result = await hathorService.SendHathorAsync(address, amount);

                await walletTransactionService.SetHathorTxFinishedAsync(dbTx.Id, result.TxId, result.Success, result.Error);
            }

            return dbTx.Id;

        }

        private bool IsOrganizationBlocked(string whoisOrganization)
        {
            string org = whoisOrganization.ToLower();

            List<string> blocked = new List<string>()
            {
                "CloudFlare",
                "Fastly",
                "AWS",
                "Amazon",
                "Google",
                "Azure",
                "CloudFlare",
                "DigitalOcean",
                "LeaseWeb"
            };

            return blocked.Select(x => whoisOrganization.Contains(x, StringComparison.InvariantCultureIgnoreCase))
                .Where(x => x)
                .Any();
        }

        private async Task<string> GetWhoisInfoAsync(string ip)
        {
            try
            {
                var whois = new WhoisLookup();

                var response = await whois.LookupAsync(ip);

                return response.Registrant.Organization;
            }
            catch(Exception)
            {
                return ip;
            }
        }

        public async Task<string> ReverseLookup(string ip)
        {
            try
            {
                var dnsInfo = await Dns.GetHostEntryAsync(ip);
                return dnsInfo.HostName;
            }
            catch (Exception) 
            { 
                return ip; 
            }
        }
    }
}
