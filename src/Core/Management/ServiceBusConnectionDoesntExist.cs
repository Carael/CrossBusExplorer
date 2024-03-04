namespace CrossBusExplorer.Management;

public class ServiceBusConnectionDoesntExist(string name)
    : Exception($"Service Bus connection with name {name} doesn't exist.");
