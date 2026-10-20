using NexusFinance.Domain.Enums;

namespace NexusFinance.Application.Abstractions;

public interface IClock
{
    DateOnly Today { get; }
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

public interface ITokenService
{
    string Create(int userId, string name, string email);
}

public interface IUserRepository
{
    Task<int> CountAsync(CancellationToken cancellationToken);
    Task<UserRecord?> FindByEmailAsync(string email, CancellationToken cancellationToken);
    Task AddAsync(string name, string email, string passwordHash, CancellationToken cancellationToken);
}

public sealed record UserRecord(int Id, string Name, string Email, string PasswordHash, bool Active);

public interface IFinanceSettingsRepository
{
    Task<FinanceSettings> GetAsync(CancellationToken cancellationToken);
}

public sealed record FinanceSettings(decimal FinePercent, decimal MonthlyInterestPercent);

public interface IClientRepository
{
    Task<PagedClients> SearchAsync(int page, int pageSize, string? search, ClientStatus? status, CancellationToken cancellationToken);
    Task<ClientRecord?> GetAsync(int id, CancellationToken cancellationToken);
    Task<ClientRecord?> FindByDocumentAsync(string document, CancellationToken cancellationToken);
    Task<IReadOnlyList<ClientRecord>> ListOptionsAsync(CancellationToken cancellationToken);
    Task<int> AddAsync(ClientRecord client, CancellationToken cancellationToken);
    Task UpdateAsync(ClientRecord client, CancellationToken cancellationToken);
}

public sealed record ClientRecord(
    int Id,
    string Name,
    string Document,
    string Email,
    string? Phone,
    ClientStatus Status,
    DateTime CreatedAt);

public sealed record PagedClients(IReadOnlyList<ClientRecord> Items, int Total);

public interface IContractRepository
{
    Task<PagedContracts> SearchAsync(int page, int pageSize, int? clientId, ContractStatus? status, CancellationToken cancellationToken);
    Task<ContractRecord?> GetAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ContractRecord>> ListByClientAsync(int clientId, CancellationToken cancellationToken);
    Task<int> AddAsync(ContractRecord contract, CancellationToken cancellationToken);
    Task UpdateAsync(ContractRecord contract, CancellationToken cancellationToken);
}

public sealed record ContractRecord(
    int Id,
    int ClientId,
    string ClientName,
    string Description,
    decimal Amount,
    BillingFrequency Frequency,
    DateOnly StartDate,
    DateOnly? EndDate,
    ContractStatus Status);

public sealed record PagedContracts(IReadOnlyList<ContractRecord> Items, int Total);

public interface IChargeRepository
{
    Task MarkOverdueAsync(DateOnly today, CancellationToken cancellationToken);
    Task<PagedCharges> SearchAsync(ChargeFilter filter, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChargeRecord>> ListAllAsync(ChargeFilter filter, CancellationToken cancellationToken);
    Task<ChargeRecord?> GetAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<DateOnly>> ListDueDatesAsync(int contractId, CancellationToken cancellationToken);
    Task AddAsync(int contractId, int clientId, decimal amount, DateOnly dueDate, ChargeStatus status, CancellationToken cancellationToken);
    Task UpdateStatusAsync(int id, ChargeStatus status, CancellationToken cancellationToken);
    Task CancelOpenWithoutPaymentsAsync(int contractId, CancellationToken cancellationToken);
}

public sealed record ChargeFilter(int Page, int PageSize, int? ClientId, ChargeStatus? Status, DateOnly? From, DateOnly? To);

public sealed record ChargeRecord(
    int Id,
    int ContractId,
    int ClientId,
    string ClientName,
    string ContractDescription,
    decimal OriginalAmount,
    DateOnly DueDate,
    ChargeStatus Status,
    string? Notes,
    decimal AmountPaid);

public sealed record PagedCharges(IReadOnlyList<ChargeRecord> Items, int Total);

public interface IPaymentRepository
{
    Task<int> AddAsync(int chargeId, decimal amount, DateOnly paidOn, PaymentMethod method, CancellationToken cancellationToken);
    Task<IReadOnlyList<PaymentRecord>> ListByChargeAsync(int chargeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<MonthlyTotal>> SumByMonthAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken);
}

public sealed record PaymentRecord(int Id, int ChargeId, decimal Amount, DateOnly PaidOn, PaymentMethod Method);

public sealed record MonthlyTotal(int Year, int Month, decimal Amount);
