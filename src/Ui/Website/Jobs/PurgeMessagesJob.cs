using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.Website.Extensions;
using MudBlazor;
namespace CrossBusExplorer.Website.Jobs;

public class PurgeMessagesJob : IJob
{
    private readonly string _connectionName;
    private readonly string _queueOrTopicName;
    private readonly string? _subscriptionName;
    private readonly SubQueue _subQueue;
    private readonly long _totalCount;
    private readonly IMessageService _messageService;
    private readonly ISnackbar _snackbar;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public event PropertyChangedEventHandler? PropertyChanged;

    public PurgeMessagesJob(
        string connectionName,
        string queueOrTopicName,
        string? subscriptionName,
        SubQueue subQueue,
        long totalCount,
        IMessageService messageService)
    {
        _connectionName = connectionName;
        _queueOrTopicName = queueOrTopicName;
        _subscriptionName = subscriptionName;
        _subQueue = subQueue;
        _totalCount = totalCount;
        _messageService = messageService;
        _cancellationTokenSource = new CancellationTokenSource();
    }

    private int _progress;
    public int Progress
    {
        get => _progress;
        private set
        {
            _progress = value;
            this.Notify(PropertyChanged);
        }
    }

    public string Name =>
        $"Purge messages from {_queueOrTopicName} {_subscriptionName}".Trim();

    public bool ViewDetails { get; set; }

    private JobStatus _status;
    public JobStatus Status
    {
        get => _status;
        private set
        {
            _status = value;
            this.Notify(PropertyChanged);
        }
    }
    public string? ErrorMessage { get; private set; }

    public async Task ExecuteAsync()
    {
        Status = JobStatus.Running;
        
        try
        {
            await foreach (var result in _messageService.PurgeAsync(
                _connectionName,
                _queueOrTopicName,
                _subscriptionName,
                _subQueue,
                _cancellationTokenSource.Token))
            {
                Progress = JobsHelper.GetProgress(_totalCount, result.PurgedCount);
            }

            Status = JobStatus.Succeeded;
        }
        catch (TaskCanceledException)
        {
            Status = JobStatus.Cancelled;
        }
        catch (Exception ex)
        {
            Cancel();
            
            ErrorMessage = $"Job {Name} failed. Error: {ex.Message}.";
            Status = JobStatus.Failed;
        }
    }

    public void Cancel()
    {
        Status = JobStatus.Cancelled;
        _cancellationTokenSource.Cancel();
    }
}