using System.Threading;
using System.Threading.Tasks;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.Website.Models;
namespace CrossBusExplorer.Website.ViewModels;

public interface ITopicViewModel
{
    event TopicAddedEventHandler? TopicAdded;
    event TopicRemovedEventHandler? TopicRemoved;
    TopicFormModel? Form { get; }
    TopicDetails? TopicDetails { get; }
    Task InitializeForm(
        string connectionName,
        string? topicName,
        CancellationToken cancellationToken);
    Task SaveTopicFormAsync(string connectionName);
    void NavigateToNewTopicForm(string connectionName);
    Task CloneTopic(
        string connectionName,
        string sourceTopicName,
        CancellationToken cancellationToken);
    Task DeleteTopic(string connectionName, string topicName, CancellationToken cancellationToken);
    Task UpdateTopicStatus(
        string connectionName,
        string topicName,
        QueueStatus active,
        CancellationToken cancellationToken);
    Task PurgeMessages(
        string connectionName,
        string topicName,
        CancellationToken cancellationToken);
    Task ResendDeadLetters(
        string connectionName,
        string topicName,
        CancellationToken cancellationToken);
}