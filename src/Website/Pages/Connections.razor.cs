using System.Collections.Generic;
using System.Threading.Tasks;
using CrossBusExplorer.Management;
using CrossBusExplorer.Website.Models;
using Microsoft.AspNetCore.Components;
namespace CrossBusExplorer.Website.Pages;

public partial class Connections
{
    [Inject]
    private IConnectionManagement ConnectionManagement { get; set; } = null!;
    // [Inject]
    // private IMBToastService ToastService { get; set; } = null!;
    // private MBDialog AddConnectionDialog { get; set; } = null!;
    // private MBDialog ViewConnectionStringDialog { get; set; } = null!;
    // private MBConfirmationDialog DeleteDialog { get; set; } = null!;

    private IList<ServiceBusConnection> _connectionsList = new List<ServiceBusConnection>();
    private AddConnectionModel _addEditConnectionModel = new AddConnectionModel();
    private string? _viewConnectionStringValue = null;

    protected override async Task OnInitializedAsync()
    {
        await ReloadConnectionsAsync();
        await base.OnInitializedAsync().ConfigureAwait(false);
    }
    
    private async Task ReloadConnectionsAsync()
    {
        _connectionsList = await ConnectionManagement.GetAsync(default);
    }
/*
    private async Task ShowAddConnectionDialog()
    {
        _addEditConnectionModel = new AddConnectionModel();
        await AddConnectionDialog.ShowAsync();
    }

    private async Task ShowDeleteConnectionConfirm(string connectionName)
    {
        var result = await DeleteDialog?.ShowAsync()!;

        if (result == WellKnown.DefaultConfirmSuccessResult)
        {
            await ConnectionManagement!.DeleteAsync(connectionName, default);

            ToastService!.ShowToast(heading: "Connection deleted",
                message: $"Connection deleted", level: MBToastLevel.Success, showIcon: false);

            await ReloadConnectionsAsync();
        }
    }

    private async Task AddConnectionDialogCanceled()
    {
        _addEditConnectionModel = new AddConnectionModel();
        await AddConnectionDialog.HideAsync();
    }

    private async Task AddConnectionDialogSubmitted()
    {
        await ConnectionManagement.SaveAsync(
            _addEditConnectionModel.Name ??
            ServiceBusConnectionStringHelper.TryGetNameFromConnectionString(_addEditConnectionModel
                .ConnectionString!),
            _addEditConnectionModel.ConnectionString!,
            default);

        await AddConnectionDialog.HideAsync();

        ToastService.ShowToast(heading: "Save connection success",
            message: $"Connection name '{_addEditConnectionModel.Name}'.",
            level: MBToastLevel.Success,
            showIcon: false);

        _addEditConnectionModel = new AddConnectionModel();
        await ReloadConnectionsAsync();
    }

    private async Task EditConnection(ServiceBusConnection connection)
    {
        _addEditConnectionModel = new AddConnectionModel
        {
            ConnectionString = connection.ConnectionString,
            Name = connection.Name
        };

        await AddConnectionDialog!.ShowAsync();
    }

    private async Task ViewConnectionString(string connectionString)
    {
        _viewConnectionStringValue = connectionString;
        await ViewConnectionStringDialog!.ShowAsync();
    }
    */
}