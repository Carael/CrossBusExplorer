using System.Threading.Tasks;
using CrossBusExplorer.Management;
using CrossBusExplorer.Management.Contracts;
using CrossBusExplorer.Website.Models;
using CrossBusExplorer.Website.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
namespace CrossBusExplorer.Website.Pages;

public partial class Connections
{
    private readonly DialogOptions _saveDialogOptions = new DialogOptions
        { FullWidth = true, CloseOnEscapeKey = true };
    private readonly ISnackbar _snackbar;
    private readonly IDialogService _dialogService;
    private readonly IConnectionsViewModel _model;

    private bool _saveDialogVisible;
    private SaveConnectionForm _saveEditConnectionForm = null!;
    private EditForm _editForm;
    private MudDialog _dialog;

    public Connections(
        ISnackbar snackbar,
        IDialogService dialogService,
        IConnectionsViewModel connectionsViewModel)
    {
        _snackbar = snackbar;
        _dialogService = dialogService;
        _model = connectionsViewModel;
    }

    protected override void OnInitialized()
    {
        _model.PropertyChanged += (_, _) =>
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
        await _model.SaveConnectionAsync(
            _saveEditConnectionForm.Name ??
            ServiceBusConnectionStringHelper.TryGetNameFromConnectionString(
                _saveEditConnectionForm.ConnectionString!),
            _saveEditConnectionForm.ConnectionString!,
            default);

        _saveDialogVisible = false;

        _snackbar.Add($"Connection {_saveEditConnectionForm.Name} added.", Severity.Success);

    }
    private void ViewConnectionString(string connectionString)
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
    private async Task OpenDeleteDialog(ServiceBusConnection serviceBusConnection)
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
            _model.RemoveConnectionAsync(serviceBusConnection, default);

            _snackbar.Add(
                $"Connection {serviceBusConnection.Name} successfully deleted.",
                Severity.Success);
        }
    }

    private async Task Submit()
    {
        if (_editForm.EditContext.Validate())
        {
            await OnValidSaveConnectionSubmit();
        }
    }
}