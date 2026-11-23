using System.Net;

namespace November.Bff.Tests;

public sealed class PaymentFlowTests
{
    [Fact]
    public async Task Approver_cannot_approve_a_payment_they_captured()
    {
        using var factory = new NovemberFactory();
        var jordan = await factory.SignInAsync("jordan");

        var (created, payment) = await jordan.PostJsonAsync("/api/payments/beneficiary-payments", new
        {
            fromAccountId = "acc_eur_trading",
            beneficiaryId = "ben_de_brandt",
            amount = 100.00m,
            currency = "EUR",
            purposeCode = "GDSP",
            reference = "FOUR-EYES",
            declarationsAccepted = true
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.False(payment["canApprove"]!.GetValue<bool>());

        var (approve, problem) = await jordan.PostJsonAsync($"/api/payments/{payment["id"]!.GetValue<string>()}/approve");

        Assert.Equal(HttpStatusCode.Forbidden, approve.StatusCode);
        Assert.Equal("self_approval_forbidden", problem["code"]!.GetValue<string>());
    }

    [Fact]
    public async Task Expired_fx_quote_is_rejected()
    {
        using var factory = new NovemberFactory();
        var alex = await factory.SignInAsync("alex");

        var (_, quote) = await alex.PostJsonAsync("/api/fx/quotes", new
        {
            sellCurrency = "GBP",
            buyCurrency = "EUR",
            amount = 1000.00m,
            fixedSide = "Sell"
        });
        factory.Clock.Advance(TimeSpan.FromSeconds(61));

        var (response, problem) = await alex.PostJsonAsync("/api/payments/beneficiary-payments", new
        {
            fromAccountId = "acc_gbp_operating",
            beneficiaryId = "ben_de_brandt",
            amount = 1000.00m,
            currency = "GBP",
            purposeCode = "GDSP",
            reference = "EXPIRED-QUOTE",
            fxQuoteId = quote["quoteId"]!.GetValue<string>(),
            declarationsAccepted = true
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("fx_quote_expired", problem["code"]!.GetValue<string>());
    }

    [Fact]
    public async Task Fx_payment_captured_by_alex_is_approved_by_sam_and_starts_processing()
    {
        using var factory = new NovemberFactory();
        var alex = await factory.SignInAsync("alex");
        var sam = await factory.SignInAsync("sam");
        var request = new { sellCurrency = "GBP", buyCurrency = "EUR", amount = 1000.00m, fixedSide = "Sell" };

        var (_, captureQuote) = await alex.PostJsonAsync("/api/fx/quotes", request);
        var (created, payment) = await alex.PostJsonAsync("/api/payments/beneficiary-payments", new
        {
            fromAccountId = "acc_gbp_operating",
            beneficiaryId = "ben_de_brandt",
            amount = 1000.00m,
            currency = "GBP",
            purposeCode = "GDSP",
            reference = "INV-1001",
            fxQuoteId = captureQuote["quoteId"]!.GetValue<string>(),
            declarationsAccepted = true
        });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("PendingApproval", payment["status"]!.GetValue<string>());
        Assert.True(payment["requiresFx"]!.GetValue<bool>());

        var id = payment["id"]!.GetValue<string>();
        var (_, approveQuote) = await sam.PostJsonAsync("/api/fx/quotes", request);
        var (approved, detail) = await sam.PostJsonAsync($"/api/payments/{id}/approve", new
        {
            fxQuoteId = approveQuote["quoteId"]!.GetValue<string>()
        });

        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        Assert.Equal("Processing", detail["status"]!.GetValue<string>());
        Assert.Equal("Sam Okafor", detail["fx"]!["acceptedBy"]!.GetValue<string>());

        var reloaded = await sam.GetJsonAsync($"/api/payments/{id}");
        Assert.Equal("Processing", reloaded["status"]!.GetValue<string>());
    }
}
