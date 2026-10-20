using NexusFinance.Domain.Enums;

namespace NexusFinance.Infrastructure.Persistence;

internal static class DbMaps
{
    public static string ToDb(ClientStatus status) => status == ClientStatus.Active ? "Ativo" : "Inativo";

    public static ClientStatus ToClientStatus(string status) =>
        status == "Ativo" ? ClientStatus.Active : ClientStatus.Inactive;

    public static string ToDb(ContractStatus status) => status switch
    {
        ContractStatus.Draft => "Rascunho",
        ContractStatus.Active => "Ativo",
        ContractStatus.Closed => "Encerrado",
        _ => "Cancelado"
    };

    public static ContractStatus ToContractStatus(string status) => status switch
    {
        "Rascunho" => ContractStatus.Draft,
        "Ativo" => ContractStatus.Active,
        "Encerrado" => ContractStatus.Closed,
        _ => ContractStatus.Cancelled
    };

    public static string ToDb(BillingFrequency frequency) => frequency switch
    {
        BillingFrequency.Quarterly => "Trimestral",
        BillingFrequency.Yearly => "Anual",
        _ => "Mensal"
    };

    public static BillingFrequency ToFrequency(string frequency) => frequency switch
    {
        "Trimestral" => BillingFrequency.Quarterly,
        "Anual" => BillingFrequency.Yearly,
        _ => BillingFrequency.Monthly
    };

    public static string ToDb(ChargeStatus status) => status switch
    {
        ChargeStatus.Paid => "Paga",
        ChargeStatus.Overdue => "Vencida",
        ChargeStatus.Cancelled => "Cancelada",
        _ => "Pendente"
    };

    public static ChargeStatus ToChargeStatus(string status) => status switch
    {
        "Paga" => ChargeStatus.Paid,
        "Vencida" => ChargeStatus.Overdue,
        "Cancelada" => ChargeStatus.Cancelled,
        _ => ChargeStatus.Pending
    };

    public static string ToDb(PaymentMethod method) => method switch
    {
        PaymentMethod.Boleto => "Boleto",
        PaymentMethod.Transfer => "Transferencia",
        PaymentMethod.Cash => "Dinheiro",
        PaymentMethod.Card => "Cartao",
        _ => "Pix"
    };

    public static PaymentMethod ToPaymentMethod(string method) => method switch
    {
        "Boleto" => PaymentMethod.Boleto,
        "Transferencia" => PaymentMethod.Transfer,
        "Dinheiro" => PaymentMethod.Cash,
        "Cartao" => PaymentMethod.Card,
        _ => PaymentMethod.Pix
    };
}
