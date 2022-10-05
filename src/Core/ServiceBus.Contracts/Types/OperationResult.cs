namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record OperationResult(bool Success);

public record OperationResult<TModel>(bool Success, TModel? Data);