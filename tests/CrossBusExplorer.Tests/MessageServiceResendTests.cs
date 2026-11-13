using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using CrossBusExplorer.Management.Contracts;
using CrossBusExplorer.ServiceBus;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using SubQueue = CrossBusExplorer.ServiceBus.Contracts.Types.SubQueue;

namespace CrossBusExplorer.Tests;

public sealed class MessageServiceResendTests
{
    [Fact]
    public async Task ResendAsync_UsesPeekLock_SendsBatchAndCompletesMessages()
    {
        var messages = new[]
        {
            CreateReceivedMessage("msg-1", 1),
            CreateReceivedMessage("msg-2", 2),
            CreateReceivedMessage("msg-3", 3)
        };

        var receiver = new FakeServiceBusReceiver([messages]);
        var sender = new FakeServiceBusSender();
        var client = new FakeServiceBusClient(receiver, sender);
        var service = CreateMessageService(client);

        var results = new List<ResendResult>();
        await foreach (var result in service.ResendAsync(
                           "conn-1",
                           "orders",
                           null,
                           SubQueue.DeadLetter,
                           "orders",
                           messages.Length,
                           CancellationToken.None))
        {
            results.Add(result);
        }

        Assert.Equal(ServiceBusReceiveMode.PeekLock, client.LastReceiverOptions?.ReceiveMode);
        Assert.Equal(Azure.Messaging.ServiceBus.SubQueue.DeadLetter, client.LastReceiverOptions?.SubQueue);
        Assert.Equal(3, results[^1].ResendCount);
        Assert.Equal(["msg-1", "msg-2", "msg-3"], sender.SentMessageIds);
        Assert.Equal(
            [1L, 2L, 3L],
            receiver.CompletedMessages.Select(m => m.SequenceNumber).OrderBy(s => s));
        Assert.Empty(receiver.AbandonedMessages);
    }

    [Fact]
    public async Task ResendAsync_WhenSendThrows_AbandonsLockedMessagesWithoutDataLoss()
    {
        var messages = new[]
        {
            CreateReceivedMessage("msg-1", 1),
            CreateReceivedMessage("msg-2", 2)
        };

        var receiver = new FakeServiceBusReceiver([messages]);
        var sender = new FakeServiceBusSender
        {
            OnSendBatchAsync = (_, _) => throw new ServiceBusException(
                "Destination unavailable",
                ServiceBusFailureReason.ServiceBusy)
        };
        var client = new FakeServiceBusClient(receiver, sender);
        var service = CreateMessageService(client);

        await Assert.ThrowsAsync<ServiceBusOperationException>(async () =>
        {
            await foreach (var _ in service.ResendAsync(
                               "conn-1",
                               "orders",
                               null,
                               SubQueue.DeadLetter,
                               "orders",
                               messages.Length,
                               CancellationToken.None))
            {
            }
        });

        Assert.Empty(receiver.CompletedMessages);
        Assert.Equal(
            [1L, 2L],
            receiver.AbandonedMessages.Select(m => m.SequenceNumber).OrderBy(s => s));
    }

