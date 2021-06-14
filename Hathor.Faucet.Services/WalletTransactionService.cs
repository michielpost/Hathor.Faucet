using Hathor.Faucet.Database;
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
    }
}
