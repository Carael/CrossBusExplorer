using System;
namespace CrossBusExplorer.Website.Exceptions;

public class FolderSettingsForConnectionNotFoundException : Exception
{
    public FolderSettingsForConnectionNotFoundException(string connectionName) 
        : base($"Folder settings for {connectionName} was not found.")
    {
        
    }
}