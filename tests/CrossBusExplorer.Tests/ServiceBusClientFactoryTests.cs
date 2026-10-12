using CrossBusExplorer.Management.Contracts;
using CrossBusExplorer.ServiceBus;

namespace CrossBusExplorer.Tests;

public sealed class ServiceBusClientFactoryTests
{
    [Theory]
    [InlineData(ServiceBusAuthenticationType.AzureCli)]
    [InlineData(ServiceBusAuthenticationType.DefaultAzureCredential)]
    [InlineData(ServiceBusAuthenticationType.InteractiveBrowser)]
    public async Task CreatesAndReusesPasswordlessClients(
        ServiceBusAuthenticationType authenticationType)
    {
        await using var factory = new ServiceBusClientFactory();
        var connection = ServiceBusConnection.CreatePasswordless(
            "profile",
            "profile.servicebus.windows.net",
            authenticationType);

        var first = factory.GetClient(connection);
        var second = factory.GetClient(connection);

        Assert.Same(first, second);
        Assert.Equal("profile.servicebus.windows.net", first.FullyQualifiedNamespace);
    }

    [Fact]
    public async Task ValidatesRequiredWorkloadIdentityFields()
    {
        await using var factory = new ServiceBusClientFactory();
        var connection = ServiceBusConnection.CreatePasswordless(
            "workload",
            "workload.servicebus.windows.net",
            ServiceBusAuthenticationType.WorkloadIdentity);

        var exception = Assert.Throws<ArgumentException>(() => factory.GetClient(connection));

        Assert.Equal("TenantId", exception.ParamName);
    }
}
