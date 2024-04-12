using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using CrossBusExplorer.Website.Extensions;
namespace CrossBusExplorer.Website.Models;

public class MessageDetailsModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public MessageDetailsModel()
    {
        ApplicationProperties = new ObservableCollection<KeyValueTypePair>();
    }

    private string _body;

    public string Body
    {
        get => _body;
        set
        {
            _body = value;
            FormatBody();
            this.Notify(PropertyChanged);
        }
    }

    private void FormatBody()
    {
        try
        {
            var jsonNode = System.Text.Json.Nodes.JsonNode.Parse(_body);
            if (jsonNode is not null)
            {
                _body = jsonNode.ToString();
            }
        }
        catch (JsonException)
        {
            // body is not valid json
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

    private ObservableCollection<KeyValueTypePair> _applicationProperties;

    public ObservableCollection<KeyValueTypePair> ApplicationProperties
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
