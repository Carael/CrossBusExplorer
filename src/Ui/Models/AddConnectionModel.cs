using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Ui.Validation.cs;
namespace Ui.Models;

public class AddConnectionModel
{
    [Parameter]
    [Required(ErrorMessage = "Name is required")]
    public string? Name { get; set; }

    [Parameter]
    public EventCallback<string> NameChanged { get; set; }


    [Required(ErrorMessage = "ConnectionString is required")]
    [ConnectionStringValidation(ErrorMessage = "ConnectionString has invalid format")]
    public string? ConnectionString { get; set; }
}