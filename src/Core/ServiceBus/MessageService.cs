using System.Runtime.CompilerServices;
using Azure.Messaging.ServiceBus;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.ServiceBus.Mappings;
using SubQueue = CrossBusExplorer.ServiceBus.Contracts.Types.SubQueue;
namespace CrossBusExplorer.ServiceBus;

public class MessageService : IMessageService
{
    public async Task<IReadOnlyList<Message>> GetMessagesAsync(
        string connectionString,
        string queueName,
        int messagesCount,
        ReceiveMode receiveMode,
        long? fromSequenceNumber,
        CancellationToken cancellationToken)
    {
        await using ServiceBusClient client = new ServiceBusClient(connectionString);

        var receiver = client.CreateReceiver(queueName, new ServiceBusReceiverOptions
        {
            ReceiveMode = Enum.Parse<ServiceBusReceiveMode>(receiveMode.ToString())
        });

        IReadOnlyList<ServiceBusReceivedMessage>? result =
            await ReceiveMessagesAsync(
                receiver,
                messagesCount,
                fromSequenceNumber,
                cancellationToken);

        return result?.Select(p => p.MapToMessage()).ToList() ?? new List<Message>();
    }

    public async Task<Removed> PurgeAsync(
        string connectionString,
        string name,
        SubQueue subQueue,
        CancellationToken cancellationToken)
    {
        await using ServiceBusClient client = new ServiceBusClient(connectionString);
        var receiver = client.CreateReceiver(name, new ServiceBusReceiverOptions
        {
            SubQueue = (Azure.Messaging.ServiceBus.SubQueue)subQueue,
            ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
        });

        var totalRemoved = 0;
        var removedCount = 0;

        do
        {
            removedCount = (await receiver.ReceiveMessagesAsync(
                1024,
                TimeSpan.FromSeconds(15),
                cancellationToken)).Count;
            totalRemoved += removedCount;
            
        } while (removedCount > 0);

        return new Removed(totalRemoved);
    }

    private async Task<IReadOnlyList<ServiceBusReceivedMessage>?> ReceiveMessagesAsync(
        ServiceBusReceiver receiver,
        int maxMessages,
        long? fromSequenceNumber,
        CancellationToken cancellationToken)
    {
        if (receiver.ReceiveMode == ServiceBusReceiveMode.PeekLock)
        {
            return await receiver.PeekMessagesAsync(
                maxMessages,
                fromSequenceNumber,
                cancellationToken);
        }

        return await receiver.ReceiveMessagesAsync(
            maxMessages,
            TimeSpan.FromSeconds(5),
            cancellationToken);
    }
}