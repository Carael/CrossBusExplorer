using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CrossBusExplorer.Management;
using CrossBusExplorer.Website.Extensions;
using MudBlazor;
namespace CrossBusExplorer.Website.Models;

public class SaveConnectionForm : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string? _connectionString;

    [Label("Connection string")]
    [Required(ErrorMessage = "ConnectionString is required")]
    [ConnectionStringValidation(ErrorMessage = "ConnectionString has invalid format!")]
    public string? ConnectionString
    {
        get => _connectionString;
        set
        {
            _connectionString = value;

            if (string.IsNullOrEmpty(Name))
            {
                Name = ServiceBusConnectionStringHelper.TryGetNameFromConnectionString(
                    _connectionString);
            }

            this.Notify(PropertyChanged);
        }
    }

    private string? _name;
    [Label("Name")]
    public string? Name
    {
        get => _name;
        set
        {
            _name = value;
            this.Notify(PropertyChanged);
        }
    }
    
    private string? _folder;
    [Label("Folder")]
    public string? Folder
    {
        get => _folder;
        set
        {
            _folder = value;
            this.Notify(PropertyChanged);
        }
    }
}