using System.ComponentModel;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.Website.Extensions;
namespace CrossBusExplorer.Website.Models;

public class KeyValueTypePair : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _key;

    public string Key
    {
        get => _key;
        set
        {
            _key = value;
            this.Notify(PropertyChanged);
        }
    }

    private string _value;

    public string Value
    {
        get => _value;
        set
        {
            _value = value;
            this.Notify(PropertyChanged);
        }
    }

    private ApplicationPropertyType _type;

    public ApplicationPropertyType Type
    {
        get => _type;
        set
        {
            _type = value;
            this.Notify(PropertyChanged);
        }
    }
}
