namespace CrossBusExplorer.ServiceBus.Contracts.Types;

public record SharedAccessAuthorizationRule(
    string KeyName, 
    string PrimaryKey, 
    string SecondaryKey, 
    IEnumerable<AccessRights> Rights);