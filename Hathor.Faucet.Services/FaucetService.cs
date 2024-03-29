﻿using Hathor.Faucet.Services.Exceptions;
using Hathor.Faucet.Services.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Whois;
using Whois.NET;

namespace Hathor.Faucet.Services
{
    /// <summary>
    /// Faucet logic
    /// </summary>
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

        public async Task<Guid?> SendHathorAsync(string address, string ip)
        {
            address = address.Trim();
            if (string.IsNullOrEmpty(address))
                throw new FaucetException("Empty address. Please provide a valid Hathor address.");

            //Check if address is valid
            bool isAddressValid = await hathorService.IsAddressValidAsync(address);
            if (!isAddressValid)
            {
                if (faucetConfig.Network == HathorNetwork.Testnet)
                    throw new FaucetException("Please provide a valid Hathor TESTNET address.");

                throw new FaucetException("Please provide a valid Hathor address.");
            }

            //Check if IP is already in database
            if (faucetConfig.Network == HathorNetwork.Mainnet)
            {
                bool existingIp = await walletTransactionService.IpHasTransactionsAsync(ip);
                if (existingIp)
                    throw new FaucetException("This IP has already used the faucet.");
            }
            else
            {
                //For Testnet, once every hour per IP is allowed
                bool existingIp = await walletTransactionService.IpHasTransactionsAsync(ip, DateTimeOffset.UtcNow.AddHours(-1));
                if (existingIp)
                    throw new FaucetException("This IP has already used the faucet in the last hour. Please wait and try again later.");
            }

            //Check if IP is on blocklist (Azure / Amazon / TOR etc)
            string? whoisOrganization = await GetWhoisInfoAsync(ip);
            string reverseDns = await ReverseLookup(ip);

            //if((reverseDns.Equals(ip, StringComparison.InvariantCultureIgnoreCase) || string.IsNullOrWhiteSpace(whoisOrganization)) && faucetConfig.Network == HathorNetwork.Mainnet)
            //    throw new FaucetException("This IP is blocked from using the faucet.");

            bool blocked = IsOrganizationBlocked(whoisOrganization ?? string.Empty);
            if (blocked)
                throw new FaucetException("This IP is blocked from using the faucet.");

            blocked = IsOrganizationBlocked(reverseDns);
            if (blocked)
                throw new FaucetException("This IP is blocked from using the faucet.");

            if (faucetConfig.Network == HathorNetwork.Mainnet && !string.IsNullOrEmpty(whoisOrganization))
            {
                //Get organization usage from past 30 days
                var orgUsage = await walletTransactionService.GetOrganizationTransactions(whoisOrganization);

                var pastDays = orgUsage.Where(x => x.CreatedDateTime > DateTimeOffset.UtcNow.AddDays(-1)).Count();
                var past10Days = orgUsage.Where(x => x.CreatedDateTime > DateTimeOffset.UtcNow.AddDays(-10)).Count();
                var past30Days = orgUsage.Where(x => x.CreatedDateTime > DateTimeOffset.UtcNow.AddDays(-30)).Count();

                if (pastDays >= 2 || past10Days >= 5 || past30Days >= 10)
                    throw new FaucetException("This IP is blocked from using the faucet.");
            }

            int lastHourAmount = await walletTransactionService.GetLastHourAmountAsync();
            if (lastHourAmount > faucetConfig.TresholdAmountCents)
                throw new FaucetException("Maximum payout reached. Please try again later.");

            int amount = await hathorService.GetCurrentPayoutAsync();

            //Check if Hathor address is empty
            if (faucetConfig.Network == HathorNetwork.Mainnet)
            {
                bool isEmpty = await hathorService.CheckIsAddressEmptyAsync(address);
                if (!isEmpty)
                    amount = 0;
            }
            else
            {
                //Check if address is only used once in last 24 hours
                bool existingAddress = await walletTransactionService.AddressHasTransactionsAsync(address, DateTimeOffset.UtcNow.AddHours(-1));
                if (existingAddress)
                    throw new FaucetException("This address has already used the faucet in the last hour. Please wait and try again later.");
            }

            //Save transaction in database
            try
            {
                var dbTx = await walletTransactionService.SaveTransactionAsync(address, amount, ip, reverseDns, whoisOrganization);

                //Send hathor
                if (amount > 0)
                {
                    var result = await hathorService.SendHathorAsync(address, amount);

                    await walletTransactionService.SetHathorTxFinishedAsync(dbTx.Id, result.TxId, result.Success, result.Error);
                }

                return dbTx.Id;
            }
            catch {
                var result = await hathorService.SendHathorAsync(address, amount);
                return null;
            }

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
                "Microsoft",
                "CloudFlare",
                "DigitalOcean",
                "LeaseWeb",
                "M247",
                "Indosat",
                "PT. ",
                "Axiata",
                "Reliance Jio",
                "Hutchison",
                "GGSN",
                "EDIS",
                "Vultr",
                "vps",
                "vpn",
                "24Shells",
                "PSINet",
                "cdn",
                "cloud",
                "tor-exit",
                "torexit",
                "torproject",
                "emerald",
                "exit",
                "ghost",
                "gthost",
                "vds",
                 "server.de",
                 "node.com",
                 "hosting",
                 "webhosting",
                 "hostpro",
                 "packethub",
                 "server",
                 "zare.com"
            };

            return blocked.Select(x => whoisOrganization.Contains(x, StringComparison.InvariantCultureIgnoreCase))
                .Where(x => x)
                .Any();
        }

        private async Task<string?> GetWhoisInfoAsync(string ip)
        {
            try
            {
                var response = await WhoisClient.QueryAsync(ip);

                return response.OrganizationName;
            }
            catch (Exception ex)
            {
                return ex.ToString();
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
