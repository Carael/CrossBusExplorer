using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CrossBusExplorer.Website.Extensions;
using CrossBusExplorer.Website.Pages;
using MudBlazor;
namespace CrossBusExplorer.Website.Models;

public class SubscriptionFormModel: INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public SubscriptionFormModel(OperationType operationType)
    {
        OperationType = operationType;
    }
    
    public OperationType OperationType { get; }

    private string? _topicName;
    
    [Label("Topic name")]
    [Required(ErrorMessage = "Topic name is required")]
    public string? TopicName
    {
        get => _topicName;
        set
        {
            _topicName = value;
            this.Notify(PropertyChanged);
        }
    }
    
    private string? _subscriptionName;
    
    [Label("Name")]
    [Required(ErrorMessage = "Name is required")]
    public string? SubscriptionName
    {
        get => _subscriptionName;
        set
        {
            _subscriptionName = value;
            this.Notify(PropertyChanged);
        }
    }

    private string? _userMetadata;
    
    [Label("User metadata")]
    public string? UserMetadata
    {
        get => _userMetadata;
        set
        {
            _userMetadata = value;
            this.Notify(PropertyChanged);
        }
    }

    private TimeSpan? _autoDeleteOnIdle;
    
    [Label("Auto delete queue on idle")]
    [Required(ErrorMessage = "Field is required. Format: DDDD.HH:MM:SS.")]
    public TimeSpan? AutoDeleteOnIdle
    {
        get => _autoDeleteOnIdle;
        set
        {
            _autoDeleteOnIdle = value;
            this.Notify(PropertyChanged);
        }
    }
    
    private TimeSpan? _defaultMessageTimeToLive;
    
    [Label("Default message time to live")]
    [Required(ErrorMessage = "Field is required. Format: DDDD.HH:MM:SS.")]
    public TimeSpan? DefaultMessageTimeToLive
    {
        get => _defaultMessageTimeToLive;
        set
        {
            _defaultMessageTimeToLive = value;
            this.Notify(PropertyChanged);
        }
    }
    
    private bool? _supportOrdering;
    
    [Label("Support ordering")]
    public bool? SupportOrdering
    {
        get => _supportOrdering;
        set
        {
            _supportOrdering = value;
            this.Notify(PropertyChanged);
        }
    }
    
    private bool? _enableBatchedOperations;
    
    [Label("Enable batch operations")]
    public bool? EnableBatchedOperations
    {
        get => _enableBatchedOperations;
        set
        {
            _enableBatchedOperations = value;
            this.Notify(PropertyChanged);
        }
    }

    private int? _maxDeliveryCount;
    
    [Label("Max delivery count")]
    [Required]
    public int? MaxDeliveryCount
    {
        get => _maxDeliveryCount;
        set
        {
            _maxDeliveryCount = value;
            this.Notify(PropertyChanged);
        }
    }
    
    
    private string? _forwardTo;
    
    [Label("Forward to (queue or topic)")]
    public string? ForwardTo
    {
        get => _forwardTo;
        set
        {
            _forwardTo = value;
            this.Notify(PropertyChanged);
        }
    }
    
    private string? _forwardDeadLetteredMessagesTo;
    
    [Label("Forward deadletter messages to")]
    public string? ForwardDeadLetteredMessagesTo
    {
        get => _forwardDeadLetteredMessagesTo;
        set
        {
            _forwardDeadLetteredMessagesTo = value;
            this.Notify(PropertyChanged);
        }
    }
    
    private TimeSpan? _lockDuration;
    
    [Label("Lock duration")]
    [Required(ErrorMessage = "Field is required. Format: DDDD.HH:MM:SS.")]
    public TimeSpan? LockDuration
    {
        get => _lockDuration;
        set
        {
            _lockDuration = value;
            this.Notify(PropertyChanged);
        }
    }
    
    private bool? _requiresSession;
    
    [Label("Require session")]
    public bool? RequiresSession
    {
        get => _requiresSession;
        set
        {
            _requiresSession = value;
            this.Notify(PropertyChanged);
        }
    }
    
    private bool? _deadLetteringOnMessageExpiration;
    
    [Label("Require session")]
    public bool? DeadLetteringOnMessageExpiration
    {
        get => _deadLetteringOnMessageExpiration;
        set
        {
            _deadLetteringOnMessageExpiration = value;
            this.Notify(PropertyChanged);
        }
    }
    
    private bool? _enableDeadLetteringOnFilterEvaluationExceptions;
    
    [Label("Enable partitioning")]
    public bool? EnableDeadLetteringOnFilterEvaluationExceptions
    {
        get => _enableDeadLetteringOnFilterEvaluationExceptions;
        set
        {
            _enableDeadLetteringOnFilterEvaluationExceptions = value;
            this.Notify(PropertyChanged);
        }
    }
}