using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.Website.Models;
namespace CrossBusExplorer.Website.ViewModels;

public interface IQueueViewModel : INotifyPropertyChanged
{
    event QueueAddedEventHandler? QueueAdded;
    event QueueRemovedEventHandler? QueueRemoved;
    QueueFormModel? Form { get; set; }
    Task InitializeForm(
        string connectionName, 
        string? queueName,
        CancellationToken cancellationToken);
    Task UpdateQueueFromAsync(string connectionName);
    QueueDetails? QueueDetails { get; }
    void NavigateToNewQueueForm(string connectionName);
    Task CloneQueue(
        string connectionName,
        string sourceQueueName,
        CancellationToken cancellationToken);
    Task DeleteQueue(string connectionName, string queueName, CancellationToken cancellationToken);
    Task ViewMessages(string connectionName, string queueName, CancellationToken cancellationToken);
}