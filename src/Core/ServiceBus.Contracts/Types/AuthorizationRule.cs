namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record AuthorizationRule(
    string ClaimType,
    string ClaimValue,
    List<AccessRights> Rights,
    string KeyName,
    DateTimeOffset CreatedTime,
    DateTimeOffset ModifiedTime);