using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.Website.Models;
namespace CrossBusExplorer.Website.ViewModels;

public interface IQueueViewModel : INotifyPropertyChanged
{
    event QueueAddedEventHandler? QueueAdded;
    QueueFormModel? Form { get; set; }
    Task InitializeForm(
        string connectionName, 
        string? queueName,
        CancellationToken cancellationToken);

    Task UpdateQueueFromAsync(string connectionName);
    
    QueueDetails? QueueDetails { get; }
    void NavigateToNewQueueForm(string connectionName);
    void CloneQueue(string connectionName, string queueName);
}