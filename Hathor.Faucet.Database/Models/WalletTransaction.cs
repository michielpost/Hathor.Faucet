using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hathor.Faucet.Database.Models
{
    public record WalletTransaction
    {
        [Key]
        public Guid Id { get; set; }

        public DateTimeOffset SendDateTime { get; set; }

        public int Amount { get; set; }

        [Required]
        public string Address { get; set; } = default!;

        [Required]
        public string HathorTransactionId { get; set; } = default!;

        [Required]
        public string IpAddress { get; set; } = default!;

    }
}
