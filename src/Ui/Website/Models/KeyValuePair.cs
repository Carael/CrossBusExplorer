using System.ComponentModel;
using CrossBusExplorer.Website.Extensions;
namespace CrossBusExplorer.Website.Models;

public class KeyValuePair : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string _key;

    public string Key
    {
        get => _key;
        set
        {
            _key = value;
            this.Notify(PropertyChanged);
        }
    }

    public object _value;

    public object Value
    {
        get => _value;
        set
        {
            _value = value;
            this.Notify(PropertyChanged);
        }
    }
}
