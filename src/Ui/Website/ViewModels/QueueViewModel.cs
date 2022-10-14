using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.Website.Extensions;
using CrossBusExplorer.Website.Mappings;
using CrossBusExplorer.Website.Models;
using MudBlazor;
namespace CrossBusExplorer.Website.ViewModels;

public class QueueViewModel : IQueueViewModel
{
    private readonly ISnackbar _snackbar;
    private readonly IQueueService _queueService;
    private QueueFormModel? _form;
    public event PropertyChangedEventHandler? PropertyChanged;
    public QueueDetails? QueueDetails { get; set; }

    public QueueViewModel(
        IQueueService queueService, 
        ISnackbar snackbar)
    {
        _queueService = queueService;
        _snackbar = snackbar;
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
    public async Task LoadQueueAsync(
        string connectionName, string queueName, CancellationToken cancellationToken)
    {
        UpdateFormModel(await _queueService.GetAsync(connectionName, queueName, default));
    }
    public async Task UpdateQueueFromAsync(string connectionName)
    {
        if (Form != null)
        {
            try
            {
                var result = await _queueService.UpdateAsync(
                    connectionName,
                    Form.ToUpdateOptions(),
                    default);

                if (result.Success && result.Data != null)
                {
                    UpdateFormModel(result.Data);
                    
                    _snackbar.Add("Queue updated successfully", Severity.Success);
                }
                else
                {
                    _snackbar.Add("Queue not updated.", Severity.Success);
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error while updating queue: {ex.Message}");
            }
        }
    }
    private void UpdateFormModel(QueueDetails resultData)
    {
        QueueDetails = resultData;
        Form = QueueDetails.ToFormModel();
    }
}