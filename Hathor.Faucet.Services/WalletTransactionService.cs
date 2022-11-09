using Hathor.Faucet.Database;
using Hathor.Faucet.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hathor.Faucet.Services
{
    /// <summary>
    /// Storage of WalletTransactions in database
    /// </summary>
    public class WalletTransactionService
    {
        private readonly FaucetDbContext dbContext;
        private readonly IMemoryCache memoryCache;

        public static readonly string CACHE_KEY_HISTORY = "historyinfo";
        public static readonly string CACHE_KEY_TX_HISTORY = "txhistoryinfo";

        public WalletTransactionService(FaucetDbContext dbContext, IMemoryCache memoryCache)
        {
            this.dbContext = dbContext;
            this.memoryCache = memoryCache;
        }

        /// <summary>
        /// Get faucet stats
        /// </summary>
        /// <returns></returns>
        public async Task<(int count, int payoutAmount)> GetStats()
        {
            var result = await memoryCache.GetOrCreateAsync(CACHE_KEY_HISTORY, async (cache) =>
            {
                cache.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);

                var transactionQuery = dbContext.WalletTransactions.Where(x => x.IsSuccess && x.Amount > 0);
                var count = await transactionQuery.CountAsync();
                var payoutAmount = await transactionQuery.SumAsync(x => x.Amount);

                return (count, payoutAmount);
            });

            return result;
          
        }

        /// <summary>
        /// Get transaction based on id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<WalletTransaction?> GetTransactionAsync(Guid id)
        {
            var result = await dbContext.WalletTransactions.Where(x => x.Id == id).FirstOrDefaultAsync();
            return result;
        }

        public async Task<List<WalletTransaction>> GetLastTransactions(int count = 10)
        {
            var result = await memoryCache.GetOrCreateAsync(CACHE_KEY_TX_HISTORY, async (cache) =>
            {
                cache.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);

                var result = await dbContext.WalletTransactions.Where(x => x.IsSuccess && x.Amount > 0)
                .OrderByDescending(x => x.TransactionDateTime).Take(count).ToListAsync();

                return result;

            });

            return result ?? new();
        }

        /// <summary>
        /// Save transaction in DB
        /// </summary>
        /// <param name="address"></param>
        /// <param name="amount"></param>
        /// <param name="ip"></param>
        /// <param name="reverseDns"></param>
        /// <param name="whoisOrganization"></param>
        /// <returns></returns>
        public async Task<WalletTransaction> SaveTransactionAsync(string address, int amount, string ip, string? reverseDns, string? whoisOrganization)
        {
            WalletTransaction tx = new WalletTransaction
            {
                Address = address,
                Amount = amount,
                IpAddress = ip,
                ReverseDns = reverseDns,
                WhoisOrganization = whoisOrganization
            };

            dbContext.WalletTransactions.Add(tx);
            await dbContext.SaveChangesAsync();

            return tx;
        }

        public async Task<List<WalletTransaction>> GetOrganizationTransactions(string whoisOrganization)
        {
            var past30 = DateTimeOffset.UtcNow.AddDays(-30);
            var all = await dbContext.WalletTransactions.Where(x => x.CreatedDateTime > past30 && x.WhoisOrganization == whoisOrganization).ToListAsync();

            if (all.Any())
                return all;

            if(whoisOrganization.Length > 8)
            {
                string searchText = whoisOrganization.Substring(0, 8);
                all = await dbContext.WalletTransactions.Where(x => x.CreatedDateTime > past30 && x.WhoisOrganization != null &&  x.WhoisOrganization.StartsWith(searchText)).ToListAsync();

            }

            return all;
        }

        /// <summary>
        /// Save Hathor TX ID in DB
        /// </summary>
        /// <param name="id"></param>
        /// <param name="txId"></param>
        /// <param name="isSuccess"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public async Task SetHathorTxFinishedAsync(Guid id, string? txId, bool isSuccess, string? error)
        {
            WalletTransaction? tx = await GetTransactionAsync(id);

            if (tx == null)
            {
                throw new Exception($"DB Transaction with id: {id} not found.");
            }

            tx.HathorTransactionId = txId;
            tx.TransactionDateTime = DateTimeOffset.UtcNow;
            tx.IsSuccess = isSuccess;
            tx.Error = error;

            await dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Check if IP already has transactions
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public Task<bool> IpHasTransactionsAsync(string ipAddress)
        {
            return dbContext.WalletTransactions.Where(x => x.IpAddress == ipAddress).AnyAsync();
        }

        public Task<bool> IpHasTransactionsAsync(string ipAddress, DateTimeOffset since)
        {
            return dbContext.WalletTransactions.Where(x => x.IpAddress == ipAddress && x.CreatedDateTime > since).AnyAsync();
        }

        public Task<bool> AddressHasTransactionsAsync(string address, DateTimeOffset since)
        {
            return dbContext.WalletTransactions.Where(x => x.Address == address && x.CreatedDateTime > since).AnyAsync();
        }

        /// <summary>
        /// Get amount of HTR payed out in the last hour
        /// </summary>
        /// <returns></returns>
        public Task<int> GetLastHourAmountAsync()
        {
            var time = DateTimeOffset.UtcNow.AddHours(-1);
            return dbContext.WalletTransactions.Where(x => x.CreatedDateTime > time).SumAsync(x => x.Amount);
        }
    }
}
