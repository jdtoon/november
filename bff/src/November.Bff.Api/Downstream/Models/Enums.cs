namespace November.Bff.Api.Downstream.Models;

public enum PaymentType
{
    BeneficiaryPayment,
    InternationalTransfer
}

public enum PaymentStatus
{
    PendingApproval,
    Processing,
    Completed,
    Rejected,
    Cancelled
}

public enum IncomingStatus
{
    ActionRequired,
    Processing,
    Credited,
    Cancelled
}

public enum IncomingAction
{
    PurposeCode,
    FxAcceptance
}

public enum FixedSide
{
    Sell,
    Buy
}

public enum BankDetailsFormat
{
    Iban,
    AccountNumberSwift,
    AccountNumberRoutingSwift
}

public enum PurposeDirection
{
    Outgoing,
    Incoming,
    Both
}
