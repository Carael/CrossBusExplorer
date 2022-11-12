namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public enum ServiceBusEntityStatus
{
    Active,
    Disabled,
    SendDisabled,       //TODO: support in ui
    ReceiveDisabled     //TODO: support in ui
}