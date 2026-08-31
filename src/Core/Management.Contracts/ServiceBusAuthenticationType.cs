namespace CrossBusExplorer.Management.Contracts;

public enum ServiceBusAuthenticationType
{
    ConnectionString,
    AzureCli,
    DefaultAzureCredential,
    WorkloadIdentity,
    InteractiveBrowser
}
