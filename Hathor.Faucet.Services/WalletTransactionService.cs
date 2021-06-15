using Hathor.Faucet.Database;
using Hathor.Faucet.Database.Models;
using Microsoft.EntityFrameworkCore;
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

        public WalletTransactionService(FaucetDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public Task<int> GetNumberOfTransactionsAsync()
        {
            return dbContext.WalletTransactions.CountAsync();
        }

        public async Task<WalletTransaction?> GetTransactionAsync(Guid id)
        {
            var result = await dbContext.WalletTransactions.Where(x => x.Id == id).FirstOrDefaultAsync();
            return result;
        }

        public async Task<WalletTransaction> SaveTransactionAsync(string address, int amount, string ip)
        {
            WalletTransaction tx = new WalletTransaction
            {
                Address = address,
                Amount = amount,
                IpAddress = ip
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
    }
}
