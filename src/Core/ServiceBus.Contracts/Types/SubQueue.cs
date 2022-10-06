namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public enum SubQueue
{
    None = 0,
    DeadLetter = 1,
    TransferDeadLetter = 2
}