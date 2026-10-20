using System.Data;

namespace NexusFinance.Application.Abstractions;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
