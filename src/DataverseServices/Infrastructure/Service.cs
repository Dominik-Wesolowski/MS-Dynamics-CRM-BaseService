using DataverseServices.Abstractions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System.Linq.Expressions;

namespace DataverseServices.Infrastructure;

public sealed class Service : IService
{
    private readonly IOrganizationService _service;
    private readonly ITracingService _tracing;

    public Service(
        IOrganizationService service,
        ITracingService tracing)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _tracing = tracing ?? throw new ArgumentNullException(nameof(tracing));
    }

    public IOrganizationService Raw => _service;

    public TEntity? Get<TEntity>(Guid id, ColumnSet columnSet)
        where TEntity : Entity, new()
    {
        Trace(nameof(Get), typeof(TEntity).Name, $"Id={id}");

        if (id == Guid.Empty)
        {
            return null;
        }

        if (columnSet == null)
        {
            throw new ArgumentNullException(nameof(columnSet));
        }

        var entity = _service.Retrieve(
            GetLogicalName<TEntity>(),
            id,
            columnSet);

        return entity?.ToEntity<TEntity>();
    }

    public TEntity? Get<TEntity>(
        Guid id,
        params Expression<Func<TEntity, object>>[] columns)
        where TEntity : Entity, new()
    {
        return Get<TEntity>(id, ColumnSetFactory.Create(columns));
    }

    public TEntity GetRequired<TEntity>(Guid id, ColumnSet columnSet)
        where TEntity : Entity, new()
    {
        var entity = Get<TEntity>(id, columnSet);

        if (entity == null)
        {
            throw new InvalidPluginExecutionException(
                $"{typeof(TEntity).Name} with id '{id}' was not found.");
        }

        return entity;
    }

    public TEntity GetRequired<TEntity>(
        Guid id,
        params Expression<Func<TEntity, object>>[] columns)
        where TEntity : Entity, new()
    {
        return GetRequired<TEntity>(id, ColumnSetFactory.Create(columns));
    }

    public TEntity? Get<TEntity>(EntityReference reference, ColumnSet columnSet)
        where TEntity : Entity, new()
    {
        Trace(nameof(Get), typeof(TEntity).Name, "EntityReference");

        if (reference == null || reference.Id == Guid.Empty)
        {
            return null;
        }

        return Get<TEntity>(reference.Id, columnSet);
    }

    public TEntity? Get<TEntity>(
        EntityReference reference,
        params Expression<Func<TEntity, object>>[] columns)
        where TEntity : Entity, new()
    {
        return Get<TEntity>(reference, ColumnSetFactory.Create(columns));
    }

    public TEntity GetRequired<TEntity>(EntityReference reference, ColumnSet columnSet)
        where TEntity : Entity, new()
    {
        var entity = Get<TEntity>(reference, columnSet);

        if (entity == null)
        {
            throw new InvalidPluginExecutionException(
                $"{typeof(TEntity).Name} for provided reference was not found.");
        }

        return entity;
    }

    public TEntity GetRequired<TEntity>(
        EntityReference reference,
        params Expression<Func<TEntity, object>>[] columns)
        where TEntity : Entity, new()
    {
        return GetRequired<TEntity>(reference, ColumnSetFactory.Create(columns));
    }

    public TEntity? GetByAlternateKey<TEntity>(
        KeyAttributeCollection keyAttributes,
        ColumnSet columnSet)
        where TEntity : Entity, new()
    {
        Trace(nameof(GetByAlternateKey), typeof(TEntity).Name, "AlternateKey");

        ValidateAlternateKey(keyAttributes);
        if (columnSet == null)
        {
            throw new ArgumentNullException(nameof(columnSet));
        }

        var reference = new EntityReference(GetLogicalName<TEntity>(), keyAttributes);
        return Get<TEntity>(reference, columnSet);
    }

    public TEntity? GetByAlternateKey<TEntity>(
        KeyAttributeCollection keyAttributes,
        params Expression<Func<TEntity, object>>[] columns)
        where TEntity : Entity, new()
    {
        return GetByAlternateKey<TEntity>(keyAttributes, ColumnSetFactory.Create(columns));
    }

    public TEntity GetByAlternateKeyRequired<TEntity>(
        KeyAttributeCollection keyAttributes,
        ColumnSet columnSet)
        where TEntity : Entity, new()
    {
        var entity = GetByAlternateKey<TEntity>(keyAttributes, columnSet);

        if (entity == null)
        {
            throw new InvalidPluginExecutionException(
                $"{typeof(TEntity).Name} for provided alternate key was not found.");
        }

        return entity;
    }

    public TEntity GetByAlternateKeyRequired<TEntity>(
        KeyAttributeCollection keyAttributes,
        params Expression<Func<TEntity, object>>[] columns)
        where TEntity : Entity, new()
    {
        return GetByAlternateKeyRequired<TEntity>(keyAttributes, ColumnSetFactory.Create(columns));
    }

    public TEntity? GetFirst<TEntity>(QueryExpression query)
        where TEntity : Entity, new()
    {
        EnsureQuery<TEntity>(query);
        query.TopCount = 1;

        return Get<TEntity>(query).FirstOrDefault();
    }

    public TEntity GetFirstRequired<TEntity>(QueryExpression query)
        where TEntity : Entity, new()
    {
        var entity = GetFirst<TEntity>(query);

        if (entity == null)
        {
            throw new InvalidPluginExecutionException(
                $"No {typeof(TEntity).Name} matched the query.");
        }

        return entity;
    }

    public TEntity? GetSingleOrDefault<TEntity>(QueryExpression query)
        where TEntity : Entity, new()
    {
        EnsureQuery<TEntity>(query);

        var entities = Get<TEntity>(query);

        if (entities.Count > 1)
        {
            throw new InvalidPluginExecutionException(
                $"Expected zero or one {typeof(TEntity).Name}, but found {entities.Count}.");
        }

        return entities.SingleOrDefault();
    }

    public TEntity GetSingle<TEntity>(QueryExpression query)
        where TEntity : Entity, new()
    {
        EnsureQuery<TEntity>(query);

        var entities = Get<TEntity>(query);

        if (entities.Count != 1)
        {
            throw new InvalidPluginExecutionException(
                $"Expected exactly one {typeof(TEntity).Name}, but found {entities.Count}.");
        }

        return entities.Single();
    }

    public IReadOnlyList<TEntity> Get<TEntity>(QueryExpression query)
        where TEntity : Entity, new()
    {
        EnsureQuery<TEntity>(query);

        Trace(nameof(Get), typeof(TEntity).Name, $"Query={query.EntityName}");

        return _service.RetrieveMultiple(query)
            .Entities
            .Select(x => x.ToEntity<TEntity>())
            .ToList();
    }

    public IReadOnlyList<TEntity> GetAll<TEntity>(QueryExpression query)
        where TEntity : Entity, new()
    {
        EnsureQuery<TEntity>(query);

        Trace(nameof(GetAll), typeof(TEntity).Name, $"Query={query.EntityName}");

        var results = new List<TEntity>();

        query.PageInfo ??= new PagingInfo();
        query.PageInfo.PageNumber = 1;
        query.PageInfo.Count = 5000;

        while (true)
        {
            var page = _service.RetrieveMultiple(query);

            results.AddRange(page.Entities.Select(x => x.ToEntity<TEntity>()));

            if (!page.MoreRecords)
            {
                break;
            }

            query.PageInfo.PageNumber++;
            query.PageInfo.PagingCookie = page.PagingCookie;
        }

        return results;
    }

    public EntityCollection Get(QueryBase query)
    {
        if (query == null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        Trace(nameof(Get), "QueryBase", query.GetType().Name);

        return _service.RetrieveMultiple(query);
    }

    public IReadOnlyList<TEntity> Get<TEntity>(
        IEnumerable<Guid> ids,
        ColumnSet columnSet)
        where TEntity : Entity, new()
    {
        if (columnSet == null)
        {
            throw new ArgumentNullException(nameof(columnSet));
        }

        var validIds = ids?
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray() ?? Array.Empty<Guid>();

        if (validIds.Length == 0)
        {
            return Array.Empty<TEntity>();
        }

        var query = new QueryExpression(GetLogicalName<TEntity>())
        {
            ColumnSet = columnSet
        };

        query.Criteria.AddCondition(
            GetPrimaryIdAttribute<TEntity>(),
            ConditionOperator.In,
            validIds.Cast<object>().ToArray());

        return Get<TEntity>(query);
    }

    public IReadOnlyList<TEntity> Get<TEntity>(
        IEnumerable<Guid> ids,
        params Expression<Func<TEntity, object>>[] columns)
        where TEntity : Entity, new()
    {
        return Get<TEntity>(ids, ColumnSetFactory.Create(columns));
    }

    public bool Exists<TEntity>(Guid id)
        where TEntity : Entity, new()
    {
        if (id == Guid.Empty)
        {
            return false;
        }

        var query = new QueryExpression(GetLogicalName<TEntity>())
        {
            TopCount = 1,
            ColumnSet = new ColumnSet(false)
        };

        query.Criteria.AddCondition(
            GetPrimaryIdAttribute<TEntity>(),
            ConditionOperator.Equal,
            id);

        return _service.RetrieveMultiple(query).Entities.Count > 0;
    }

    public bool Exists<TEntity>(QueryExpression query)
        where TEntity : Entity, new()
    {
        EnsureQuery<TEntity>(query);

        query.TopCount = 1;
        query.ColumnSet = new ColumnSet(false);

        return _service.RetrieveMultiple(query).Entities.Count > 0;
    }

    public int Count(QueryExpression query)
    {
        if (query == null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        query.PageInfo ??= new PagingInfo();
        query.PageInfo.PageNumber = 1;
        query.PageInfo.Count = 5000;
        query.ColumnSet = new ColumnSet(false);

        var total = 0;

        while (true)
        {
            var page = _service.RetrieveMultiple(query);
            total += page.Entities.Count;

            if (!page.MoreRecords)
            {
                break;
            }

            query.PageInfo.PageNumber++;
            query.PageInfo.PagingCookie = page.PagingCookie;
        }

        return total;
    }

    public Guid Create(Entity entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        Trace(nameof(Create), entity.LogicalName, string.Empty);

        return _service.Create(entity);
    }

    public void Update(Entity entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        Trace(nameof(Update), entity.LogicalName, entity.Id.ToString());

        _service.Update(entity);
    }

    public void Delete<TEntity>(Guid id)
        where TEntity : Entity, new()
    {
        if (id == Guid.Empty)
        {
            return;
        }

        var logicalName = GetLogicalName<TEntity>();

        Trace(nameof(Delete), logicalName, id.ToString());

        _service.Delete(logicalName, id);
    }

    public void Delete(EntityReference reference)
    {
        if (reference == null || reference.Id == Guid.Empty)
        {
            return;
        }

        Trace(nameof(Delete), reference.LogicalName, reference.Id.ToString());

        _service.Delete(reference.LogicalName, reference.Id);
    }

    public UpsertResponse Upsert(Entity entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        Trace(nameof(Upsert), entity.LogicalName, entity.Id.ToString());

        return (UpsertResponse)_service.Execute(new UpsertRequest
        {
            Target = entity
        });
    }

    public OrganizationResponse Execute(OrganizationRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        Trace(nameof(Execute), request.RequestName, string.Empty);

        return _service.Execute(request);
    }

    private static string GetLogicalName<TEntity>()
        where TEntity : Entity, new()
    {
        return new TEntity().LogicalName;
    }

    private static string GetPrimaryIdAttribute<TEntity>()
        where TEntity : Entity, new()
    {
        return $"{GetLogicalName<TEntity>()}id";
    }

    private static void EnsureQuery<TEntity>(QueryExpression query)
        where TEntity : Entity, new()
    {
        if (query == null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        var expectedLogicalName = GetLogicalName<TEntity>();

        if (!string.Equals(query.EntityName, expectedLogicalName, StringComparison.Ordinal))
        {
            throw new InvalidPluginExecutionException(
                $"Query entity '{query.EntityName}' does not match expected entity '{expectedLogicalName}'.");
        }
    }

    private static void ValidateAlternateKey(KeyAttributeCollection keyAttributes)
    {
        if (keyAttributes == null || keyAttributes.Count == 0)
        {
            throw new InvalidPluginExecutionException(
                "Alternate key attributes must contain at least one value.");
        }
    }

    private void Trace(string operation, string target, string details)
    {
        _tracing.Trace(
            "Service.{0} | Target={1} | Details={2}",
            operation,
            target,
            details);
    }
}
