using CrossBusExplorer.Management;
using Material.Blazor;
using Microsoft.AspNetCore.Components;
using Ui.Models;
namespace Ui.Pages;

public partial class Connections
{
    [Inject]
    private IConnectionManagement ConnectionManagement { get; set; }
    private MBDialog AddConnectionDialog { get; set; }
    [Inject]
    private IMBToastService ToastService { get; set; }
    private IList<ServiceBusConnection>? ConnectionsList;
    private AddConnectionModel AddConnection { get; set; }

    public Connections()
    {

    }

    protected override async Task OnInitializedAsync()
    {
        ConnectionsList = await ConnectionManagement.GetAsync(default);
    }

    private async Task TryParseName()
    {
        if (AddConnection is { ConnectionString: { } } &&
            ServiceBusConnectionStringHelper.IsValid(AddConnection?.ConnectionString))
        {
            AddConnection.Name =
                ServiceBusConnectionStringHelper.GetNameFromConnectionString(
                    AddConnection.ConnectionString);
            
            await AddConnection.NameChanged.InvokeAsync(AddConnection.Name);

        }
    }

    private async Task ShowAddConnectionDialog()
    {
        AddConnection = new AddConnectionModel();
        _ = AddConnectionDialog.ShowAsync();

        await Task.CompletedTask;
    }

    private void AddConnectionDialogInvalid()
    {
        ToastService.ShowToast(heading: "Save connection invalid",
            message: $"Save form was invalid", level: MBToastLevel.Warning, showIcon: false);
    }

    private async Task AddConnectionDialogCanceled()
    {
        await AddConnectionDialog.HideAsync();
        ToastService.ShowToast(heading: "Save connection cancelled",
            message: "The cancel button was selected", level: MBToastLevel.Success,
            showIcon: false);
    }

    private async Task AddConnectionDialogSubmitted()
    {
        await ConnectionManagement.AddAsync(
            new ServiceBusConnection(
                AddConnection.Name,
                AddConnection.ConnectionString),
            default);
        await AddConnectionDialog.HideAsync();

        ToastService.ShowToast(heading: "Save connection success",
            message: $"Connection name '{AddConnection.Name}'.", level: MBToastLevel.Success,
            showIcon: false);

        ConnectionsList = await ConnectionManagement.GetAsync(default);
    }
}