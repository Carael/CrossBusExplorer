using System.ComponentModel;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.Website.Extensions;
using CrossBusExplorer.Website.Pages;
using MudBlazor;
namespace CrossBusExplorer.Website.Models;

public class RuleFormModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public RuleFormModel(OperationType operationType)
    {
        OperationType = operationType;
    }
    
    public OperationType OperationType { get; }
    
    private string? _name;
    
    public string? Name
    {
        get => _name;
        set
        {
            _name = value;
            this.Notify(PropertyChanged);
        }
    }

    private RuleType _type;
    
    public RuleType Type
    {
        get => _type;
        set
        {
            _type = value;
            this.Notify(PropertyChanged);
        }
    }
    
    private string? _value;
    
    public string? Value
    {
        get => _value;
        set
        {
            _value = value;
            this.Notify(PropertyChanged);
        }
    }
}