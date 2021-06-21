using Hathor.Faucet.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hathor.Faucet.Web.Models
{
    public class ThankYouViewModel
    {
        public string? Address { get; set; }

        public WalletTransaction? WalletTransaction { get; set; }

        public string ExplorerUrl { get; set; } = default!;
    }
}
