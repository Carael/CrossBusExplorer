using System.Threading.Tasks;
using CrossBusExplorer.Management;
using CrossBusExplorer.Management.Contracts;
using CrossBusExplorer.Website.Models;
using CrossBusExplorer.Website.Shared;
using Microsoft.AspNetCore.Components;
using MudBlazor;
namespace CrossBusExplorer.Website.Pages;

public partial class Connections
{
    private readonly DialogOptions _saveDialogOptions = new DialogOptions { FullWidth = true };

    private bool _saveDialogVisible;
    private SaveConnectionForm _saveEditConnectionForm = null!;
    
    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;
    [Inject]
    private IDialogService DialogService { get; set; } = null!;
    [Inject]
    private IConnectionsViewModel ConnectionsViewModel { get; set; }

    protected override void OnInitialized()
    {
        ConnectionsViewModel.PropertyChanged += (_, _) =>
        {
            StateHasChanged();
        };
        
        base.OnInitialized();
    }

    private void OpenSaveDialog(ServiceBusConnection? model = null)
    {
        _saveEditConnectionForm = model == null
            ? new SaveConnectionForm()
            : new SaveConnectionForm
                { Name = model.Name, ConnectionString = model.ConnectionString };
        _saveDialogVisible = true;
    }

    private async Task OnValidSaveConnectionSubmit()
    {
        await ConnectionsViewModel.SaveConnectionAsync(
            _saveEditConnectionForm.Name ??
            ServiceBusConnectionStringHelper.TryGetNameFromConnectionString(
                _saveEditConnectionForm.ConnectionString!),
            _saveEditConnectionForm.ConnectionString!,
            default);

        _saveDialogVisible = false;

        Snackbar.Add($"Connection {_saveEditConnectionForm.Name} added.", Severity.Success);
        
    }
    private void ViewConnectionString(string connectionString)
    {
        var parameters = new DialogParameters();
        parameters.Add(nameof(ViewDialog.ContentText), connectionString);

        DialogService.Show<ViewDialog>(
            "Service bus connection string",
            parameters,
            new DialogOptions
            {
                FullWidth = true,
                CloseOnEscapeKey = true
            });
    }
    private async Task OpenDeleteDialog(ServiceBusConnection serviceBusConnection)
    {
        var parameters = new DialogParameters();
        parameters.Add(
            "ContentText",
            $"Are you sure you want to remove {serviceBusConnection.Name} connection?");

        var dialog = DialogService.Show<ConfirmDialog>("Confirm", parameters);
        var result = await dialog.Result;

        if (result.Data is true)
        {
            ConnectionsViewModel.RemoveConnectionAsync(serviceBusConnection, default);

            Snackbar.Add(
                $"Connection {serviceBusConnection.Name} successfully deleted.", 
                Severity.Success);
        }
    }
}