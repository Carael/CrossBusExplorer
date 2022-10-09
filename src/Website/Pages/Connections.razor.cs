using System.Collections.Generic;
using System.Threading.Tasks;
using CrossBusExplorer.Management;
using CrossBusExplorer.Website.Models;
using Material.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
namespace CrossBusExplorer.Website.Pages;

public partial class Connections
{
    [Inject]
    private IConnectionManagement ConnectionManagement { get; set; }
    private MBDialog AddConnectionDialog { get; set; }
    [Inject]
    private IMBToastService ToastService { get; set; }
    private IList<ServiceBusConnection>? ConnectionsList;
    private AddConnectionModel AddConnection = new AddConnectionModel();
    private EditContext EditContext;


    public Connections()
    {

    }

    protected override async Task OnInitializedAsync()
    {
        EditContext = new EditContext(AddConnection);
        EditContext.OnFieldChanged += EditContext_OnFieldChanged;
        ConnectionsList = await ConnectionManagement.GetAsync(default);
        
        base.OnInitialized();
    }
    
    
    // Note: The OnFieldChanged event is raised for each field in the model
    private void EditContext_OnFieldChanged(object sender, FieldChangedEventArgs e)
    {
        if (e.FieldIdentifier.FieldName == nameof(AddConnectionModel.ConnectionString))
        {
            if (e.FieldIdentifier.Model is AddConnectionModel model)
            {
                AddConnection.Name = TryParseName(model.ConnectionString);
            }
            
        }
    }

    private string? TryParseName(string connectionString)
    {
        if (connectionString!=null &&
            ServiceBusConnectionStringHelper.IsValid(connectionString))
        {
            return 
                ServiceBusConnectionStringHelper.GetNameFromConnectionString(
                    connectionString);
        }

        return null;
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