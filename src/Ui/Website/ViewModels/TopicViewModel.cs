using System.Threading;
using System.Threading.Tasks;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.Website.Models;
namespace CrossBusExplorer.Website.ViewModels;

public class TopicViewModel : ITopicViewModel
{

    public event TopicAddedEventHandler? TopicAdded;
    public event TopicRemovedEventHandler? TopicRemoved;
    public TopicFormModel? Form { get; }
    public TopicDetails? TopicDetails { get; }
    
    public Task InitializeForm(string connectionName, string? topicName, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    
    public Task SaveTopicFormAsync(string connectionName)
    {
        throw new System.NotImplementedException();
    }
    
    public void NavigateToNewTopicForm(string connectionName)
    {
        throw new System.NotImplementedException();
    }
    
    public Task CloneTopic(
        string connectionName, 
        string sourceTopicName, 
        CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    
    public Task DeleteTopic(
        string connectionName, 
        string topicName, 
        CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    
    public Task UpdateTopicStatus(
        string connectionName, 
        string topicName, 
        QueueStatus active,
        CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    
    public Task PurgeMessages(
        string connectionName, 
        string topicName, 
        CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    
    public Task ResendDeadLetters(
        string connectionName, 
        string topicName,
        CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}