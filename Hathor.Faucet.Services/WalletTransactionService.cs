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
    public class WalletTransactionService
    {
        private readonly FaucetDbContext dbContext;
        private readonly IMemoryCache memoryCache;

        public static readonly string CACHE_KEY_HISTORY = "historyinfo";


        public WalletTransactionService(FaucetDbContext dbContext, IMemoryCache memoryCache)
        {
            this.dbContext = dbContext;
            this.memoryCache = memoryCache;
        }

        public async Task<(int count, int payoutAmount)> GetHistoryInfo()
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

        public async Task<WalletTransaction?> GetTransactionAsync(Guid id)
        {
            var result = await dbContext.WalletTransactions.Where(x => x.Id == id).FirstOrDefaultAsync();
            return result;
        }

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

        public Task<bool> IpHasTransactionsAsync(string ipAddress)
        {
            return dbContext.WalletTransactions.Where(x => x.IpAddress == ipAddress).AnyAsync();
        }

        public Task<int> GetLastHourAmountAsync()
        {
            var time = DateTimeOffset.UtcNow.AddHours(-1);
            return dbContext.WalletTransactions.Where(x => x.CreatedDateTime > time).SumAsync(x => x.Amount);
        }
    }
}
