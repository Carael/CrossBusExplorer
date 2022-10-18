using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.Website.Extensions;
using CrossBusExplorer.Website.Mappings;
using CrossBusExplorer.Website.Models;
using CrossBusExplorer.Website.Pages;
using CrossBusExplorer.Website.Shared;
using Microsoft.AspNetCore.Components;
using MudBlazor;
namespace CrossBusExplorer.Website.ViewModels;

public class QueueViewModel : IQueueViewModel
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event QueueAddedEventHandler? QueueAdded;
    public event QueueRemovedEventHandler? QueueRemoved;
    
    private readonly ISnackbar _snackbar;
    private readonly NavigationManager _navigationManager;
    private readonly IDialogService _dialogService;
    private readonly INavigationViewModel _navigationViewModel;
    private readonly IQueueService _queueService;
    private QueueFormModel? _form;
    public QueueDetails? QueueDetails { get; set; }

    public QueueViewModel(
        IQueueService queueService,
        ISnackbar snackbar,
        NavigationManager navigationManager,
        IDialogService dialogService)
    {
        _queueService = queueService;
        _snackbar = snackbar;
        _navigationManager = navigationManager;
        _dialogService = dialogService;
    }

    public QueueFormModel? Form
    {
        get => _form;
        set
        {
            _form = value;
            _form.PropertyChanged += (_, _) => this.Notify(PropertyChanged);
            this.Notify(PropertyChanged);
        }
    }
    public async Task InitializeForm(
        string connectionName, string? queueName, CancellationToken cancellationToken)
    {
        if (queueName != null)
        {
            UpdateFormModel(
                await _queueService.GetAsync(connectionName, queueName, cancellationToken));
        }
        else
        {
            CreateFormModel();
        }
    }

    public async Task UpdateQueueFromAsync(string connectionName)
    {
        if (Form != null)
        {
            try
            {
                var result = await SaveQueueAsync(connectionName, Form);

                HandleSaveResult(connectionName, result, Form.OperationType);
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error while updating queue: {ex.Message}", Severity.Error);
            }
        }
    }
    private void HandleSaveResult(
        string connectionName, 
        OperationResult<QueueDetails> result,
        OperationType operationType)
    {
        if (result.Success && result.Data != null)
        {
            if (operationType == OperationType.Create)
            {
                _navigationManager.NavigateTo($"queue/{connectionName}/{result.Data.Info.Name}");

                QueueAdded(connectionName, result.Data.Info);
            }
            else
            {
                UpdateFormModel(result.Data);
            }

            _snackbar.Add("Saved successfully", Severity.Success);
        }
        else
        {
            _snackbar.Add("Not saved. Please correct parameters and try again.",
                Severity.Error);
        }
    }
    private async Task<OperationResult<QueueDetails>> SaveQueueAsync(string connectionName,
        QueueFormModel form)
    {
        switch (form.OperationType)
        {
            case OperationType.Create:
                return await _queueService.CreateAsync(
                    connectionName,
                    Form.ToCreateOptions(),
                    default);
            case OperationType.Update:
                return await _queueService.UpdateAsync(
                    connectionName,
                    Form.ToUpdateOptions(),
                    default);
            default:
                return new OperationResult<QueueDetails>(false, null);
        }
    }

    public void NavigateToNewQueueForm(string connectionName)
    {
        _navigationManager.NavigateTo($"new-queue/{connectionName}");
    }
    
    public async Task CloneQueue(
        string connectionName, 
        string sourceQueueName, 
        CancellationToken cancellationToken)
    {
        var parameters = new DialogParameters();
        
        parameters.Add(nameof(CloneQueueDialog.ConnectionName), connectionName);
        parameters.Add(nameof(CloneQueueDialog.SourceDialogName), sourceQueueName);
        
        var dialog = _dialogService.Show<CloneQueueDialog>(
            $"Clone queue {sourceQueueName}",
            parameters,
            new DialogOptions
            {
                FullWidth = true,
                CloseOnEscapeKey = true
            });

        var dialogResult = await dialog.Result;

        if (!dialogResult.Cancelled && dialogResult.Data is string newQueueName)
        {
            var result = await _queueService.CloneAsync(
                connectionName,
                newQueueName,
                sourceQueueName,
                cancellationToken);

            HandleSaveResult(connectionName, result, OperationType.Create);
        }
    }
    public async Task DeleteQueue(
        string connectionName, 
        string queueName, 
        CancellationToken cancellationToken)
    {
        var parameters = new DialogParameters();
        parameters.Add(
            "ContentText",
            $"Are you sure you want to delete queue {queueName}?");

        var dialog = _dialogService.Show<ConfirmDialog>("Confirm", parameters);
        var dialogResult = await dialog.Result;

        if (dialogResult.Data is true)
        {
            var deleteResult =
                await _queueService.DeleteAsync(connectionName, queueName, cancellationToken);

            if (deleteResult.Success)
            {
                _snackbar.Add(
                    $"Queue {queueName} successfully deleted.",
                    Severity.Success);
                QueueRemoved(connectionName, queueName);
                NavigateToNewQueueForm(connectionName);

            }
            else
            {
                _snackbar.Add(
                    $"Queue {queueName} was not deleted. " +
                    $"Please check the queue name and try again later.",
                    Severity.Error);
            }

        }
    }
    public Task ViewMessages(string connectionName, string queueName, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private void UpdateFormModel(QueueDetails resultData)
    {
        QueueDetails = resultData;
        Form = QueueDetails.ToFormModel(OperationType.Update);
    }

    private void CreateFormModel()
    {
        Form = new QueueFormModel(OperationType.Create)
        {
            MaxDeliveryCount = 10,
            MaxSizeInMegabytes = 1024,
            MaxMessageSizeInKilobytes = 256,
            EnableBatchedOperations = true,
            AutoDeleteOnIdle = TimeSpan.FromDays(365),
            DefaultMessageTimeToLive = TimeSpan.FromDays(365),
            DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(5),
            LockDuration = TimeSpan.FromMinutes(1)
        };
    }
}