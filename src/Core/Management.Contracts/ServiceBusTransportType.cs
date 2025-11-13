namespace CrossBusExplorer.Management.Contracts;

public enum ServiceBusTransportType
{
    /// <summary>
    /// AMQP over TCP transport
    /// </summary>
    AmqpTcp = 0,
    
    /// <summary>
    /// AMQP over WebSockets transport (recommended for restrictive network environments)
    /// </summary>
    AmqpWebSockets = 1
}
