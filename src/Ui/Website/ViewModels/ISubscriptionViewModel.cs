using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.Website.Models;
namespace CrossBusExplorer.Website.ViewModels;

public interface ISubscriptionViewModel : INotifyPropertyChanged
{
    event SubscriptionAddedEventHandler? SubscriptionAdded;
    event SubscriptionRemovedEventHandler? SubscriptionRemoved;
    SubscriptionFormModel? Form { get; }
    SubscriptionDetails? SubscriptionDetails { get; }
    Task InitializeFormAsync(
        string connectionName,
        string topicName,
        string? subscriptionName,
        CancellationToken cancellationToken);
    Task SaveSubscriptionFormAsync(string connectionName);
    void NavigateToNewSubscriptionForm(string connectionName, string topicName);
    Task CloneSubscriptionAsync(
        string connectionName,
        string sourceTopicName,
        string sourceSubscriptionName,
        CancellationToken cancellationToken);
    Task DeleteSubscriptionAsync(
        string connectionName, 
        string topicName,
        string subscriptionName,
        CancellationToken cancellationToken);
    Task UpdateSubscriptionStatus(
        string connectionName,
        string topicName,
        string subscriptionName,
        ServiceBusEntityStatus status,
        CancellationToken cancellationToken);
    Task PurgeMessages(
        string connectionName,
        string topicName,
        string subscriptionName,
        CancellationToken cancellationToken);
    Task ResendDeadLetters(
        string connectionName,
        string topicName,
        string subscriptionName,
        CancellationToken cancellationToken);
}