using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models.Users.Transactions;
public class Transaction
{
    public Guid Id { get; set; }
    public string ItemPurchased { get; set; } = string.Empty;
    public float Cost { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTimeOffset TimeOfPurchase { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public Status TransactionStatus { get; set; }
}