    [Fact]
    public async Task ResendAsync_WhenCancelledDuringSend_AbandonsLockedMessages()
    {
        using var cts = new CancellationTokenSource();
        var messages = new[]
        {
            CreateReceivedMessage("msg-1", 1),
            CreateReceivedMessage("msg-2", 2)
        };

        var receiver = new FakeServiceBusReceiver([messages]);
        var sender = new FakeServiceBusSender
        {
            OnSendBatchAsync = (_, ct) =>
            {
                cts.Cancel();
                ct.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            }
        };
        var client = new FakeServiceBusClient(receiver, sender);
        var service = CreateMessageService(client);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in service.ResendAsync(
                               "conn-1",
                               "orders",
                               null,
                               SubQueue.DeadLetter,
                               "orders",
                               messages.Length,
                               cts.Token))
            {
            }
        });

        Assert.Empty(receiver.CompletedMessages);
        Assert.Equal(
            [1L, 2L],
            receiver.AbandonedMessages.Select(m => m.SequenceNumber).OrderBy(s => s));
    }

    [Fact]
    public async Task ResendAsync_WhenSubBatchSplitsAndSecondSendFails_CompletesFirstSubBatchAndAbandonsRemaining()
    {
        var messages = new[]
        {
            CreateReceivedMessage("msg-1", 1),
            CreateReceivedMessage("msg-2", 2),
            CreateReceivedMessage("msg-3", 3)
        };

        var receiver = new FakeServiceBusReceiver([messages]);
        var sendAttempts = 0;
        var sender = new FakeServiceBusSender(maxMessagesPerBatch: 2)
        {
            OnSendBatchAsync = (_, _) =>
            {
                sendAttempts++;
                if (sendAttempts == 2)
                {
                    throw new ServiceBusException(
                        "Send failed on second sub-batch",
                        ServiceBusFailureReason.ServiceCommunicationProblem);
                }

                return Task.CompletedTask;
            }
        };
        var client = new FakeServiceBusClient(receiver, sender);
        var service = CreateMessageService(client);

        await Assert.ThrowsAsync<ServiceBusOperationException>(async () =>
        {
            await foreach (var _ in service.ResendAsync(
                               "conn-1",
                               "orders",
                               null,
                               SubQueue.DeadLetter,
                               "orders",
                               messages.Length,
                               CancellationToken.None))
            {
            }
        });

        Assert.Equal(
            [1L, 2L],
            receiver.CompletedMessages.Select(m => m.SequenceNumber).OrderBy(s => s));
        Assert.Equal(
            [3L],
            receiver.AbandonedMessages.Select(m => m.SequenceNumber).OrderBy(s => s));
    }

    private static MessageService CreateMessageService(ServiceBusClient client) =>
        new(new FakeConnectionManagement(), new FakeClientFactory(client));

    private static ServiceBusReceivedMessage CreateReceivedMessage(string id, long sequenceNumber) =>
        ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString($"body-{id}"),
            messageId: id,
            sequenceNumber: sequenceNumber);

    private sealed class FakeConnectionManagement : IConnectionManagement
    {
        private static readonly ServiceBusConnection Connection =
            ServiceBusConnection.CreatePasswordless(
                "test",
                "test.servicebus.windows.net",
                ServiceBusAuthenticationType.AzureCli,
                id: "conn-1");

        public Task<IList<ServiceBusConnection>> GetAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IList<ServiceBusConnection>>([Connection]);

        public Task<ServiceBusConnection> GetAsync(string name, CancellationToken cancellationToken) =>
            Task.FromResult(Connection);

        public Task<ServiceBusConnection> SaveAsync(
            string name,
            string connectionString,
            string folder,
            CrossBusExplorer.Management.Contracts.ServiceBusTransportType transportType,
            CancellationToken cancellationToken) =>
            Task.FromResult(Connection);

        public Task<ServiceBusConnection> SaveAsync(
            ServiceBusConnection connection,
            CancellationToken cancellationToken) =>
            Task.FromResult(Connection);

        public Task DeleteAsync(string name, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FakeClientFactory(ServiceBusClient client) : IServiceBusClientFactory
    {
        public ServiceBusClient GetClient(ServiceBusConnection connection) => client;

        public ServiceBusAdministrationClient GetAdministrationClient(ServiceBusConnection connection) =>
            throw new NotSupportedException();
    }

    private sealed class FakeServiceBusClient(
        ServiceBusReceiver receiver,
        ServiceBusSender sender) : ServiceBusClient
    {
        public ServiceBusReceiverOptions? LastReceiverOptions { get; private set; }

        public override ServiceBusReceiver CreateReceiver(
            string queueName,
            ServiceBusReceiverOptions options)
        {
            LastReceiverOptions = options;
            return receiver;
        }

        public override ServiceBusReceiver CreateReceiver(
            string topicName,
            string subscriptionName,
            ServiceBusReceiverOptions options)
        {
            LastReceiverOptions = options;
            return receiver;
        }

        public override ServiceBusSender CreateSender(string queueOrTopicName) => sender;
    }

    private sealed class FakeServiceBusReceiver(
        IEnumerable<IReadOnlyList<ServiceBusReceivedMessage>> batches) : ServiceBusReceiver
    {
        private readonly Queue<IReadOnlyList<ServiceBusReceivedMessage>> _batches = new(batches);

        public ConcurrentBag<ServiceBusReceivedMessage> CompletedMessages { get; } = [];
        public ConcurrentBag<ServiceBusReceivedMessage> AbandonedMessages { get; } = [];

        public override Task<IReadOnlyList<ServiceBusReceivedMessage>> ReceiveMessagesAsync(
            int maxMessages,
            TimeSpan? maxWaitTime = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_batches.TryDequeue(out var batch))
            {
                return Task.FromResult(batch);
            }

            return Task.FromResult<IReadOnlyList<ServiceBusReceivedMessage>>([]);
        }

        public override Task CompleteMessageAsync(
            ServiceBusReceivedMessage message,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CompletedMessages.Add(message);
            return Task.CompletedTask;
        }

        public override Task AbandonMessageAsync(
            ServiceBusReceivedMessage message,
            IDictionary<string, object>? propertiesToModify = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AbandonedMessages.Add(message);
            return Task.CompletedTask;
        }

        public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakeServiceBusSender(int maxMessagesPerBatch = 100) : ServiceBusSender
    {
        private readonly Dictionary<ServiceBusMessageBatch, List<ServiceBusMessage>> _batchItems = [];

        public List<string> SentMessageIds { get; } = [];
        public Func<ServiceBusMessageBatch, CancellationToken, Task>? OnSendBatchAsync { get; init; }

        public override ValueTask<ServiceBusMessageBatch> CreateMessageBatchAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var items = new List<ServiceBusMessage>();
            var batch = ServiceBusModelFactory.ServiceBusMessageBatch(
                batchSizeBytes: 1024 * 1024,
                batchMessageStore: items,
                tryAddCallback: _ => items.Count < maxMessagesPerBatch);
            _batchItems[batch] = items;
            return ValueTask.FromResult(batch);
        }

        public override async Task SendMessagesAsync(
            ServiceBusMessageBatch messageBatch,
            CancellationToken cancellationToken = default)
        {
            if (OnSendBatchAsync is not null)
            {
                await OnSendBatchAsync(messageBatch, cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (_batchItems.TryGetValue(messageBatch, out var items))
            {
                SentMessageIds.AddRange(items.Select(m => m.MessageId));
            }
        }

        public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
