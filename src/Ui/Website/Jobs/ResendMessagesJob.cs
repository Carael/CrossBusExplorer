using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.Website.Extensions;
namespace CrossBusExplorer.Website.Jobs;

public class ResendMessagesJob : IJob
{
    private readonly string _connectionName;
    private readonly string _queueOrTopicName;
    private readonly string? _subscriptionName;
    private readonly SubQueue _subQueue;
    private readonly string _destinationTopicOrQueueName;
    private readonly long _totalCount;
    private readonly IMessageService _messageService;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ResendMessagesJob(
        string connectionName,
        string queueOrTopicName,
        string? subscriptionName,
        SubQueue subQueue,
        string destinationTopicOrQueueName,
        long totalCount,
        IMessageService messageService)
    {
        _connectionName = connectionName;
        _queueOrTopicName = queueOrTopicName;
        _subscriptionName = subscriptionName;
        _subQueue = subQueue;
        _destinationTopicOrQueueName = destinationTopicOrQueueName;
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
    public async Task StartAsync()
    {
        await foreach (var result in _messageService.ResendAsync(
            _connectionName,
            _queueOrTopicName,
            _subscriptionName,
            _subQueue,
            _destinationTopicOrQueueName,
            _cancellationTokenSource.Token))
        {
            Progress = JobsHelper.GetProgress(_totalCount, result.ResendCount);
        }

        Progress = WellKnown.ProgressCompleted;
    }

    public void Cancel()
    {
        _cancellationTokenSource.Cancel();
    }
    
    public string Name =>
        $"Resend messages from {_queueOrTopicName} {_subscriptionName} " +
        $"to {_destinationTopicOrQueueName}.".Trim();
}