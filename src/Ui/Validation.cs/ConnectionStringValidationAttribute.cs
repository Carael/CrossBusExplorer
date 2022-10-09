using System.ComponentModel.DataAnnotations;
using CrossBusExplorer.Management;
namespace Ui.Validation.cs;

public class ConnectionStringValidationAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        return value is string connectionString &&
               ServiceBusConnectionStringHelper.IsValid(connectionString);
    }
}