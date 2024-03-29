using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.Website.Extensions;
using MudBlazor;
namespace CrossBusExplorer.Website.Jobs;

public class DeleteMessageJob : IJob
{
    private readonly string _connectionName;
    private readonly string _queueOrTopicName;
    private readonly string? _subscriptionName;
    private readonly SubQueue _subQueue;
    private readonly long _sequenceNumber;
    private readonly IMessageService _messageService;
    private readonly ISnackbar _snackbar;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public event PropertyChangedEventHandler? PropertyChanged;

    public event JobCompletedEventHandler? OnCompleted;

    public DeleteMessageJob(
        string connectionName,
        string queueOrTopicName,
        string? subscriptionName,
        SubQueue subQueue,
        long sequenceNumber,
        IMessageService messageService)
    {
        _connectionName = connectionName;
        _queueOrTopicName = queueOrTopicName;
        _subscriptionName = subscriptionName;
        _subQueue = subQueue;
        _sequenceNumber = sequenceNumber;

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
        $"Delete message {_sequenceNumber} from {_queueOrTopicName} {_subscriptionName}".Trim();

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
    public string? WarningMessage { get; private set; }

    public async Task ExecuteAsync()
    {
        Status = JobStatus.Running;

        try
        {
            Result result = await _messageService.DeleteMessage(
                _connectionName,
                _queueOrTopicName,
                _subscriptionName,
                _subQueue,
                _sequenceNumber,
                _cancellationTokenSource.Token);

            Progress = JobsHelper.GetProgress(1, result.Count);

            Status = JobStatus.Succeeded;

            if (result.Count == 0)
            {
                WarningMessage = "Message was not not deleted";
            }
        }
        catch (TaskCanceledException)
        {
            Status = JobStatus.Cancelled;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Job {Name} failed. Error: {ex.Message}.";
            Status = JobStatus.Failed;
        }

        await OnCompleted(_connectionName, _queueOrTopicName, _subscriptionName);
    }

    public void Cancel()
    {
        _cancellationTokenSource.Cancel();
    }
}
