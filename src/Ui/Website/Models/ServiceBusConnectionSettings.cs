namespace CrossBusExplorer.Website.Models;

public class ServiceBusConnectionSettings
{
    public ServiceBusConnectionSettings(string name, int index)
    {
        Name = name;
        Index = index;
    }
    
    public string Name { get; }
    public int Index { get; private set; }
    
    public void UpdateIndex(int newIndex)
    {
        Index = newIndex;
    }
}