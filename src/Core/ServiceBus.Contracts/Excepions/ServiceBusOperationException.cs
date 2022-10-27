namespace CrossBusExplorer.ServiceBus.Contracts;

public class ServiceBusOperationException : Exception
{
    public ServiceBusOperationException(string code, string message) : base(message)
    {
        Code = code;
    }
    
    public string Code { get; }
}