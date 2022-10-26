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
using CrossBusExplorer.Website.Mappings;
using CrossBusExplorer.Website.Models;
using CrossBusExplorer.Website.Shared;
using CrossBusExplorer.Website.Shared.Messages;
using MudBlazor;
namespace CrossBusExplorer.Website.ViewModels;

public class MessagesViewModel : IMessagesViewModel
{
    private readonly IMessageService _messageService;
    private readonly ISnackbar _snackbar;
    private readonly IDialogService _dialogService;
    public event PropertyChangedEventHandler? PropertyChanged;

    private ObservableCollection<Message> _messages;
    private CurrentMessagesEntity? _entity;

    public MessagesViewModel(
        IMessageService messageService,
        ISnackbar snackbar,
        IDialogService dialogService)
    {
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

    public bool CanPeekMore(ReceiveMessagesForm formModel) =>
        Messages.Any() &&
        formModel.Mode == ReceiveMode.PeekLock &&
        formModel.Type == ReceiveType.ByCount;

    public async Task PeekMore(
        ReceiveMessagesForm formModel,
        CancellationToken cancellationToken)
    {
        var lastSequenceNumber = Messages
            .OrderBy(p => p.SystemProperties.SequenceNumber)
            .Last()
            .SystemProperties.SequenceNumber;

        formModel.FromSequenceNumber = lastSequenceNumber + 1;

        var messages = await LoadMessagesAsync(formModel, cancellationToken);

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

        var dialogReference = _dialogService.Show<MessageDetailsDialog>(
            "Message details",
            parameters,
            new DialogOptions
            {
                FullWidth = true,
                FullScreen = false,
                MaxWidth = MaxWidth.Large,
                CloseButton = true,
                CloseOnEscapeKey = true,
                Position = DialogPosition.Center
            });

        var dialogResult = await dialogReference.Result;

        if (!dialogResult.Cancelled && dialogResult.Data is RequeueMessage requeueMessage)
        {
            Requeue(requeueMessage.QueueOrTopicName, requeueMessage.Message);
        }
    }

    public async Task Requeue(string queueOrTopicName, MessageDetailsModel message)
    {
        try
        {
            var sendMessageResult = await _messageService.SendMessagesAsync(
                _entity.ConnectionName,
                queueOrTopicName,
                new[] { message.ToSendMessage() },
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