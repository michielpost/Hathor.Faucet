using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hathor.Faucet.Database.Models
{
    [Index(nameof(Address))]
    [Index(nameof(IpAddress))]
    public record WalletTransaction
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTimeOffset CreatedDateTime { get; set; } = DateTimeOffset.UtcNow;

        public int Amount { get; set; }

        [Required]
        public string Address { get; set; } = default!;

        [Required]
        public string IpAddress { get; set; } = default!;

        public string? ReverseDns { get; set; }
        public string? WhoisOrganization { get; set; }

        public string? HathorTransactionId { get; set; } = default!;

        public DateTimeOffset? TransactionDateTime { get; set; }
        public bool IsSuccess { get; set; }
        public string? Error { get; set; }

    }
}
