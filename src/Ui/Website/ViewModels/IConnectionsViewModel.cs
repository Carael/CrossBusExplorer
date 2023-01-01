using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CrossBusExplorer.Website.Models;
using CrossBusExplorer.Website.ViewModels;
using Microsoft.AspNetCore.Components.Forms;
namespace CrossBusExplorer.Website;

public interface IConnectionsViewModel : INotifyPropertyChanged
{
    event SettingsChangedEventHandler? OnSettingsChanged;
    public SaveConnectionForm? SaveConnectionForm { get; }
    bool SaveDialogVisible { get; set; }
    ObservableCollection<FolderSettings> Folders { get; }
    ObservableCollection<ServiceBusConnectionWithFolder> ServiceBusConnections { get; }
    Task InitializeAsync(CancellationToken cancellationToken);
    void OpenSaveDialog(ServiceBusConnectionWithFolder? model = null);
    Task OpenDeleteDialog(ServiceBusConnectionWithFolder item);
    void ViewConnectionString(string connectionString);
    Task OnValidSaveConnectionSubmit();
    Task SubmitSaveConnectionForm(EditForm editForm);
    Task UpdateConnectionPosition(ServiceBusConnectionWithFolder serviceBusConnection,
        int index,
        string newFolder, CancellationToken cancellationToken);
    Task UpdateFolderPositionAsync(
        FolderSettings folder, 
        DirectionChange up,
        CancellationToken cancellationToken);
}