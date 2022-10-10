namespace CrossBusExplorer.Management;

public class ServiceBusConnectionDoesntExist : Exception
{
    public ServiceBusConnectionDoesntExist(string name) : base(
        $"Service Bus connection with name {name} doesn't exist.")
    {
    }
}