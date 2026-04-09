using DataverseServices.Abstractions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace DataverseServices.Infrastructure;

public sealed class BulkService : IBulkService
{
    private const int MaxBatchSize = 1000;

    private readonly IOrganizationService _service;
    private readonly ITracingService _tracing;

    public BulkService(
        IOrganizationService service,
        ITracingService tracing)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _tracing = tracing ?? throw new ArgumentNullException(nameof(tracing));
    }

    public ExecuteMultipleResponse ExecuteMultiple(
        IEnumerable<OrganizationRequest> requests,
        bool continueOnError = false,
        bool returnResponses = false)
    {
        var requestList = requests?.ToList() ?? new List<OrganizationRequest>();

        if (requestList.Count == 0)
        {
            throw new ArgumentException("At least one request is required.", nameof(requests));
        }

        if (requestList.Count > MaxBatchSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requests),
                $"ExecuteMultiple supports at most {MaxBatchSize} requests per batch.");
        }

        if (requestList.Any(r => r is ExecuteMultipleRequest))
        {
            throw new InvalidPluginExecutionException(
                "Nested ExecuteMultipleRequest is not allowed.");
        }

        var request = new ExecuteMultipleRequest
        {
            Settings = new ExecuteMultipleSettings
            {
                ContinueOnError = continueOnError,
                ReturnResponses = returnResponses
            },
            Requests = new OrganizationRequestCollection()
        };

        foreach (var item in requestList)
        {
            request.Requests.Add(item);
        }

        _tracing.Trace(
            "BulkService.ExecuteMultiple | Count={0} | ContinueOnError={1} | ReturnResponses={2}",
            requestList.Count,
            continueOnError,
            returnResponses);

        return (ExecuteMultipleResponse)_service.Execute(request);
    }

    public IReadOnlyList<ExecuteMultipleResponse> ExecuteMultipleChunked(
        IEnumerable<OrganizationRequest> requests,
        int batchSize = 500,
        bool continueOnError = false,
        bool returnResponses = false)
    {
        var normalizedBatchSize = NormalizeBatchSize(batchSize);
        var requestList = requests?.ToList() ?? new List<OrganizationRequest>();
        var responses = new List<ExecuteMultipleResponse>();

        foreach (var chunk in Chunk(requestList, normalizedBatchSize))
        {
            responses.Add(ExecuteMultiple(chunk, continueOnError, returnResponses));
        }

        return responses;
    }

    public CreateMultipleResponse CreateMultiple<TEntity>(
        IReadOnlyCollection<TEntity> entities)
        where TEntity : Entity, new()
    {
        var entityCollection = BuildEntityCollection<TEntity>(entities);

        var request = new CreateMultipleRequest
        {
            Targets = entityCollection
        };

        _tracing.Trace(
            "BulkService.CreateMultiple | Entity={0} | Count={1}",
            GetLogicalName<TEntity>(),
            entityCollection.Entities.Count);

        return (CreateMultipleResponse)_service.Execute(request);
    }

    public IReadOnlyList<CreateMultipleResponse> CreateMultipleChunked<TEntity>(
        IEnumerable<TEntity> entities,
        int batchSize = 500)
        where TEntity : Entity, new()
    {
        var normalizedBatchSize = NormalizeBatchSize(batchSize);
        var entityList = entities?.ToList() ?? new List<TEntity>();
        var responses = new List<CreateMultipleResponse>();

        foreach (var chunk in Chunk(entityList, normalizedBatchSize))
        {
            responses.Add(CreateMultiple(chunk));
        }

        return responses;
    }

    public UpdateMultipleResponse UpdateMultiple<TEntity>(
        IReadOnlyCollection<TEntity> entities)
        where TEntity : Entity, new()
    {
        var entityCollection = BuildEntityCollection<TEntity>(entities);

        var request = new UpdateMultipleRequest
        {
            Targets = entityCollection
        };

        _tracing.Trace(
            "BulkService.UpdateMultiple | Entity={0} | Count={1}",
            GetLogicalName<TEntity>(),
            entityCollection.Entities.Count);

        return (UpdateMultipleResponse)_service.Execute(request);
    }

    public IReadOnlyList<UpdateMultipleResponse> UpdateMultipleChunked<TEntity>(
        IEnumerable<TEntity> entities,
        int batchSize = 500)
        where TEntity : Entity, new()
    {
        var normalizedBatchSize = NormalizeBatchSize(batchSize);
        var entityList = entities?.ToList() ?? new List<TEntity>();
        var responses = new List<UpdateMultipleResponse>();

        foreach (var chunk in Chunk(entityList, normalizedBatchSize))
        {
            responses.Add(UpdateMultiple(chunk));
        }

        return responses;
    }

    public UpsertMultipleResponse UpsertMultiple<TEntity>(
        IReadOnlyCollection<TEntity> entities)
        where TEntity : Entity, new()
    {
        var entityCollection = BuildEntityCollection<TEntity>(entities);

        var request = new UpsertMultipleRequest
        {
            Targets = entityCollection
        };

        _tracing.Trace(
            "BulkService.UpsertMultiple | Entity={0} | Count={1}",
            GetLogicalName<TEntity>(),
            entityCollection.Entities.Count);

        return (UpsertMultipleResponse)_service.Execute(request);
    }

    public IReadOnlyList<UpsertMultipleResponse> UpsertMultipleChunked<TEntity>(
        IEnumerable<TEntity> entities,
        int batchSize = 500)
        where TEntity : Entity, new()
    {
        var normalizedBatchSize = NormalizeBatchSize(batchSize);
        var entityList = entities?.ToList() ?? new List<TEntity>();
        var responses = new List<UpsertMultipleResponse>();

        foreach (var chunk in Chunk(entityList, normalizedBatchSize))
        {
            responses.Add(UpsertMultiple(chunk));
        }

        return responses;
    }

    public IReadOnlyList<ExecuteMultipleResponse> UpdateMultipleWithFallbackChunked<TEntity>(
        IEnumerable<TEntity> entities,
        int batchSize = 500,
        bool continueOnError = false,
        bool returnResponses = false)
        where TEntity : Entity, new()
    {
        var normalizedBatchSize = NormalizeBatchSize(batchSize);
        var entityList = entities?.ToList() ?? new List<TEntity>();
        var responses = new List<ExecuteMultipleResponse>();

        foreach (var chunk in Chunk(entityList, normalizedBatchSize))
        {
            var requests = chunk
                .Select(entity => (OrganizationRequest)new UpdateRequest { Target = entity })
                .ToArray();

            responses.Add(ExecuteMultiple(requests, continueOnError, returnResponses));
        }

        return responses;
    }

    public IReadOnlyList<ExecuteMultipleResponse> UpsertMultipleWithFallbackChunked<TEntity>(
        IEnumerable<TEntity> entities,
        int batchSize = 500,
        bool continueOnError = false,
        bool returnResponses = false)
        where TEntity : Entity, new()
    {
        var normalizedBatchSize = NormalizeBatchSize(batchSize);
        var entityList = entities?.ToList() ?? new List<TEntity>();
        var responses = new List<ExecuteMultipleResponse>();

        foreach (var chunk in Chunk(entityList, normalizedBatchSize))
        {
            var requests = chunk
                .Select(entity => (OrganizationRequest)new UpsertRequest { Target = entity })
                .ToArray();

            responses.Add(ExecuteMultiple(requests, continueOnError, returnResponses));
        }

        return responses;
    }

    private static EntityCollection BuildEntityCollection<TEntity>(
        IReadOnlyCollection<TEntity> entities)
        where TEntity : Entity, new()
    {
        if (entities == null || entities.Count == 0)
        {
            throw new ArgumentException("At least one entity is required.", nameof(entities));
        }

        if (entities.Count > MaxBatchSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(entities),
                $"Bulk request supports at most {MaxBatchSize} records per batch.");
        }

        var logicalName = GetLogicalName<TEntity>();

        var collection = new EntityCollection
        {
            EntityName = logicalName
        };

        foreach (var entity in entities)
        {
            if (entity == null)
            {
                throw new InvalidPluginExecutionException("Entity collection contains null item.");
            }

            if (!string.Equals(entity.LogicalName, logicalName, StringComparison.Ordinal))
            {
                throw new InvalidPluginExecutionException(
                    $"Entity logical name '{entity.LogicalName}' does not match expected '{logicalName}'.");
            }

            collection.Entities.Add(entity);
        }

        return collection;
    }

    private static string GetLogicalName<TEntity>()
        where TEntity : Entity, new()
    {
        return new TEntity().LogicalName;
    }

    private static int NormalizeBatchSize(int batchSize)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize));
        }

        return Math.Min(batchSize, MaxBatchSize);
    }

    private static IEnumerable<IReadOnlyCollection<T>> Chunk<T>(
        IReadOnlyCollection<T> items,
        int chunkSize)
    {
        if (items.Count == 0)
        {
            yield break;
        }

        var buffer = new List<T>(chunkSize);

        foreach (var item in items)
        {
            buffer.Add(item);

            if (buffer.Count == chunkSize)
            {
                yield return buffer.ToArray();
                buffer.Clear();
            }
        }

        if (buffer.Count > 0)
        {
            yield return buffer.ToArray();
        }
    }
}
