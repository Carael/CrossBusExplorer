using System.Collections.Generic;
namespace CrossBusExplorer.Website.Models;

public class UserSettings
{
    public bool IsDarkMode { get; set; }
    public List<FolderSetting> FolderSettings { get; set; }
}