using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace DataverseServices.Abstractions;

public interface IBulkService
{
    ExecuteMultipleResponse ExecuteMultiple(
        IEnumerable<OrganizationRequest> requests,
        bool continueOnError = false,
        bool returnResponses = false);

    IReadOnlyList<ExecuteMultipleResponse> ExecuteMultipleChunked(
        IEnumerable<OrganizationRequest> requests,
        int batchSize = 500,
        bool continueOnError = false,
        bool returnResponses = false);

    CreateMultipleResponse CreateMultiple<TEntity>(
        IReadOnlyCollection<TEntity> entities)
        where TEntity : Entity, new();

    IReadOnlyList<CreateMultipleResponse> CreateMultipleChunked<TEntity>(
        IEnumerable<TEntity> entities,
        int batchSize = 500)
        where TEntity : Entity, new();

    UpdateMultipleResponse UpdateMultiple<TEntity>(
        IReadOnlyCollection<TEntity> entities)
        where TEntity : Entity, new();

    IReadOnlyList<UpdateMultipleResponse> UpdateMultipleChunked<TEntity>(
        IEnumerable<TEntity> entities,
        int batchSize = 500)
        where TEntity : Entity, new();

    UpsertMultipleResponse UpsertMultiple<TEntity>(
        IReadOnlyCollection<TEntity> entities)
        where TEntity : Entity, new();

    IReadOnlyList<UpsertMultipleResponse> UpsertMultipleChunked<TEntity>(
        IEnumerable<TEntity> entities,
        int batchSize = 500)
        where TEntity : Entity, new();

    IReadOnlyList<ExecuteMultipleResponse> UpdateMultipleWithFallbackChunked<TEntity>(
        IEnumerable<TEntity> entities,
        int batchSize = 500,
        bool continueOnError = false,
        bool returnResponses = false)
        where TEntity : Entity, new();

    IReadOnlyList<ExecuteMultipleResponse> UpsertMultipleWithFallbackChunked<TEntity>(
        IEnumerable<TEntity> entities,
        int batchSize = 500,
        bool continueOnError = false,
        bool returnResponses = false)
        where TEntity : Entity, new();
}
