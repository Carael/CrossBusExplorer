using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Material.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
namespace CrossBusExplorer.Website.Pages;

public partial class Index
{
    [Inject] private NavigationManager NavigationManager { get; set; }
    [Inject] private IJSRuntime JSRuntime { get; set; }

#if DEBUG
    private string BuildMode { get; set; } = "debug";
#else
    private string BuildMode { get; set; } = "release";
#endif

    private string OSArchitecture { get; set; }
    private string OSDescription { get; set; }
    private string Runtime { get; set; }
    private string Version { get; set; }

    public Index()
    {
        OSArchitecture = RuntimeInformation.OSArchitecture.ToString();
        OSDescription = RuntimeInformation.OSDescription.ToString();
        Runtime = RuntimeInformation.FrameworkDescription.ToString();
        Version = MBVersion.MaterialBlazorVersion();
    }


    private async Task NavigateToDocs()
    {
        var baseURI = NavigationManager.BaseUri;
        await JSRuntime.InvokeAsync<object>("open", $"{baseURI}docs", "_blank");
    }



    private void NavigateToButton()
    {
        NavigationManager.NavigateTo("button");
    }
}
