using System.ComponentModel.DataAnnotations;
using CrossBusExplorer.Management;
namespace CrossBusExplorer.Website;

public class ConnectionStringValidationAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        return value is string connectionString &&
               ServiceBusConnectionStringHelper.IsValid(connectionString);
    }
}