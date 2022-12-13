using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Azure.Core.Pipeline;
using CrossBusExplorer.Website.Extensions;
namespace CrossBusExplorer.Website.Models;

public class ConnectionFolder : INotifyPropertyChanged
{
    public ConnectionFolder(string name)
    {
        _name = name;
        _menuItems = new ObservableCollection<ConnectionMenuItem>();
        _menuItems.CollectionChanged += (_, _) => this.Notify(PropertyChanged);
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    private string _name;
    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            this.Notify(PropertyChanged);
        }
    }

    private ObservableCollection<ConnectionMenuItem> _menuItems;
    
    public ObservableCollection<ConnectionMenuItem> MenuItems
    {
        get => _menuItems;
        private set
        {
            _menuItems = value;
            _menuItems.CollectionChanged += (_, _) =>
            {
                this.Notify(PropertyChanged);
            };
            this.Notify(PropertyChanged);
        }
    }
}