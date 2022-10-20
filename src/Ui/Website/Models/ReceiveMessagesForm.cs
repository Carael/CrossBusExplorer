using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.Website.Extensions;
using MudBlazor;
namespace CrossBusExplorer.Website.Models;

public class ReceiveMessagesForm : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private ReceiveMode _mode;

    [Label("Receive type")]
    public ReceiveMode Mode
    {
        get => _mode;
        set
        {
            _mode = value;
            this.Notify(PropertyChanged);
        }
    }

    private ReceiveMessagesType _type;

    [Label("Receive type")]
    public ReceiveMessagesType Type
    {
        get => _type;
        set
        {
            _type = value;
            this.Notify(PropertyChanged);
        }
    }

    private int? _messagesCount;

    [Label("Messages count")]
    public int? MessagesCount
    {
        get => _messagesCount;
        set
        {
            _messagesCount = value;
            this.Notify(PropertyChanged);
        }
    }

    private int? _fromSequenceNumber;

    [Label("From sequence number")]
    public int? FromSequenceNumber
    {
        get => _fromSequenceNumber;
        set
        {
            _fromSequenceNumber = value;
            this.Notify(PropertyChanged);
        }
    }
}