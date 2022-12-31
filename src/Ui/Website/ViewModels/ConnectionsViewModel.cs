using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CrossBusExplorer.Management;
using CrossBusExplorer.Management.Contracts;
using CrossBusExplorer.Website.Exceptions;
using CrossBusExplorer.Website.Extensions;
using CrossBusExplorer.Website.Models;
using CrossBusExplorer.Website.Shared;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
namespace CrossBusExplorer.Website.ViewModels;

public class ConnectionsViewModel : IConnectionsViewModel
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event SettingsChangedEventHandler? OnSettingsChanged;

    private readonly ISnackbar _snackbar;
    private readonly IDialogService _dialogService;
    private readonly IConnectionManagement _connectionManagement;
    private readonly ISettingsService _settingsService;

    public ConnectionsViewModel(
        ISnackbar snackbar,
        IDialogService dialogService,
        IConnectionManagement connectionManagement,
        ISettingsService settingsService)
    {
        _snackbar = snackbar;
        _dialogService = dialogService;
        _connectionManagement = connectionManagement;
        _settingsService = settingsService;
    }

    private ObservableCollection<ServiceBusConnectionWithFolder> _serviceBusConnections =
        new ObservableCollection<ServiceBusConnectionWithFolder>();

    public ObservableCollection<ServiceBusConnectionWithFolder> ServiceBusConnections
    {
        get => _serviceBusConnections;
        private set
        {
            _serviceBusConnections = value;
            _serviceBusConnections.CollectionChanged += (_, _) =>
            {
                this.Notify(PropertyChanged);
            };
            this.Notify(PropertyChanged);
        }
    }

    private ObservableCollection<FolderSettings> _folders =
        new ObservableCollection<FolderSettings>();

    public ObservableCollection<FolderSettings> Folders
    {
        get => _folders;
        private set
        {
            _folders = value;
            _folders.CollectionChanged += (_, _) =>
            {
                this.Notify(PropertyChanged);
            };
            this.Notify(PropertyChanged);
        }
    }

    private bool _saveDialogVisible;

    public bool SaveDialogVisible
    {
        get => _saveDialogVisible;
        set
        {
            _saveDialogVisible = value;
            this.Notify(PropertyChanged);
        }
    }

    private SaveConnectionForm? _saveConnectionForm;

    public SaveConnectionForm? SaveConnectionForm
    {
        get => _saveConnectionForm;
        set
        {
            _saveConnectionForm = value;
            this.Notify(PropertyChanged);
        }
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var serviceBusConnections = await _connectionManagement.GetAsync(default);
        var folderSettings = await InitializeFoldersSettingsAsync(serviceBusConnections, default);

        ServiceBusConnections = new ObservableCollection<ServiceBusConnectionWithFolder>(
            GetServiceBusConnectionsWithFolder(serviceBusConnections, folderSettings));

        Folders = new ObservableCollection<FolderSettings>(folderSettings);
        OnSettingsChanged(Folders);
    }

    private IEnumerable<ServiceBusConnectionWithFolder> GetServiceBusConnectionsWithFolder(
        IEnumerable<ServiceBusConnection> serviceBusConnections,
        IEnumerable<FolderSettings> foldersSettings)
    {
        foreach (var folderSetting in foldersSettings.OrderBy(p => p.Index))
        {
            foreach (ServiceBusConnectionSettings serviceBusConnectionSetting in
                folderSetting.ServiceBusConnectionSettings.OrderBy(p => p.Index))
            {
                var serviceBusConnection = serviceBusConnections.FirstOrDefault(p =>
                    p.Name.EqualsInvariantIgnoreCase(serviceBusConnectionSetting.Name));

                if (serviceBusConnection != null)
                {
                    yield return new ServiceBusConnectionWithFolder(
                        serviceBusConnection,
                        folderSetting.Name);
                }
            }
        }
    }

    private async Task<List<FolderSettings>> InitializeFoldersSettingsAsync(
        IList<ServiceBusConnection> serviceBusConnections,
        CancellationToken cancellationToken)
    {
        var settings = await _settingsService.GetAsync(cancellationToken);

        var foldersSettings = settings.FolderSettings;

        var saveSettings = false;

        var defaultFolder = foldersSettings.FirstOrDefault(p => p.Name == "");
        if (defaultFolder == null)
        {
            defaultFolder =
                new FolderSettings(string.Empty, 0, new List<ServiceBusConnectionSettings>());
            foldersSettings.Add(defaultFolder);
            saveSettings = true;
        }

        foreach (ServiceBusConnection serviceBusConnection in serviceBusConnections)
        {
            var folderSettings = TryGetFolderSettings(foldersSettings, serviceBusConnection.Name);

            if (folderSettings == null)
            {
                defaultFolder.AddServiceBusConnectionSetting(serviceBusConnection);
                saveSettings = true;
            }
        }

        if (saveSettings)
        {
            for (var i = 0; i < foldersSettings.Count; i++)
            {
                foldersSettings[i].UpdateIndex(i);
            }

            await _settingsService.SaveAsync(settings, cancellationToken);
        }

        return foldersSettings;
    }

    private void AddOrReplaceFolder(
        string? folderName,
        string connectionName)
    {
        folderName ??= "";

        FolderSettings? folder =
            Folders.FirstOrDefault(p => p.Name.EqualsInvariantIgnoreCase(folderName));

        if (folder == null)
        {
            folder = new FolderSettings(folderName, Folders.MaxBy(p => p.Index)?.Index ?? 0,
                new List<ServiceBusConnectionSettings>());
        }

        var connectionSettings =
            folder.ServiceBusConnectionSettings.FirstOrDefault(p =>
                p.Name.EqualsInvariantIgnoreCase(connectionName));

        if (connectionSettings == null)
        {
            connectionSettings = new ServiceBusConnectionSettings(
                connectionName,
                folder.ServiceBusConnectionSettings.MaxBy(p => p.Index).Index + 1);

            folder.ServiceBusConnectionSettings
                .AddOrReplace(
                    p => p.Name.EqualsInvariantIgnoreCase(connectionName),
                    connectionSettings);
        }

        Folders.AddOrReplace(p => p.Name.EqualsInvariantIgnoreCase(folderName), folder);
    }

    private async Task SaveConnectionAsync(
        string name,
        string connectionString,
        string folder,
        CancellationToken cancellationToken)
    {
        ServiceBusConnection newConnection =
            await _connectionManagement.SaveAsync(
                name,
                connectionString,
                folder,
                cancellationToken);


        RemoveOrReplace(new ServiceBusConnectionWithFolder(newConnection, folder));
        AddOrReplaceFolder(folder, name);
        OnSettingsChanged(Folders);
    }

    private async Task RemoveConnectionAsync(
        ServiceBusConnectionWithFolder item,
        CancellationToken cancellationToken)
    {
        await _connectionManagement.DeleteAsync(
            item.Name,
            cancellationToken);

        ServiceBusConnections.Remove(item);

        var folderSettings = TryGetFolderSettings(Folders, item.Name);
        if (folderSettings != null)
        {
            folderSettings.ServiceBusConnectionSettings
                .Remove(folderSettings.ServiceBusConnectionSettings
                    .First(p => p.Name.EqualsInvariantIgnoreCase(item.Name)));
        }

        OnSettingsChanged(Folders);

        await SaveFoldersSettingsAsync(cancellationToken);
        this.Notify(PropertyChanged);
    }

    public void OpenSaveDialog(ServiceBusConnectionWithFolder? model = null)
    {
        _saveConnectionForm = model == null
            ? new SaveConnectionForm()
            : new SaveConnectionForm
            {
                Name = model.Name,
                ConnectionString = model.ConnectionString,
                Folder = TryGetFolderSettings(Folders, model?.Name)?.Name
            };
        _saveDialogVisible = true;
        _saveConnectionForm.PropertyChanged += (_, _) =>
        {
            this.Notify(PropertyChanged);
        };
    }

    private FolderSettings GetFolderSettings(
        IEnumerable<FolderSettings> folderSettings,
        string connectionName)
    {
        var folder = TryGetFolderSettings(folderSettings, connectionName);

        if (folder == null)
        {
            throw new FolderSettingsForConnectionNotFoundException(connectionName);
        }

        return folder;
    }

    private FolderSettings? TryGetFolderSettings(
        IEnumerable<FolderSettings> folderSettings,
        string? connectionName)
    {
        if (connectionName == null)
        {
            return null;
        }

        foreach (FolderSettings folder in folderSettings)
        {
            if (folder.ServiceBusConnectionSettings.FirstOrDefault(p =>
                    p.Name.EqualsInvariantIgnoreCase(connectionName)) != null)
            {
                return folder;
            }
        }

        return null;
    }

    public async Task OnValidSaveConnectionSubmit()
    {
        await SaveConnectionAsync(
            _saveConnectionForm.Name ??
            ServiceBusConnectionStringHelper.TryGetNameFromConnectionString(
                _saveConnectionForm.ConnectionString!),
            _saveConnectionForm.ConnectionString!,
            _saveConnectionForm.Folder ?? "",
            default);

        _saveDialogVisible = false;

        _snackbar.Add($"Connection {_saveConnectionForm.Name} added.", Severity.Success);

    }

    public void ViewConnectionString(string connectionString)
    {
        var parameters = new DialogParameters();
        parameters.Add(nameof(ViewDialog.ContentText), connectionString);

        _dialogService.Show<ViewDialog>(
            "Service bus connection string",
            parameters,
            new DialogOptions
            {
                FullWidth = true,
                CloseOnEscapeKey = true
            });
    }

    public async Task OpenDeleteDialog(ServiceBusConnectionWithFolder item)
    {
        var parameters = new DialogParameters();
        parameters.Add(
            "ContentText",
            $"Are you sure you want to remove {item.Name} connection?");

        var dialog = _dialogService.Show<ConfirmDialog>(
            "Confirm",
            parameters,
            new DialogOptions { CloseOnEscapeKey = true });

        var result = await dialog.Result;

        if (result.Data is true)
        {
            await RemoveConnectionAsync(item, default);

            _snackbar.Add(
                $"Connection {item.Name} successfully deleted.",
                Severity.Success);
        }
    }

    public async Task SubmitSaveConnectionForm(EditForm editForm)
    {
        if (editForm.EditContext.Validate())
        {
            await OnValidSaveConnectionSubmit();
        }
    }
    public async Task UpdateConnectionPosition(
        ServiceBusConnectionWithFolder serviceBusConnection,
        int index,
        string newFolder,
        CancellationToken cancellationToken)
    {
        // update folder
        if (serviceBusConnection.Folder != newFolder)
        {
            var oldFolderSettings =
                Folders.First(p => p.Name.EqualsInvariantIgnoreCase(serviceBusConnection.Folder));

            var serviceBusConnectionSettings = oldFolderSettings
                .ServiceBusConnectionSettings
                .First(p =>
                    p.Name.EqualsInvariantIgnoreCase(serviceBusConnection
                        .Name));
            oldFolderSettings.ServiceBusConnectionSettings.Remove(serviceBusConnectionSettings);

            var newFolderSettings =
                Folders.First(p => p.Name.EqualsInvariantIgnoreCase(newFolder));

            serviceBusConnectionSettings.UpdateIndex(index);

            newFolderSettings.ServiceBusConnectionSettings.AddOrReplace(p =>
                p.Name.EqualsInvariantIgnoreCase(
                    serviceBusConnectionSettings.Name), serviceBusConnectionSettings);

            serviceBusConnection.UpdateFolder(newFolder);
        }

        //update index
        var folderSettings =
            Folders.First(p => p.Name.EqualsInvariantIgnoreCase(newFolder));

        folderSettings.UpdateServiceBusConnectionsIndexes(
            serviceBusConnection.Name,
            index);

        await SaveFoldersSettingsAsync(cancellationToken);
        OnSettingsChanged(Folders);
    }

    public async Task UpdateFolderPositionAsync(
        FolderSettings folder,
        DirectionChange change,
        CancellationToken cancellationToken)
    {
        var currentIndex = folder.Index;
        var newIndex = change switch
        {
            DirectionChange.Up => currentIndex > 0 ? currentIndex - 1 : currentIndex,
            DirectionChange.Down => folder.Index < Folders.Count - 1
                ? currentIndex + 1 : folder.Index,
            _ => throw new NotSupportedException($"{change} is not supported.")
        };

        if (currentIndex != newIndex)
        {
            var newItem = Folders.First(p => p.Index == currentIndex);
            var oldItem = Folders.First(p => p.Index == newIndex);

            newItem.UpdateIndex(newIndex);
            oldItem.UpdateIndex(currentIndex);

            await SaveFoldersSettingsAsync(cancellationToken);
            OnSettingsChanged(Folders);
        }
    }

    private async Task SaveFoldersSettingsAsync(CancellationToken cancellationToken)
    {
        var userSettings = await _settingsService.GetAsync(cancellationToken);
        var i = 0;
        foreach (FolderSettings folderSettings in Folders.OrderBy(p => p.Index))
        {
            folderSettings.UpdateIndex(i);
            i++;
        }
        userSettings.FolderSettings = Folders.OrderBy(p => p.Index).ToList();
        await _settingsService.SaveAsync(userSettings, cancellationToken);
    }

    private void RemoveOrReplace(ServiceBusConnectionWithFolder newConnection)
    {
        var current = ServiceBusConnections.FirstOrDefault(p =>
            p.Name.EqualsInvariantIgnoreCase(
                newConnection.Name));

        if (current != null)
        {
            ServiceBusConnections.Remove(current);
        }

        ServiceBusConnections.Add(newConnection);
    }
}