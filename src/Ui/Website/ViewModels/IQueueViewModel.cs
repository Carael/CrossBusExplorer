using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.Website.Models;
namespace CrossBusExplorer.Website.ViewModels;

public interface IQueueViewModel : INotifyPropertyChanged
{
    QueueFormModel? Form { get; set; }
    Task LoadQueueAsync(
        string connectionName, 
        string queueName,
        CancellationToken cancellationToken);

    Task UpdateQueueFromAsync(string connectionName);
    
    QueueDetails? QueueDetails { get; set; }
}