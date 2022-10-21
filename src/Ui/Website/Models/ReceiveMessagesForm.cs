using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.Website.Extensions;
using MudBlazor;
namespace CrossBusExplorer.Website.Models;

public class ReceiveMessagesForm : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private SubQueue _subQueue;

    [Label("Queue type")]
    public SubQueue SubQueue
    {
        get => _subQueue;
        set
        {
            _subQueue = value;
            this.Notify(PropertyChanged);
        }
    }

    private ReceiveMode _mode;

    [Label("Receive type")]
    public ReceiveMode Mode
    {
        get => _mode;
        set
        {
            _mode = value;

            if (_mode == ReceiveMode.ReceiveAndDelete && _mode != value)
            {
                FromSequenceNumber = null;
            }

            this.Notify(PropertyChanged);
        }
    }

    private ReceiveType _type;

    [Label("Receive type")]
    public ReceiveType Type
    {
        get => _type;
        set
        {
            if (value == ReceiveType.All && value != _type)
            {
                MessagesCount = null;
            }
            else
            {
                MessagesCount = 10;
            }

            _type = value;

            this.Notify(PropertyChanged);
        }
    }

    private int? _messagesCount;

    public int? MessagesCount
    {
        get => _messagesCount;
        set
        {
            _messagesCount = value;
            this.Notify(PropertyChanged);
        }
    }

    private long? _fromSequenceNumber;

    public long? FromSequenceNumber
    {
        get => _fromSequenceNumber;
        set
        {
            _fromSequenceNumber = value;
            this.Notify(PropertyChanged);
        }
    }
}