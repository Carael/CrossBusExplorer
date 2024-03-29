using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.Website.Extensions;
using CrossBusExplorer.Website.Jobs;
using CrossBusExplorer.Website.Mappings;
using CrossBusExplorer.Website.Models;
using CrossBusExplorer.Website.Shared;
using CrossBusExplorer.Website.Shared.Messages;
using Microsoft.Azure.Amqp.Serialization;
using MudBlazor;
namespace CrossBusExplorer.Website.ViewModels;

public class MessagesViewModel : IMessagesViewModel
{
    private readonly IJobsViewModel _jobsViewModel;
    private readonly IMessageService _messageService;
    private readonly ISnackbar _snackbar;
    private readonly IDialogService _dialogService;
    public event PropertyChangedEventHandler? PropertyChanged;

    private ObservableCollection<Message> _messages;
    private CurrentMessagesEntity? _entity;

    public MessagesViewModel(
        IJobsViewModel jobsViewModel,
        IMessageService messageService,
        ISnackbar snackbar,
        IDialogService dialogService)
    {
        _jobsViewModel = jobsViewModel;
        _messageService = messageService;
        _snackbar = snackbar;
        _dialogService = dialogService;
    }

    public ObservableCollection<Message> Messages
    {
        get => _messages;
        private set
        {
            _messages = value;
            _messages.CollectionChanged += (_, _) => this.Notify(PropertyChanged);
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

    public bool IsPeekMode(ReceiveMessagesForm formModel)
    {
        return formModel.Mode == ReceiveMode.PeekLock;
    }

    public bool CanPeekMore(ReceiveMessagesForm formModel) =>
        Messages.Any() &&
        formModel is { Mode: ReceiveMode.PeekLock, Type: ReceiveType.ByCount };

    public async Task PeekMore(
        ReceiveMessagesForm formModel,
        CancellationToken cancellationToken)
    {
        var lastSequenceNumber = Messages
            .OrderBy(p => p.SystemProperties.SequenceNumber)
            .Last()
            .SystemProperties.SequenceNumber;

        formModel.FromSequenceNumber = lastSequenceNumber + 1;

        List<Message> messages = await LoadMessagesAsync(formModel, cancellationToken);

        foreach (Message message in messages)
        {
            Messages.Add(message);
        }
    }

    public async Task OnSubmitReceiveForm(
        MudForm form,
        ReceiveMessagesForm formModel,
        CancellationToken cancellationToken)
    {
        await form.Validate();

        if (form.IsValid)
        {
            Messages =
                new ObservableCollection<Message>(
                    await LoadMessagesAsync(formModel, cancellationToken));
        }

        DialogVisible = false;
    }
    public void Initialize(CurrentMessagesEntity entity)
    {
        if (_entity == null || !_entity.Equals(entity))
        {
            _entity = entity;
            Messages = new ObservableCollection<Message>();
            DialogVisible = true;
        }
    }
    public async Task ViewMessageDetails(Message? message, bool editMode)
    {
        var parameters = new DialogParameters
        {
            {
                nameof(MessageDetailsDialog.Message), message
            },
            {
                nameof(MessageDetailsDialog.QueueOrTopicName), _entity.QueueOrTopicName
            },
            {
                nameof(MessageDetailsDialog.EditMode), editMode
            }
        };

        IDialogReference? dialogReference = await _dialogService.ShowAsync<MessageDetailsDialog>(
            "Message details",
            parameters,
            new DialogOptions
            {
                FullWidth = true,
                FullScreen = false,
                MaxWidth = MaxWidth.ExtraLarge,
                CloseButton = true,
                CloseOnEscapeKey = true,
                Position = DialogPosition.Center
            });

        DialogResult? dialogResult = await dialogReference.Result;

        if (dialogResult is { Canceled: false, Data: RequeueMessage requeueMessage })
        {
            await Requeue(requeueMessage.QueueOrTopicName, requeueMessage.Message);
        }
    }

    public async Task Requeue(string queueOrTopicName, MessageDetailsModel message)
    {
        try
        {
            Result sendMessageResult = await _messageService.SendMessagesAsync(
                _entity.ConnectionName,
                queueOrTopicName,
                new[]
                {
                    message.ToSendMessage()
                },
                default);

            //TODO: rethink the SendMessageAsync method result
            if (sendMessageResult.Count == 1)
            {
                _snackbar.Add(
                    $"Message successfully resend to queue/topic " +
                    $"{queueOrTopicName}", Severity.Success);
            }
            else
            {
                _snackbar.Add("Message was not send. Please try again.", Severity.Warning);
            }
        }
        catch (Exception ex)
        {
            _snackbar.Add($"Error while sending message. Error: {ex.Message}", Severity.Error);
        }
    }

    public async Task Delete(Message message, SubQueue subQueue)
    {
        try
        {
            var parameters = new DialogParameters();
            parameters.Add(
                "ContentText",
                $"Are you sure you want to remove message with sequence " +
                $"number {message.SystemProperties.SequenceNumber}?");

            IDialogReference? dialog = await _dialogService.ShowAsync<ConfirmDialog>(
                "Confirm",
                parameters,
                new DialogOptions
                {
                    CloseOnEscapeKey = true
                });

            DialogResult? result = await dialog.Result;

            if (result.Data is false)
            {
                return;
            }

            var job = new DeleteMessageJob(
                _entity.ConnectionName,
                _entity.QueueOrTopicName,
                _entity.SubscriptionName,
                subQueue,
                message.SystemProperties.SequenceNumber,
                _messageService);

            job.OnCompleted += async (_, _, _) =>
            {
                if(job.WarningMessage != null)
                {
                    _snackbar.Add(job.WarningMessage, Severity.Warning);
                }
                else
                {
                    Messages.Remove(message);
                }
            };

            await _jobsViewModel.ScheduleJob(job);
        }
        catch (Exception ex)
        {
            _snackbar.Add($"Error while removing message. Error: {ex.Message}", Severity.Error);
        }
    }

    public async Task ImportMessagesFromFileAsync(CancellationToken cancellationToken)
    {
        try
        {
            var parameters = new DialogParameters();
            IDialogReference? dialog = await _dialogService.ShowAsync<MessagesUploadDialog>(
                "Confirm",
                parameters,
                new DialogOptions
                {
                    CloseOnEscapeKey = true
                });

            DialogResult? result = await dialog.Result;

            if (result.Data is MessagesUploadDialogResult resultData)
            {
                if (resultData.FilesContent.Count == 0)
                {
                    _snackbar.Add("No files selected", Severity.Warning);
                    return;
                }

                var messages = new List<SendMessage>();

                foreach (var fileContent in resultData.FilesContent)
                {
                    messages.Add(SendMessage.CreateFromBody(fileContent));
                }

                var resultCount = await _messageService.SendMessagesAsync(
                    _entity!.ConnectionName,
                    _entity!.QueueOrTopicName,
                    messages,
                    cancellationToken);

                _snackbar.Add($"Successfully uploaded {resultCount} messages", Severity.Success);
            }
            else
            {
                _snackbar.Add("No files were uploaded.", Severity.Warning);
            }
        }
        catch (Exception ex)
        {
            _snackbar.Add($"Error while sending messages from files. {ex.Message}", Severity.Error);
        }
    }

    private async Task<List<Message>> LoadMessagesAsync(
        ReceiveMessagesForm formModel,
        CancellationToken cancellationToken)
    {
        try
        {
            return (await _messageService.GetMessagesAsync(
                _entity.ConnectionName,
                _entity.QueueOrTopicName,
                _entity.SubscriptionName,
                formModel.SubQueue,
                formModel.Mode,
                formModel.Type,
                formModel.MessagesCount,
                formModel.FromSequenceNumber,
                cancellationToken)).ToList();
        }
        catch (ServiceBusOperationException ex)
        {
            _snackbar.Add(ex.Message, Severity.Error);
            return new List<Message>();
        }
    }
}
