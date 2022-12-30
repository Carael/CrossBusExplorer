namespace CrossBusExplorer.Website.Models;

public class FolderSetting
{
    public FolderSetting(string name, int index)
    {
        Name = name;
        Index = index;
    }
    public string Name { get; }
    public int Index { get; }
}