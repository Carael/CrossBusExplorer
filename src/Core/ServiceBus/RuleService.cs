using System.Runtime.CompilerServices;
using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using CrossBusExplorer.Management.Contracts;
using CrossBusExplorer.ServiceBus.Contracts;
using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.ServiceBus;

public class RuleService : IRuleService
{
    private readonly IConnectionManagement _connectionManagement;

    public RuleService(IConnectionManagement connectionManagement)
    {
        _connectionManagement = connectionManagement;
    }

    public async IAsyncEnumerable<Rule> GetAsync(
        string connectionName,
        string topicName,
        string subscriptionName,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var connection =
            await _connectionManagement.GetAsync(connectionName, cancellationToken);

        ServiceBusAdministrationClient administrationClient =
            new ServiceBusAdministrationClient(connection.ConnectionString);

        AsyncPageable<RuleProperties> subscriptionsPageable =
            administrationClient.GetRulesAsync(
                topicName,
                subscriptionName,
                cancellationToken);

        await foreach (var ruleProperties in subscriptionsPageable)
        {
            yield return new Rule(
                ruleProperties.Name,
                GetFilterType(ruleProperties.Filter),
                GetFilterValue(ruleProperties.Filter));
        }
    }

    public async Task<OperationResult> DeleteAsync(
        string connectionName,
        string topicName,
        string subscriptionName,
        string ruleName,
        CancellationToken cancellationToken)
    {
        try
        {
            var connection =
                await _connectionManagement.GetAsync(connectionName, cancellationToken);

            ServiceBusAdministrationClient administrationClient =
                new ServiceBusAdministrationClient(connection.ConnectionString);

            var response = await administrationClient.DeleteRuleAsync(
                topicName,
                subscriptionName,
                ruleName,
                cancellationToken);

            return new OperationResult(!response.IsError);
        }
        catch (ServiceBusException ex)
        {
            //TODO: log

            throw new ServiceBusOperationException(ex.Reason.ToString(), ex.Message);
        }
    }

    public async Task<OperationResult<Rule>> CreateAsync(
        string connectionName,
        string topicName,
        string subscriptionName,
        string ruleName,
        RuleType type,
        string? value,
        CancellationToken cancellationToken)
    {
        try
        {
            var connection =
                await _connectionManagement.GetAsync(connectionName, cancellationToken);

            ServiceBusAdministrationClient administrationClient =
                new ServiceBusAdministrationClient(connection.ConnectionString);

            var options = GetCreateOptions(ruleName, type, value);

            var response = await administrationClient.CreateRuleAsync(
                topicName,
                subscriptionName,
                options,
                cancellationToken);

            return new OperationResult<Rule>(
                true,
                GetRule(response.Value));
        }
        catch (ServiceBusException ex)
        {
            //TODO: log

            throw new ServiceBusOperationException(ex.Reason.ToString(), ex.Message);
        }
        catch (ArgumentException ex)
        {
            //TODO: log

            throw new ValidationException(ErrorCodes.InvalidArgument, ex.Message);
        }
    }

    public async Task<OperationResult<Rule>> UpdateAsync(
        string connectionName,
        string topicName,
        string subscriptionName,
        string ruleName,
        RuleType type,
        string? value,
        CancellationToken cancellationToken)
    {
        try
        {
            var connection =
                await _connectionManagement.GetAsync(connectionName, cancellationToken);

            ServiceBusAdministrationClient administrationClient =
                new ServiceBusAdministrationClient(connection.ConnectionString);

            var ruleProperties = await administrationClient.GetRuleAsync(
                topicName,
                subscriptionName,
                ruleName,
                cancellationToken);

            ruleProperties.Value.Filter = GetRuleFilter(type, value);

            var response = await administrationClient.UpdateRuleAsync(
                topicName,
                subscriptionName,
                ruleProperties,
                cancellationToken);

            return new OperationResult<Rule>(
                true,
                GetRule(response.Value));
        }
        catch (ServiceBusException ex)
        {
            //TODO: log

            throw new ServiceBusOperationException(ex.Reason.ToString(), ex.Message);
        }
        catch (ArgumentException ex)
        {
            //TODO: log

            throw new ValidationException(ErrorCodes.InvalidArgument, ex.Message);
        }
    }
    private RuleFilter GetRuleFilter(RuleType type, string? value) =>
        type switch
        {
            RuleType.Sql => new SqlRuleFilter(value),
            RuleType.CorrelationId => new CorrelationRuleFilter(value),
            RuleType.FalseFilter => new FalseRuleFilter(),
            RuleType.TrueFilter => new TrueRuleFilter(),
            _ => throw new NotSupportedException($"RuleType {type} not supported.")
        };

    private Rule GetRule(RuleProperties ruleProperties)
    {
        return new Rule(
            ruleProperties.Name,
            GetFilterType(ruleProperties.Filter),
            GetFilterValue(ruleProperties.Filter));
    }

    private string? GetFilterValue(RuleFilter ruleProperties) =>
        ruleProperties switch
        {
            TrueRuleFilter or FalseRuleFilter => null,
            SqlRuleFilter sqlRuleFilter => sqlRuleFilter.SqlExpression,
            CorrelationRuleFilter correlationRuleFilter => correlationRuleFilter.CorrelationId,
            _ => throw new NotSupportedException(
                $"RuleFilter of type {ruleProperties.GetType().Name} is not supported.")
        };

    private RuleType GetFilterType(RuleFilter ruleProperties) =>
        ruleProperties switch
        {
            TrueRuleFilter => RuleType.TrueFilter,
            FalseRuleFilter => RuleType.FalseFilter,
            SqlRuleFilter => RuleType.Sql,
            CorrelationRuleFilter => RuleType.CorrelationId,
            _ => throw new NotSupportedException(
                $"RuleFilter of type {ruleProperties.GetType().Name} is not supported.")
        };

    private CreateRuleOptions GetCreateOptions(string ruleName, RuleType type, string? value) =>
        new CreateRuleOptions(ruleName, GetRuleFilter(type, value));
}