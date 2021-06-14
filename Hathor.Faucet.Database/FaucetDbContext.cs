using Hathor.Faucet.Database.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace Hathor.Faucet.Database
{
    public class FaucetDbContext : DbContext
    {
        public FaucetDbContext(DbContextOptions<FaucetDbContext> options) : base(options)
        {
        }

        public DbSet<WalletTransaction> WalletTransactions { get; set; } = default!;
    }
}
