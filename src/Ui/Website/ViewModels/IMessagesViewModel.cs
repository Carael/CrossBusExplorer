using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.Website.Models;
using MudBlazor;
namespace CrossBusExplorer.Website.ViewModels;

public interface IMessagesViewModel : INotifyPropertyChanged
{
    ObservableCollection<Message> Messages { get; }
    bool DialogVisible { get; set; }
    bool CanPeekMore(ReceiveMessagesForm formModel);
    Task PeekMore(ReceiveMessagesForm formModel, CancellationToken cancellationToken);
    Task OnSubmitReceiveForm(
        MudForm form,
        ReceiveMessagesForm formModel,
        CancellationToken cancellationToken);
    void Initialize(CurrentMessagesEntity entity);
    Task ViewMessageDetails(Message context);
}