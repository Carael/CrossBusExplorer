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
using Microsoft.AspNetCore.Components;
using MudBlazor;
namespace CrossBusExplorer.Website.ViewModels;

public class QueueViewModel : IQueueViewModel
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event QueueAddedEventHandler? QueueAdded;
    
    private readonly ISnackbar _snackbar;
    private readonly NavigationManager _navigationManager;
    private readonly INavigationViewModel _navigationViewModel;
    private readonly IQueueService _queueService;
    private QueueFormModel? _form;
    public QueueDetails? QueueDetails { get; set; }

    public QueueViewModel(
        IQueueService queueService,
        ISnackbar snackbar,
        NavigationManager navigationManager)
    {
        _queueService = queueService;
        _snackbar = snackbar;
        _navigationManager = navigationManager;
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

                if (result.Success && result.Data != null)
                {
                    if (Form.OperationType == OperationType.Create)
                    {
                        _navigationManager.NavigateTo($"queue/{connectionName}/{Form.Name}");

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
            catch (Exception ex)
            {
                _snackbar.Add($"Error while updating queue: {ex.Message}", Severity.Error);
            }
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
    public void CloneQueue(string connectionName, string queueName)
    {
        //todo: dialog
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