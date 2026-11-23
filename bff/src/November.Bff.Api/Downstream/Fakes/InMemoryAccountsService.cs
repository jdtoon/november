using November.Bff.Api.Downstream.Models;
using November.Bff.Api.Infrastructure.ErrorHandling;
using November.Bff.Api.Infrastructure.Validation;

namespace November.Bff.Api.Downstream.Fakes;

internal sealed class InMemoryAccountsService : IAccountsService, IAccountLedger
{
    private readonly Lock gate = new();
    private readonly Dictionary<string, Entry> accounts;

    public InMemoryAccountsService()
    {
        accounts = new[]
        {
            Seed("acc_gbp_operating", "GBP Operating", "40291837", "GBP", 252_418.60m, 9_200.00m),
            Seed("acc_gbp_payroll", "GBP Payroll", "40291845", "GBP", 84_530.25m, 0m),
            Seed("acc_usd_trading", "USD Trading", "40291853", "USD", 61_200.00m, 0m),
            Seed("acc_eur_trading", "EUR Trading", "40291861", "EUR", 47_850.40m, 8_750.00m)
        }.ToDictionary(e => e.Id);
    }

    public ValueTask<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken)
    {
        lock (gate)
        {
            return ValueTask.FromResult<IReadOnlyList<Account>>(accounts.Values.Select(e => e.ToAccount()).ToList());
        }
    }

    public ValueTask<Account> GetAsync(string accountId, CancellationToken cancellationToken)
    {
        lock (gate)
        {
            return ValueTask.FromResult(Find(accountId).ToAccount());
        }
    }

    Account IAccountLedger.Get(string accountId)
    {
        lock (gate)
        {
            return Find(accountId).ToAccount();
        }
    }

    void IAccountLedger.Reserve(string accountId, decimal amount)
    {
        lock (gate)
        {
            var entry = Find(accountId);
            var available = entry.Balance - entry.Reserved;
            if (available < amount)
            {
                throw Errors.InsufficientFunds(
                    $"Available balance {Currencies.Format(available, entry.Currency)} on {entry.Name} is lower than the required {Currencies.Format(amount, entry.Currency)}.");
            }

            entry.Reserved += amount;
        }
    }

    void IAccountLedger.AdjustReservation(string accountId, decimal delta)
    {
        if (delta > 0)
        {
            ((IAccountLedger)this).Reserve(accountId, delta);
        }
        else if (delta < 0)
        {
            ((IAccountLedger)this).Release(accountId, -delta);
        }
    }

    void IAccountLedger.Release(string accountId, decimal amount)
    {
        lock (gate)
        {
            var entry = Find(accountId);
            entry.Reserved = Math.Max(0m, entry.Reserved - amount);
        }
    }

    void IAccountLedger.Settle(string accountId, decimal amount)
    {
        lock (gate)
        {
            var entry = Find(accountId);
            entry.Balance -= amount;
            entry.Reserved = Math.Max(0m, entry.Reserved - amount);
        }
    }

    void IAccountLedger.Credit(string accountId, decimal amount)
    {
        lock (gate)
        {
            Find(accountId).Balance += amount;
        }
    }

    private Entry Find(string accountId) =>
        accounts.TryGetValue(accountId, out var entry) ? entry : throw Errors.NotFound("Account", accountId);

    private static Entry Seed(string id, string name, string accountNumber, string currency, decimal balance, decimal reserved)
    {
        const string sortCode = "20-45-67";
        var iban = BankFormats.CreateIban("GB", "NOVB" + sortCode.Replace("-", string.Empty, StringComparison.Ordinal) + accountNumber);
        return new Entry(id, name, accountNumber, sortCode, iban, currency) { Balance = balance, Reserved = reserved };
    }

    private sealed class Entry(string id, string name, string accountNumber, string sortCode, string iban, string currency)
    {
        public string Id { get; } = id;

        public string Name { get; } = name;

        public string Currency { get; } = currency;

        public decimal Balance { get; set; }

        public decimal Reserved { get; set; }

        public Account ToAccount() => new(Id, Name, accountNumber, sortCode, iban, Currency, Balance, Balance - Reserved);
    }
}
