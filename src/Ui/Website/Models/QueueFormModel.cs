using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CrossBusExplorer.Website.Extensions;
using CrossBusExplorer.Website.Pages;
using MudBlazor;
namespace CrossBusExplorer.Website.Models;

public class QueueFormModel : INotifyPropertyChanged
{

    public event PropertyChangedEventHandler? PropertyChanged;

    public QueueFormModel(OperationType operationType)
    {
        OperationType = operationType;
    }
    
    public OperationType OperationType { get; }

    private string? _name;
    
    [Label("Name")]
    [Required(ErrorMessage = "Name is required")]
    public string? Name
    {
        get => _name;
        set
        {
            _name = value;
            this.Notify(PropertyChanged);
        }
    }

    private long? _maxSizeInMegabytes;
    
    [Label("Max size in MB")]
    [Required]
    public long? MaxSizeInMegabytes
    {
        get => _maxSizeInMegabytes;
        set
        {
            _maxSizeInMegabytes = value;
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
    
    private string? _duplicateDetectionHistoryTimeWindow;
    
    [Label("Duplicate detection history time window")]
    [Required(ErrorMessage = "Name is required")]
    public string? DuplicateDetectionHistoryTimeWindow
    {
        get => _duplicateDetectionHistoryTimeWindow;
        set
        {
            _duplicateDetectionHistoryTimeWindow = value;
            this.Notify(PropertyChanged);
        }
    }
    
    private string? _autoDeleteOnIdle;
    
    [Label("Auto delete queue on idle")]
    [Required(ErrorMessage = "Name is required")]
    public string? AutoDeleteOnIdle
    {
        get => _autoDeleteOnIdle;
        set
        {
            _autoDeleteOnIdle = value;
            this.Notify(PropertyChanged);
        }
    }
    
    private string? _defaultMessageTimeToLive;
    
    [Label("Default message time to live")]
    [Required(ErrorMessage = "Name is required")]
    public string? DefaultMessageTimeToLive
    {
        get => _defaultMessageTimeToLive;
        set
        {
            _defaultMessageTimeToLive = value;
            this.Notify(PropertyChanged);
        }
    }

    private string? _lockDuration;
    
    [Label("Duplicate detection history time window")]
    [Required(ErrorMessage = "Name is required")]
    public string? LockDuration
    {
        get => _lockDuration;
        set
        {
            _lockDuration = value;
            this.Notify(PropertyChanged);
        }
    }
    
    private bool? _deadLetteringOnMessageExpiration;
    
    [Label("Duplicate detection history time window")]
    public bool? DeadLetteringOnMessageExpiration
    {
        get => _deadLetteringOnMessageExpiration;
        set
        {
            _deadLetteringOnMessageExpiration = value;
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
    
    private bool? _requiresDuplicateDetection;
    
    [Label("Requires duplicate detection")]
    public bool? RequiresDuplicateDetection
    {
        get => _requiresDuplicateDetection;
        set
        {
            _requiresDuplicateDetection = value;
            this.Notify(PropertyChanged);
        }
    }
    
    private bool? _enablePartitioning;
    
    [Label("Enable partitioning")]
    public bool? EnablePartitioning
    {
        get => _enablePartitioning;
        set
        {
            _enablePartitioning = value;
            this.Notify(PropertyChanged);
        }
    }
    
    private long? _maxMessageSizeInKilobytes;
    
    [Label("Max message size in KB")]
    public long? MaxMessageSizeInKilobytes
    {
        get => _maxMessageSizeInKilobytes;
        set
        {
            _maxMessageSizeInKilobytes = value;
            this.Notify(PropertyChanged);
        }
    }
}