namespace CrossBusExplorer.ServiceBus.Contracts;

public class ValidationException: Exception
{
    public ValidationException(string code, string message) : base(message)
    {
        Code = code;
    }
    
    public string Code { get; }
}