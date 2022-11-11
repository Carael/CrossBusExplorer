using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CrossBusExplorer.Website.Extensions;
using CrossBusExplorer.Website.Pages;
using MudBlazor;
namespace CrossBusExplorer.Website.Models;

public class TopicFormModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public TopicFormModel(OperationType operationType)
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
    [Required(ErrorMessage = "Field is required")]
    public long? MaxSizeInMegabytes
    {
        get => _maxSizeInMegabytes;
        set
        {
            _maxSizeInMegabytes = value;
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

    private TimeSpan? _duplicateDetectionHistoryTimeWindow;
    
    [Label("Duplicate detection history time window")]
    [Required(ErrorMessage = "Field is required. Format: DDDD.HH:MM:SS.")]
    public TimeSpan? DuplicateDetectionHistoryTimeWindow
    {
        get => _duplicateDetectionHistoryTimeWindow;
        set
        {
            _duplicateDetectionHistoryTimeWindow = value;
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