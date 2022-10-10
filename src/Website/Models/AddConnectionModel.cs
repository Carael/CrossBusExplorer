using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
namespace CrossBusExplorer.Website.Models;

public class AddConnectionModel
{
    [Required(ErrorMessage = "ConnectionString is required")]
    [ConnectionStringValidation(ErrorMessage = "ConnectionString has invalid format")]
    public string? ConnectionString { get; set; }
    
    [Parameter]
    public string? Name { get; set; }
}