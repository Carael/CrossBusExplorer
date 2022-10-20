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
using CrossBusExplorer.Website.Models;
using MudBlazor;
namespace CrossBusExplorer.Website.ViewModels;

public class MessagesViewModel : IMessagesViewModel
{
    private readonly IMessageService _messageService;
    private readonly ISnackbar _snackbar;
    public event PropertyChangedEventHandler? PropertyChanged;

    private ObservableCollection<Message> _messages;
    private CurrentMessagesEntity? _entity;

    public MessagesViewModel(
        IMessageService messageService,
        ISnackbar snackbar)
    {
        _messageService = messageService;
        _snackbar = snackbar;
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

    public void SetDialogVisibility(bool visible)
    {
        DialogVisible = visible;
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