using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.Website.Extensions;
using CrossBusExplorer.Website.Models;
using CrossBusExplorer.Website.Pages;
using CrossBusExplorer.Website.Shared;
using MudBlazor;
namespace CrossBusExplorer.Website.ViewModels;

public class RulesViewModel : IRulesViewModel
{
    private readonly IRuleService _ruleService;
    private readonly ISnackbar _snackbar;
    private readonly IDialogService _dialogService;
    public event PropertyChangedEventHandler? PropertyChanged;

    public RulesViewModel(
        IRuleService ruleService,
        ISnackbar snackbar,
        IDialogService dialogService)
    {
        _ruleService = ruleService;
        _snackbar = snackbar;
        _dialogService = dialogService;
    }

    private RuleFormModel? _form;
    public RuleFormModel? Form
    {
        get => _form;
        private set
        {
            _form = value;
            _form.PropertyChanged += (_, _) => this.Notify(PropertyChanged);
            this.Notify(PropertyChanged);
        }
    }

    private ObservableCollection<Rule> _rules;

    public ObservableCollection<Rule> Rules
    {
        get => _rules;
        private set
        {
            _rules = value;
            _rules.CollectionChanged += (_, _) => this.Notify(PropertyChanged);
        }
    }
    private bool _dialogVisible;
    public bool DialogVisible
    {
        get => _dialogVisible;
        set
        {
            _dialogVisible = value;
            this.Notify(PropertyChanged);
        }
    }

    public void ShowCreateRuleForm()
    {
        Form = new RuleFormModel(OperationType.Create)
        {
            Type = RuleType.TrueFilter
        };

        DialogVisible = true;
    }

    public async Task InitializeAsync(
        string connectionName,
        string topicName,
        string subscriptionName,
        CancellationToken cancellationToken)
    {
        try
        {
            Rules = new ObservableCollection<Rule>();

            await foreach (var rule
                in _ruleService.GetAsync(connectionName, topicName, subscriptionName, default))
            {
                Rules.Add(rule);
            }
        }
        catch (Exception ex)
        {
            _snackbar.Add($"Error while loading rules. Error: {ex.Message}", Severity.Error);
        }
    }

    public void ShowEditRuleForm(Rule rule)
    {
        Form = new RuleFormModel(OperationType.Update)
        {
            Name = rule.Name,
            Type = rule.Type,
            Value = rule.Value
        };

        DialogVisible = true;
    }

    public async Task DeleteRuleAsync(
        string connectionName,
        string topicName,
        string subscriptionName,
        Rule rule,
        CancellationToken cancellationToken)
    {
        var parameters = new DialogParameters();
        parameters.Add(
            "ContentText",
            $"Are you sure you want to delete rule {rule.Name} " +
            $"from {subscriptionName} subscription?");

        var dialog = _dialogService.Show<ConfirmDialog>("Confirm", parameters);
        var dialogResult = await dialog.Result;

        if (dialogResult.Data is true)
        {
            var deleteResult =
                await _ruleService.DeleteAsync(
                    connectionName,
                    topicName,
                    subscriptionName,
                    rule.Name,
                    cancellationToken);

            if (deleteResult.Success)
            {
                _snackbar.Add(
                    $"Rule {rule.Name} was successfully deleted.",
                    Severity.Success);

                Rules.Remove(rule);
            }
            else
            {
                _snackbar.Add(
                    $"Subscription {subscriptionName} was not deleted. " +
                    $"Please check the subscription name and try again later.",
                    Severity.Error);
            }
        }
    }

    public async Task OnSubmitFormAsync(
        string connectionName,
        string topicName,
        string subscriptionName,
        CancellationToken cancellationToken)
    {
        if (Form != null)
        {
            try
            {
                var result = await SaveRuleAsync(connectionName, topicName, subscriptionName, Form,
                    cancellationToken);

                HandleSaveResult(result);
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error while updating subscription: {ex.Message}", Severity.Error);
            }
        }
    }

    private void HandleSaveResult(OperationResult<Rule> result)
    {
        if (result is { Success: true, Data: { } })
        {
            Rules.AddOrReplace(
                p => p.Name.Equals(result.Data.Name,
                    StringComparison.InvariantCultureIgnoreCase), result.Data);

            _snackbar.Add("Saved successfully", Severity.Success);
            DialogVisible = false;
        }
        else
        {
            _snackbar.Add(
                "Not saved. Please correct parameters and try again.",
                Severity.Error);
        }
    }

    private async Task<OperationResult<Rule>> SaveRuleAsync(
        string connectionName,
        string topicName,
        string subscriptionName,
        RuleFormModel form,
        CancellationToken cancellationToken)
    {
        switch (form.OperationType)
        {
            case OperationType.Create:
                return await _ruleService.CreateAsync(
                    connectionName,
                    topicName,
                    subscriptionName,
                    form.Name,
                    form.Type,
                    form.Value,
                    cancellationToken);
            case OperationType.Update:
                return await _ruleService.UpdateAsync(
                    connectionName,
                    topicName,
                    subscriptionName,
                    form.Name,
                    form.Type,
                    form.Value,
                    cancellationToken);
            default:
                return new OperationResult<Rule>(false, null);
        }
    }
}