using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using FluentValidation;
namespace CrossBusExplorer.Website.Models.Validators;

public class ReceiveMessageFormValidator : AbstractValidator<ReceiveMessagesForm>
{
    public ReceiveMessageFormValidator()
    {
        RuleFor(p => p.Mode)
            .Empty()
            .WithMessage("Required");

        RuleFor(p => p.Type)
            .Empty()
            .WithMessage("Required");

        When(p => p.Type == ReceiveMessagesType.Top, () =>
        {
            RuleFor(p => p.MessagesCount).GreaterThan(0)
                .WithMessage($"When mode is set to {ReceiveMode.PeekLock} then " +
                             $"{nameof(ReceiveMessagesForm.MessagesCount)} hast to be" +
                             $" greater then 0.");
        });
    }

    public Func<object, string, Task<IEnumerable<string>>> ValidateValue =>
        async (model, propertyName) =>
        {
            var result = await ValidateAsync(
                ValidationContext<ReceiveMessagesForm>.CreateWithOptions((ReceiveMessagesForm)model,
                    x => x.IncludeProperties(propertyName)));
            if (result.IsValid)
                return Array.Empty<string>();

            return result.Errors.Select(e => e.ErrorMessage);
        };
}