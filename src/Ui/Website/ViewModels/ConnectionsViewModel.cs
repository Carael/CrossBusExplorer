using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CrossBusExplorer.Management;
using CrossBusExplorer.Management.Contracts;
using CrossBusExplorer.Website.Extensions;
using CrossBusExplorer.Website.Models;
using CrossBusExplorer.Website.Shared;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
namespace CrossBusExplorer.Website.ViewModels;

public class ConnectionsViewModel : IConnectionsViewModel
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly ISnackbar _snackbar;
    private readonly IDialogService _dialogService;
    private readonly IConnectionManagement _connectionManagement;

    public ConnectionsViewModel(
        ISnackbar snackbar,
        IDialogService dialogService,
        IConnectionManagement connectionManagement)
    {
        _snackbar = snackbar;
        _dialogService = dialogService;
        _connectionManagement = connectionManagement;
    }

    private ObservableCollection<ServiceBusConnection> _serviceBusConnections =
        new ObservableCollection<ServiceBusConnection>();
    
    public ObservableCollection<ServiceBusConnection> ServiceBusConnections
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
    
    private ObservableCollection<string> _folders =
        new ObservableCollection<string>();
    
    public ObservableCollection<string> Folders
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
        
        ServiceBusConnections = new ObservableCollection<ServiceBusConnection>();

        Folders = new ObservableCollection<string>(serviceBusConnections
            .Where(p => !string.IsNullOrEmpty(p.Folder))
            .Select(p => p.Folder)
            .Distinct(StringComparer.InvariantCultureIgnoreCase));
    }
    
    public void AddFolder(string folder)
    {
        Folders.Add(folder);
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
        RemoveOrReplace(newConnection);
    }
    
    

    private async Task RemoveConnectionAsync(
        ServiceBusConnection serviceBusConnection,
        CancellationToken cancellationToken)
    {
        await _connectionManagement.DeleteAsync(serviceBusConnection.Name, cancellationToken);

        _serviceBusConnections.Remove(serviceBusConnection);
    }

    public void OpenSaveDialog(ServiceBusConnection? model = null)
    {
        _saveConnectionForm = model == null
            ? new SaveConnectionForm()
            : new SaveConnectionForm
                { Name = model.Name, ConnectionString = model.ConnectionString };
        _saveDialogVisible = true;
        _saveConnectionForm.PropertyChanged += (_, _) =>
        {
            this.Notify(PropertyChanged);
        };
    }

    public async Task OnValidSaveConnectionSubmit()
    {
        await SaveConnectionAsync(
            _saveConnectionForm.Name ??
            ServiceBusConnectionStringHelper.TryGetNameFromConnectionString(
                _saveConnectionForm.ConnectionString!),
            _saveConnectionForm.ConnectionString!,
            _saveConnectionForm.Folder!,
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

    public async Task OpenDeleteDialog(ServiceBusConnection serviceBusConnection)
    {
        var parameters = new DialogParameters();
        parameters.Add(
            "ContentText",
            $"Are you sure you want to remove {serviceBusConnection.Name} connection?");

        var dialog = _dialogService.Show<ConfirmDialog>(
            "Confirm",
            parameters,
            new DialogOptions { CloseOnEscapeKey = true });

        var result = await dialog.Result;

        if (result.Data is true)
        {
            await RemoveConnectionAsync(serviceBusConnection, default);

            _snackbar.Add(
                $"Connection {serviceBusConnection.Name} successfully deleted.",
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

    private void RemoveOrReplace(ServiceBusConnection newConnection)
    {
        var current = ServiceBusConnections.FirstOrDefault(p =>
            p.Name.EqualsInvariantIgnoreCase(newConnection.Name));

        if (current != null)
        {
            _serviceBusConnections.Remove(current);
        }

        _serviceBusConnections.Add(newConnection);
    }
}