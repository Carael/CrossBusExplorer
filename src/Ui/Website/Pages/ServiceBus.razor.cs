using Microsoft.AspNetCore.Components;
namespace CrossBusExplorer.Website.Pages;

public partial class ServiceBus
{
    [Parameter]
    public string ServiceBusName { get; set; }
}