using Hathor.Faucet.Database.Models;
using Hathor.Models.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hathor.Faucet.Web.Models
{
    public class HomepageViewModel
    {
        public string? Address { get; set; }
        public int? Amount { get; set; }
        public int NumberOfTransactions { get; set; }
        public int CurrentPayout { get; set; }
        public int HistoricPayoutAmount { get; set; }
        public List<WalletTransaction> LastTransactions { get; set; } = new List<WalletTransaction>();
        public string ExplorerUrl { get; set; } = default!;
    }
}
