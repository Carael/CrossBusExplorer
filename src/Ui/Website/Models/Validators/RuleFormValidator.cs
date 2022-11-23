using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrossBusExplorer.ServiceBus.Contracts.Types;
using CrossBusExplorer.Website.Extensions;
using FluentValidation;
namespace CrossBusExplorer.Website.Models.Validators;

public class RuleFormValidator : AbstractValidator<RuleFormModel>
{
    public RuleFormValidator()
    {
        RuleFor(p => p.Name)
            .NotNull()
            .NotEmpty()
            .WithMessage("Name is required.");

        RuleFor(p => p.Value)
            .NotNull()
            .NotEmpty()
            .When(p => p.Type is RuleType.CorrelationId or RuleType.Sql)
            .WithMessage($"When type is set to {RuleType.Sql} or " +
                         $"{RuleType.CorrelationId} the value must be set.");
    }

    public Func<object, string, Task<IEnumerable<string>>> ValidateValue =>
        async (model, propertyName) =>
        {
            var result = await ValidateAsync(
                ValidationContext<RuleFormModel>.CreateWithOptions(
                    (RuleFormModel)model,
                    x => x.IncludeProperties(propertyName.SplitAndGetLastSection('.'))));

            if (result.IsValid)
                return Array.Empty<string>();

            return result.Errors.Select(e => e.ErrorMessage);
        };
}