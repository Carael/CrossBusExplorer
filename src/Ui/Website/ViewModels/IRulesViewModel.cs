using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.Website.Models;
using MudBlazor;
namespace CrossBusExplorer.Website.ViewModels;

public interface IRulesViewModel : INotifyPropertyChanged
{
    RuleFormModel? Form { get; }
    ObservableCollection<Rule> Rules { get; }
    
    bool DialogVisible { get; set; }
    
    void ShowCreateRuleForm();
    
    Task InitializeAsync(
        string connectionName,
        string topicName,
        string subscriptionName,
        CancellationToken cancellationToken);
    
    void ShowEditRuleForm(Rule rule);

    Task DeleteRuleAsync(
        string connectionName,
        string topicName,
        string subscriptionName,
        Rule rule,
        CancellationToken cancellationToken);
    Task OnSubmitFormAsync(
        string connectionName,
        string topicName,
        string subscriptionName,
        CancellationToken cancellationToken);
}