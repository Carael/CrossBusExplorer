using System.Threading.Tasks;
namespace CrossBusExplorer.Website.Jobs;

public delegate Task JobCompletedEventHandler(
    string connectionName,
    string queueOrTopicName,
    string? subscriptionName);