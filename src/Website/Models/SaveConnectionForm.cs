using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using MudBlazor;
namespace CrossBusExplorer.Website.Models;

public class SaveConnectionForm
{
    [Label("Connection string")]
    [Required(ErrorMessage = "ConnectionString is required")]
    [ConnectionStringValidation(ErrorMessage = "ConnectionString has invalid format!")]
    public string? ConnectionString { get; set; }
    
    [Label("Name")]
    public string? Name { get; set; }
}