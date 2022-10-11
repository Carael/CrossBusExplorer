using System.Collections.Generic;
using System.Threading.Tasks;
using CrossBusExplorer.Management;
using CrossBusExplorer.Website.Models;
using CrossBusExplorer.Website.Shared;
using Microsoft.AspNetCore.Components;
using MudBlazor;
namespace CrossBusExplorer.Website.Pages;

public partial class Connections
{
    [Inject]
    private IConnectionManagement ConnectionManagement{ get; set; } = null!;
    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;
    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    private bool _saveDialogVisible;
    private DialogOptions _saveDialogOptions = new() { FullWidth = true };

    private IList<ServiceBusConnection> _connectionsList = new List<ServiceBusConnection>();
    private SaveConnectionForm _saveEditConnectionForm { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await ReloadConnectionsAsync();
        await base.OnInitializedAsync().ConfigureAwait(false);
    }

    private async Task ReloadConnectionsAsync()
    {
        _connectionsList = await ConnectionManagement.GetAsync(default);
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
        await ConnectionManagement.SaveAsync(
            _saveEditConnectionForm.Name ??
            ServiceBusConnectionStringHelper.TryGetNameFromConnectionString(
                _saveEditConnectionForm.ConnectionString!),
            _saveEditConnectionForm.ConnectionString!,
            default);

        _saveDialogVisible = false;

        Snackbar.Add($"Connection {_saveEditConnectionForm.Name} added.", Severity.Success);
        await ReloadConnectionsAsync();
        StateHasChanged();
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
    private async Task OpenDeleteDialog(ServiceBusConnection context)
    {
        var parameters = new DialogParameters();
            parameters.Add(
                "ContentText", 
                $"Are you sure you want to remove {context.Name} connection?");

            var dialog = DialogService.Show<ConfirmDialog>("Confirm", parameters);
            var result = await dialog.Result;

            if (result.Data is true)
            {
                await ConnectionManagement.DeleteAsync(context.Name, default);
                await ReloadConnectionsAsync();
                Snackbar.Add($"Connection {context.Name} successfully deleted.", Severity.Success);
            }
    }
}