using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NexusFinance.Application.Abstractions;

namespace NexusFinance.Api.Tests.Support;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public bool DatabaseReachable { get; set; } = true;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHealthProbe>();
            services.AddSingleton<IHealthProbe>(new FakeHealthProbe(() => DatabaseReachable));
        });
    }

    private sealed class FakeHealthProbe : IHealthProbe
    {
        private readonly Func<bool> _databaseReachable;

        public FakeHealthProbe(Func<bool> databaseReachable)
        {
            _databaseReachable = databaseReachable;
        }

        public Task<bool> DatabaseIsReachableAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_databaseReachable());
        }
    }
}
