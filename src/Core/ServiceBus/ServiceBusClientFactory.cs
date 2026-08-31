using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Azure.Core;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using CrossBusExplorer.Management.Contracts;
using AzureTransportType = Azure.Messaging.ServiceBus.ServiceBusTransportType;
using ContractTransportType = CrossBusExplorer.Management.Contracts.ServiceBusTransportType;

namespace CrossBusExplorer.ServiceBus;

public sealed class ServiceBusClientFactory : IServiceBusClientFactory, IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, ServiceBusClient> _clients = new();
    private readonly ConcurrentDictionary<string, ServiceBusAdministrationClient> _administrationClients = new();
    private readonly ConcurrentDictionary<string, TokenCredential> _credentials = new();

    public ServiceBusClient GetClient(ServiceBusConnection connection)
    {
        var key = GetCacheKey(connection);
        return _clients.GetOrAdd(key, _ => CreateClient(connection));
    }

    public ServiceBusAdministrationClient GetAdministrationClient(
        ServiceBusConnection connection)
    {
        var key = GetCacheKey(connection);
        return _administrationClients.GetOrAdd(
            key,
            _ => CreateAdministrationClient(connection));
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var client in _clients.Values)
        {
            await client.DisposeAsync();
        }

        _clients.Clear();
        _administrationClients.Clear();
        _credentials.Clear();
    }

    private ServiceBusClient CreateClient(ServiceBusConnection connection)
    {
        var options = new ServiceBusClientOptions
        {
            TransportType = connection.TransportType == ContractTransportType.AmqpTcp
                ? AzureTransportType.AmqpTcp
                : AzureTransportType.AmqpWebSockets,
            RetryOptions = new ServiceBusRetryOptions
            {
                Mode = ServiceBusRetryMode.Exponential,
                MaxRetries = 3,
                Delay = TimeSpan.FromSeconds(1),
                MaxDelay = TimeSpan.FromSeconds(10),
                TryTimeout = TimeSpan.FromSeconds(60)
            }
        };

        return connection.AuthenticationType == ServiceBusAuthenticationType.ConnectionString
            ? new ServiceBusClient(
                connection.ConnectionString ?? throw MissingConnectionString(connection),
                options)
            : new ServiceBusClient(
                connection.FullyQualifiedName,
                GetCredential(connection),
                options);
    }

    private ServiceBusAdministrationClient CreateAdministrationClient(
        ServiceBusConnection connection)
    {
        return connection.AuthenticationType == ServiceBusAuthenticationType.ConnectionString
            ? new ServiceBusAdministrationClient(
                connection.ConnectionString ?? throw MissingConnectionString(connection))
            : new ServiceBusAdministrationClient(
                connection.FullyQualifiedName,
                GetCredential(connection));
    }

    private TokenCredential GetCredential(ServiceBusConnection connection)
    {
        var key = GetCacheKey(connection);
        return _credentials.GetOrAdd(key, _ => CreateCredential(connection));
    }

    private static TokenCredential CreateCredential(ServiceBusConnection connection)
    {
        return connection.AuthenticationType switch
        {
            ServiceBusAuthenticationType.AzureCli => new AzureCliCredential(
                new AzureCliCredentialOptions
                {
                    TenantId = connection.TenantId
                }),
            ServiceBusAuthenticationType.DefaultAzureCredential =>
                new DefaultAzureCredential(
                    new DefaultAzureCredentialOptions
                    {
                        TenantId = connection.TenantId,
                        ManagedIdentityClientId = connection.ClientId
                    }),
            ServiceBusAuthenticationType.WorkloadIdentity =>
                new WorkloadIdentityCredential(
                    new WorkloadIdentityCredentialOptions
                    {
                        TenantId = Require(connection.TenantId, nameof(connection.TenantId)),
                        ClientId = Require(connection.ClientId, nameof(connection.ClientId)),
                        TokenFilePath = Require(
                            connection.TokenFilePath,
                            nameof(connection.TokenFilePath))
                    }),
            ServiceBusAuthenticationType.InteractiveBrowser =>
                new InteractiveBrowserCredential(
                    new InteractiveBrowserCredentialOptions
                    {
                        TenantId = connection.TenantId,
                        ClientId = connection.ClientId
                    }),
            _ => throw new NotSupportedException(
                $"Authentication type {connection.AuthenticationType} is not supported.")
        };
    }

    private static string GetCacheKey(ServiceBusConnection connection)
    {
        var value = string.Join(
            '|',
            connection.Name,
            connection.FullyQualifiedName,
            connection.AuthenticationType,
            connection.TransportType,
            connection.TenantId,
            connection.ClientId,
            connection.TokenFilePath,
            connection.ConnectionString);

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static ArgumentException MissingConnectionString(ServiceBusConnection connection) =>
        new($"Connection {connection.Name} does not contain a connection string.");

    private static string Require(string? value, string propertyName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"{propertyName} is required.", propertyName)
            : value;
}
