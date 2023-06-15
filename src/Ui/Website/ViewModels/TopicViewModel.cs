using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.Website.Extensions;
using CrossBusExplorer.Website.Mappings;
using CrossBusExplorer.Website.Models;
using CrossBusExplorer.Website.Pages;
using CrossBusExplorer.Website.Shared;
using CrossBusExplorer.Website.Shared.Queue;
using Microsoft.AspNetCore.Components;
using MudBlazor;
namespace CrossBusExplorer.Website.ViewModels;

public class TopicViewModel : ITopicViewModel
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event TopicAddedEventHandler? TopicAdded;
    public event TopicRemovedEventHandler? TopicRemoved;

    private readonly ISnackbar _snackbar;
    private readonly NavigationManager _navigationManager;
    private readonly IDialogService _dialogService;
    private readonly IJobsViewModel _jobsViewModel;
    private readonly INavigationViewModel _navigationViewModel;
    private readonly ITopicService _topicService;

    public TopicViewModel(
        ITopicService topicService,
        ISnackbar snackbar,
        NavigationManager navigationManager,
        IDialogService dialogService,
        IJobsViewModel jobsViewModel)
    {
        _topicService = topicService;
        _snackbar = snackbar;
        _navigationManager = navigationManager;
        _dialogService = dialogService;
        _jobsViewModel = jobsViewModel;
    }

    private TopicFormModel? _form;
    public TopicFormModel? Form
    {
        get => _form;
        private set
        {
            _form = value;
            _form.PropertyChanged += (_, _) => this.Notify(PropertyChanged);
            this.Notify(PropertyChanged);
        }
    }
    public TopicDetails? TopicDetails { get; private set; }


    public async Task InitializeForm(
        string connectionName,
        string? topicName,
        CancellationToken cancellationToken)
    {
        if (topicName != null)
        {
            UpdateFormModel(
                await _topicService.GetAsync(connectionName, topicName, cancellationToken));
        }
        else
        {
            CreateFormModel();
        }
    }

    public async Task SaveTopicFormAsync(string connectionName)
    {
        if (Form != null)
        {
            try
            {
                var result = await SaveTopicAsync(connectionName, Form);

                HandleSaveResult(connectionName, result, Form.OperationType);
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error while updating topic: {ex.Message}", Severity.Error);
            }
        }
    }

    public void NavigateToNewTopicForm(string connectionName)
    {
        _navigationManager.NavigateTo($"new-topic/{connectionName}");
    }

    public async Task CloneTopic(
        string connectionName,
        string sourceTopicName,
        CancellationToken cancellationToken)
    {
        var parameters = new DialogParameters();

        parameters.Add(nameof(CloneDialog.ConnectionName), connectionName);
        parameters.Add(nameof(CloneDialog.SourceDialogName), sourceTopicName);

        var dialog = _dialogService.Show<CloneDialog>(
            $"Clone topic {sourceTopicName}",
            parameters,
            new DialogOptions
            {
                FullWidth = true,
                CloseOnEscapeKey = true
            });

        var dialogResult = await dialog.Result;

        if (!dialogResult.Cancelled && dialogResult.Data is string newTopicName)
        {
            var result = await _topicService.CloneAsync(
                connectionName,
                newTopicName,
                sourceTopicName,
                cancellationToken);

            HandleSaveResult(connectionName, result, OperationType.Create);
        }
    }

    public async Task DeleteTopic(
        string connectionName,
        string topicName,
        CancellationToken cancellationToken)
    {
        var parameters = new DialogParameters();
        parameters.Add(
            "ContentText",
            $"Are you sure you want to delete topic {topicName}?");

        var dialog = _dialogService.Show<ConfirmDialog>("Confirm", parameters);
        var dialogResult = await dialog.Result;

        if (dialogResult.Data is true)
        {
            var deleteResult =
                await _topicService.DeleteAsync(connectionName, topicName, cancellationToken);

            if (deleteResult.Success)
            {
                _snackbar.Add(
                    $"Topic {topicName} successfully deleted.",
                    Severity.Success);
                TopicRemoved(connectionName, topicName);
                NavigateToNewTopicForm(connectionName);
            }
            else
            {
                _snackbar.Add(
                    $"Topic {topicName} was not deleted. " +
                    $"Please check the topic name and try again later.",
                    Severity.Error);
            }
        }
    }

    public async Task UpdateTopicStatus(
        string connectionName,
        string topicName,
        ServiceBusEntityStatus status,
        CancellationToken cancellationToken)
    {
        OperationResult<TopicDetails> result = await _topicService.UpdateAsync(
            connectionName,
            new UpdateTopicOptions(topicName, Status: status),
            cancellationToken);

        HandleSaveResult(connectionName, result, OperationType.Update);
    }

    private void UpdateFormModel(TopicDetails resultData)
    {
        TopicDetails = resultData;
        Form = TopicDetails.ToFormModel(OperationType.Update);
    }

    private async Task<OperationResult<TopicDetails>> SaveTopicAsync(
        string connectionName,
        TopicFormModel form)
    {
        switch (form.OperationType)
        {
            case OperationType.Create:
                return await _topicService.CreateAsync(
                    connectionName,
                    Form.ToCreateOptions(),
                    default);
            case OperationType.Update:
                return await _topicService.UpdateAsync(
                    connectionName,
                    Form.ToUpdateOptions(),
                    default);
            default:
                return new OperationResult<TopicDetails>(false, null);
        }
    }

    private void HandleSaveResult(
        string connectionName,
        OperationResult<TopicDetails> result,
        OperationType operationType)
    {
        if (result.Success && result.Data != null)
        {
            if (operationType == OperationType.Create)
            {
                _navigationManager.NavigateTo(
                    $"topic/{connectionName}/{HttpUtility.UrlEncode(result.Data.Info.Name)}");

                TopicAdded(connectionName, result.Data.Info);
            }
            else
            {
                UpdateFormModel(result.Data);
            }

            _snackbar.Add("Saved successfully", Severity.Success);
        }
        else
        {
            _snackbar.Add(
                "Not saved. Please correct parameters and try again.",
                Severity.Error);
        }
    }

    private void CreateFormModel()
    {
        Form = new TopicFormModel(OperationType.Create)
        {
            MaxSizeInMegabytes = 1024,
            MaxMessageSizeInKilobytes = 256,
            EnableBatchedOperations = true,
            AutoDeleteOnIdle = TimeSpan.FromDays(365),
            DefaultMessageTimeToLive = TimeSpan.FromDays(365),
            DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(1),
        };
    }
}