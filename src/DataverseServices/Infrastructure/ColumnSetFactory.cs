using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Linq.Expressions;

namespace DataverseServices.Infrastructure;

public static class ColumnSetFactory
{
    public static ColumnSet Create<TEntity>(
        params Expression<Func<TEntity, object>>[] columns)
        where TEntity : Entity, new()
    {
        if (columns == null || columns.Length == 0)
        {
            throw new InvalidPluginExecutionException(
                $"At least one column must be specified for {typeof(TEntity).Name}.");
        }

        return new ColumnSet(columns.Select(GetAttributeName).ToArray());
    }

    private static string GetAttributeName<TEntity>(
        Expression<Func<TEntity, object>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        if (expression.Body is UnaryExpression unaryExpression &&
            unaryExpression.Operand is MemberExpression unaryMemberExpression)
        {
            return unaryMemberExpression.Member.Name;
        }

        throw new InvalidPluginExecutionException(
            $"Unsupported column expression for {typeof(TEntity).Name}.");
    }
}
