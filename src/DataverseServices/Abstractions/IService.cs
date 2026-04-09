using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System.Linq.Expressions;

namespace DataverseServices.Abstractions;

public interface IService
{
    IOrganizationService Raw { get; }

    TEntity? Get<TEntity>(Guid id, ColumnSet columnSet)
        where TEntity : Entity, new();

    TEntity? Get<TEntity>(
        Guid id,
        params Expression<Func<TEntity, object>>[] columns)
        where TEntity : Entity, new();

    TEntity GetRequired<TEntity>(Guid id, ColumnSet columnSet)
        where TEntity : Entity, new();

    TEntity GetRequired<TEntity>(
        Guid id,
        params Expression<Func<TEntity, object>>[] columns)
        where TEntity : Entity, new();

    TEntity? Get<TEntity>(EntityReference reference, ColumnSet columnSet)
        where TEntity : Entity, new();

    TEntity? Get<TEntity>(
        EntityReference reference,
        params Expression<Func<TEntity, object>>[] columns)
        where TEntity : Entity, new();

    TEntity GetRequired<TEntity>(EntityReference reference, ColumnSet columnSet)
        where TEntity : Entity, new();

    TEntity GetRequired<TEntity>(
        EntityReference reference,
        params Expression<Func<TEntity, object>>[] columns)
        where TEntity : Entity, new();

    TEntity? GetByAlternateKey<TEntity>(
        KeyAttributeCollection keyAttributes,
        ColumnSet columnSet)
        where TEntity : Entity, new();

    TEntity? GetByAlternateKey<TEntity>(
        KeyAttributeCollection keyAttributes,
        params Expression<Func<TEntity, object>>[] columns)
        where TEntity : Entity, new();

    TEntity GetByAlternateKeyRequired<TEntity>(
        KeyAttributeCollection keyAttributes,
        ColumnSet columnSet)
        where TEntity : Entity, new();

    TEntity GetByAlternateKeyRequired<TEntity>(
        KeyAttributeCollection keyAttributes,
        params Expression<Func<TEntity, object>>[] columns)
        where TEntity : Entity, new();

    TEntity? GetFirst<TEntity>(QueryExpression query)
        where TEntity : Entity, new();

    TEntity GetFirstRequired<TEntity>(QueryExpression query)
        where TEntity : Entity, new();

    TEntity? GetSingleOrDefault<TEntity>(QueryExpression query)
        where TEntity : Entity, new();

    TEntity GetSingle<TEntity>(QueryExpression query)
        where TEntity : Entity, new();

    IReadOnlyList<TEntity> Get<TEntity>(QueryExpression query)
        where TEntity : Entity, new();

    IReadOnlyList<TEntity> GetAll<TEntity>(QueryExpression query)
        where TEntity : Entity, new();

    EntityCollection Get(QueryBase query);

    IReadOnlyList<TEntity> Get<TEntity>(
        IEnumerable<Guid> ids,
        ColumnSet columnSet)
        where TEntity : Entity, new();

    IReadOnlyList<TEntity> Get<TEntity>(
        IEnumerable<Guid> ids,
        params Expression<Func<TEntity, object>>[] columns)
        where TEntity : Entity, new();

    bool Exists<TEntity>(Guid id)
        where TEntity : Entity, new();

    bool Exists<TEntity>(QueryExpression query)
        where TEntity : Entity, new();

    int Count(QueryExpression query);

    Guid Create(Entity entity);

    void Update(Entity entity);

    void Delete<TEntity>(Guid id)
        where TEntity : Entity, new();

    void Delete(EntityReference reference);

    UpsertResponse Upsert(Entity entity);

    OrganizationResponse Execute(OrganizationRequest request);
}
