namespace NexusFinance.Domain.Enums;

public enum ClientStatus
{
    Active,
    Inactive
}

public enum ContractStatus
{
    Draft,
    Active,
    Closed,
    Cancelled
}

public enum BillingFrequency
{
    Monthly,
    Quarterly,
    Yearly
}

public enum ChargeStatus
{
    Pending,
    Paid,
    Overdue,
    Cancelled
}

public enum PaymentMethod
{
    Pix,
    Boleto,
    Transfer,
    Cash,
    Card
}
