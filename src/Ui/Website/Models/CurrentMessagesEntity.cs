using System;
namespace CrossBusExplorer.Website.Models;

public record CurrentMessagesEntity(
    string ConnectionName,
    string QueueOrTopicName,
    string SubscriptionName)
{
    public virtual bool Equals(CurrentMessagesEntity? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return string.Equals(ConnectionName, other.ConnectionName,
                   StringComparison.InvariantCultureIgnoreCase) &&
               string.Equals(QueueOrTopicName, other.QueueOrTopicName,
                   StringComparison.InvariantCultureIgnoreCase) && string.Equals(SubscriptionName,
                   other.SubscriptionName, StringComparison.InvariantCultureIgnoreCase);
    }
    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(ConnectionName, StringComparer.InvariantCultureIgnoreCase);
        hashCode.Add(QueueOrTopicName, StringComparer.InvariantCultureIgnoreCase);
        hashCode.Add(SubscriptionName, StringComparer.InvariantCultureIgnoreCase);
        return hashCode.ToHashCode();
    }
}