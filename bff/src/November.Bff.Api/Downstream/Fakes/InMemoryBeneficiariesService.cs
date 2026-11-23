using November.Bff.Api.Downstream.Models;
using November.Bff.Api.Infrastructure.ErrorHandling;
using November.Bff.Api.Infrastructure.Validation;

namespace November.Bff.Api.Downstream.Fakes;

internal sealed class InMemoryBeneficiariesService : IBeneficiariesService
{
    private readonly Lock gate = new();
    private readonly Dictionary<string, Beneficiary> beneficiaries;
    private readonly HashSet<string> saved;

    public InMemoryBeneficiariesService(TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();
        var seeded = new[]
        {
            new Beneficiary("ben_de_brandt", "Brandt Maschinenbau GmbH", "DE", "EUR", "Rheinische Handelsbank AG",
                BankFormats.CreateIban("DE", "10077700" + "4410265839"), null, null, "RHHBDEFF", now.AddDays(-9)),
            new Beneficiary("ben_us_lakeshore", "Lakeshore Components Inc.", "US", "USD", "Prairie State Bank",
                null, "4829105736", "123456780", "PSTBUS33", now.AddDays(-21)),
            new Beneficiary("ben_za_karoo", "Karoo Fresh Produce (Pty) Ltd", "ZA", "ZAR", "Boland Commercial Bank",
                null, "1023948576", null, "BCBKZAJJ", now.AddDays(-3)),
            new Beneficiary("ben_ch_alpenblick", "Alpenblick Precision AG", "CH", "CHF", "Zentralmatt Privatbank",
                BankFormats.CreateIban("CH", "08877" + "000412067391"), null, null, "ZMPBCHZZ", null),
            new Beneficiary("ben_jp_tsukimi", "Tsukimi Components KK", "JP", "JPY", "Harumi Trust Bank",
                null, "7745120", null, "HRTBJPJT", now.AddDays(-40))
        };

        beneficiaries = seeded.ToDictionary(b => b.Id);
        saved = seeded.Select(b => b.Id).ToHashSet();
    }

    public ValueTask<IReadOnlyList<Beneficiary>> ListAsync(CancellationToken cancellationToken)
    {
        lock (gate)
        {
            return ValueTask.FromResult<IReadOnlyList<Beneficiary>>(
                beneficiaries.Values.Where(b => saved.Contains(b.Id)).OrderBy(b => b.Name).ToList());
        }
    }

    public ValueTask<Beneficiary> GetAsync(string beneficiaryId, CancellationToken cancellationToken)
    {
        lock (gate)
        {
            return ValueTask.FromResult(
                beneficiaries.TryGetValue(beneficiaryId, out var beneficiary)
                    ? beneficiary
                    : throw Errors.NotFound("Beneficiary", beneficiaryId));
        }
    }

    public ValueTask<Beneficiary> AddAsync(NewBeneficiaryDetails details, CancellationToken cancellationToken)
    {
        var beneficiary = new Beneficiary(
            "ben_" + Guid.NewGuid().ToString("N")[..12],
            details.Name,
            details.Country,
            details.Currency,
            details.BankName,
            details.Iban is null ? null : BankFormats.NormaliseIban(details.Iban),
            details.AccountNumber,
            details.RoutingNumber,
            details.SwiftBic,
            null);

        lock (gate)
        {
            beneficiaries[beneficiary.Id] = beneficiary;
        }

        return ValueTask.FromResult(beneficiary);
    }

    public ValueTask SaveForFutureAsync(string beneficiaryId, CancellationToken cancellationToken)
    {
        lock (gate)
        {
            if (!beneficiaries.ContainsKey(beneficiaryId))
            {
                throw Errors.NotFound("Beneficiary", beneficiaryId);
            }

            saved.Add(beneficiaryId);
        }

        return ValueTask.CompletedTask;
    }
}
