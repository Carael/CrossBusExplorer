using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CrossBusExplorer.Website.Extensions;
namespace CrossBusExplorer.Website.Models;

public class MessageDetailsModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public MessageDetailsModel()
    {
        ApplicationProperties = new ObservableCollection<KeyValuePair>();
    }

    private string _body;

    public string Body
    {
        get => _body;
        set
        {
            _body = value;
            this.Notify(PropertyChanged);
        }
    }

    private string? _subject;

    public string? Subject
    {
        get => _subject;
        set
        {
            _subject = value;
            this.Notify(PropertyChanged);
        }
    }

    private string? _to;

    public string? To
    {
        get => _to;
        set
        {
            _to = value;
            this.Notify(PropertyChanged);
        }
    }

    private string? _contentType;

    public string? ContentType
    {
        get => _contentType;
        set
        {
            _contentType = value;
            this.Notify(PropertyChanged);
        }
    }

    private string? _correlationId;

    public string? CorrelationId
    {
        get => _correlationId;
        set
        {
            _correlationId = value;
            this.Notify(PropertyChanged);
        }
    }

    private string? _id;

    public string? Id
    {
        get => _id;
        set
        {
            _id = value;
            this.Notify(PropertyChanged);
        }
    }

    private string? _partitionKey;

    public string? PartitionKey
    {
        get => _partitionKey;
        set
        {
            _partitionKey = value;
            this.Notify(PropertyChanged);
        }
    }

    private string? _replyTo;

    public string? ReplyTo
    {
        get => _replyTo;
        set
        {
            _replyTo = value;
            this.Notify(PropertyChanged);
        }
    }

    private string? _sessionId;

    public string? SessionId
    {
        get => _sessionId;
        set
        {
            _sessionId = value;
            this.Notify(PropertyChanged);
        }
    }

    private DateTimeOffset? _scheduledEnqueueTime;

    public DateTimeOffset? ScheduledEnqueueTime
    {
        get => _scheduledEnqueueTime;
        set
        {
            _scheduledEnqueueTime = value;
            this.Notify(PropertyChanged);
        }
    }

    private TimeSpan? _timeToLive;

    public TimeSpan? TimeToLive
    {
        get => _timeToLive;
        set
        {
            _timeToLive = value;
            this.Notify(PropertyChanged);
        }
    }

    private ObservableCollection<KeyValuePair> _applicationProperties;

    public ObservableCollection<KeyValuePair> ApplicationProperties
    {
        get => _applicationProperties;
        set
        {
            _applicationProperties = value;
            _applicationProperties.CollectionChanged += (_, _) => this.Notify(PropertyChanged);
            this.Notify(PropertyChanged);
        }
    }
}