using NexusFinance.Application.Abstractions;
using NexusFinance.Application.Charges;
using NexusFinance.Application.Clients;
using NexusFinance.Application.Contracts;
using NexusFinance.Domain.Enums;
using NexusFinance.Domain.Rules;

namespace NexusFinance.Infrastructure.Persistence;

public sealed class DemoSeeder
{
    public const string DemoEmail = "demo@nexusfinance.dev";
    public const string DemoPassword = "Demo@123";

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwords;
    private readonly ClientService _clients;
    private readonly ContractService _contracts;
    private readonly ChargeService _charges;
    private readonly IClock _clock;

    public DemoSeeder(
        IUserRepository users,
        IPasswordHasher passwords,
        ClientService clients,
        ContractService contracts,
        ChargeService charges,
        IClock clock)
    {
        _users = users;
        _passwords = passwords;
        _clients = clients;
        _contracts = contracts;
        _charges = charges;
        _clock = clock;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await _users.FindByEmailAsync(DemoEmail, cancellationToken) is null)
        {
            await _users.AddAsync("Helena Prado", DemoEmail, _passwords.Hash(DemoPassword), cancellationToken);
        }

        if ((await _clients.SearchAsync(1, 1, null, null, cancellationToken)).Total > 0)
        {
            return;
        }

        var names = new[]
        {
            "Ateliê Lume", "Padaria Serra Azul", "Oficina Norte", "Estúdio Vértice", "Clínica Bem Estar",
            "Mercado da Praça", "Livraria Página", "Pet Casa Amarela", "Studio Horizonte", "Café do Cais",
            "Tecidos Aurora", "Auto Elétrica Sul", "Escola Pequeno Mundo", "Farmácia Centralina", "Buffet Alegria",
            "Gráfica Tipo", "Floricultura Jardim", "Consultório Vila Nova", "Bike Oficina", "Doceria Nuvem",
            "Lavanderia Brisa", "Ótica Clara", "Madeireira Pinheiro", "Agência Roteiro", "Hotel Serra Fina",
            "Academia Pulso", "Papelaria Traço", "Vidraçaria Luz", "Contabilidade Ponto", "Restaurante Maré"
        };

        for (var index = 0; index < names.Length; index++)
        {
            var document = index % 4 == 0 ? CreateCnpj(index + 1) : CreateCpf(index + 1);
            var client = await _clients.CreateAsync(
                new SaveClientRequest(names[index], document, $"contato{index + 1}@exemplo.test", $"(11) 9{index + 1000:0000}-0000"),
                cancellationToken);

            var frequency = (BillingFrequency)(index % 3);
            var start = _clock.Today.AddMonths(-6).AddDays(index % 12);
            var contract = await _contracts.CreateAsync(
                new SaveContractRequest(client.Id, $"Plano de serviço {index + 1}", 650 + (index * 85), frequency, start, null),
                cancellationToken);

            if (index % 10 == 9)
            {
                await _contracts.ChangeStatusAsync(contract.Id, ContractStatus.Cancelled, cancellationToken);
                await _clients.InactivateAsync(client.Id, cancellationToken);
                continue;
            }

            if (index % 8 == 7)
            {
                continue;
            }

            await _contracts.ChangeStatusAsync(contract.Id, ContractStatus.Active, cancellationToken);
            var charges = await _charges.SearchAsync(1, 50, client.Id, null, null, null, cancellationToken);
            foreach (var charge in charges.Items.Where(item => item.DueDate < _clock.Today.AddDays(-25)))
            {
                await _charges.RegisterPaymentAsync(
                    charge.Id,
                    new RegisterPaymentRequest(charge.Balance, charge.DueDate.AddDays(3), PaymentMethod.Pix),
                    cancellationToken);
            }

            foreach (var charge in (await _charges.SearchAsync(1, 50, client.Id, null, null, null, cancellationToken)).Items
                .Where(item => item.Status != ChargeStatus.Paid && item.DueDate < _clock.Today.AddDays(-5) && item.DueDate >= _clock.Today.AddDays(-25)))
            {
                var partial = MoneyRules.Round(charge.Balance * 0.45m);
                if (partial > 0 && partial < charge.Balance)
                {
                    await _charges.RegisterPaymentAsync(
                        charge.Id,
                        new RegisterPaymentRequest(partial, charge.DueDate.AddDays(2), PaymentMethod.Boleto),
                        cancellationToken);
                }
            }
        }
    }

    private static string CreateCpf(int seed)
    {
        var numbers = new int[11];
        var value = 120000000 + (seed * 137);
        for (var index = 8; index >= 0; index--)
        {
            numbers[index] = value % 10;
            value /= 10;
        }

        if (numbers.Take(9).Distinct().Count() == 1)
        {
            numbers[0] = (numbers[0] + 2) % 10;
        }

        numbers[9] = CheckDigit(numbers, 9);
        numbers[10] = CheckDigit(numbers, 10);
        var document = string.Concat(numbers);
        return DocumentoRules.IsValid(document) ? document : CreateCpf(seed + 100);
    }

    private static string CreateCnpj(int seed)
    {
        var numbers = new int[14];
        var value = 20000000 + (seed * 91);
        for (var index = 7; index >= 0; index--)
        {
            numbers[index] = value % 10;
            value /= 10;
        }

        numbers[11] = 1;
        int[] firstWeights = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] secondWeights = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        numbers[12] = Weighted(numbers, firstWeights);
        numbers[13] = Weighted(numbers, secondWeights);
        var document = string.Concat(numbers);
        return DocumentoRules.IsValid(document) ? document : CreateCnpj(seed + 100);
    }

    private static int CheckDigit(int[] numbers, int length)
    {
        var sum = 0;
        for (var index = 0; index < length; index++)
        {
            sum += numbers[index] * (length + 1 - index);
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    private static int Weighted(int[] numbers, int[] weights)
    {
        var sum = 0;
        for (var index = 0; index < weights.Length; index++)
        {
            sum += numbers[index] * weights[index];
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
