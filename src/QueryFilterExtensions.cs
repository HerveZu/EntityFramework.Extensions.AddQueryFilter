using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Query;

namespace EntityFramework.Extensions.AddQueryFilter;

/// <summary>
///     Dark magic, refer to https://gist.github.com/haacked/febe9e88354fb2f4a4eb11ba88d64c24
/// </summary>
public static class ModelBuilderExtensions
{
    private static readonly MethodInfo _convertAndAppendQueryFilterMethod = typeof(ModelBuilderExtensions)
        .GetMethod(nameof(ConvertAndAppendQueryFilter), BindingFlags.NonPublic | BindingFlags.Static)!;

    [PublicAPI]
    public static void AddQueryFilterOnAllEntities<TBaseEntity>(
        this ModelBuilder builder,
        Expression<Func<TBaseEntity, bool>> filterExpression)
    {
        var entityTypes = builder.Model.GetEntityTypes()
            .Where(type => type.BaseType is null)
            .Select(type => type.ClrType)
            .Where(type => typeof(TBaseEntity).IsAssignableFrom(type));

        foreach (var entityType in entityTypes)
        {
            builder.AppendQueryFilter(entityType, filterExpression);
        }
    }

    [PublicAPI]
    public static void AddQueryFilter<TEntity>(
        this EntityTypeBuilder<TEntity> entityTypeBuilder,
        Expression<Func<TEntity, bool>> expression)
        where TEntity : class
    {
        entityTypeBuilder.AppendQueryFilter(expression);
    }

    private static void AppendQueryFilter<TBaseEntity>(
        this ModelBuilder builder,
        Type entityType,
        Expression<Func<TBaseEntity, bool>> filterExpression)
    {
        _convertAndAppendQueryFilterMethod
            .MakeGenericMethod(typeof(TBaseEntity), entityType)
            .Invoke(null, [builder, filterExpression]);
    }

    private static void ConvertAndAppendQueryFilter<TBaseEntity, TEntity>(
        this ModelBuilder builder,
        Expression<Func<TBaseEntity, bool>> filterExpression)
        where TBaseEntity : class
        where TEntity : class, TBaseEntity
    {
        var concreteExpression = filterExpression.Convert<TBaseEntity, TEntity>();

        builder.Entity<TEntity>().AppendQueryFilter(concreteExpression);
    }

    private static void AppendQueryFilter<TEntity>(
        this EntityTypeBuilder entityTypeBuilder,
        Expression<Func<TEntity, bool>> expression)
        where TEntity : class
    {
        var parameterType = Expression.Parameter(entityTypeBuilder.Metadata.ClrType);

        var expressionFilter = ReplacingExpressionVisitor.Replace(
            expression.Parameters.Single(),
            parameterType,
            expression.Body);

        if (entityTypeBuilder.Metadata.GetQueryFilter() is not null)
        {
            var currentQueryFilter = entityTypeBuilder.Metadata.GetQueryFilter()!;

            var currentExpressionFilter = ReplacingExpressionVisitor.Replace(
                currentQueryFilter.Parameters.Single(),
                parameterType,
                currentQueryFilter.Body);

            expressionFilter = Expression.AndAlso(currentExpressionFilter, expressionFilter);
        }

        var lambdaExpression = Expression.Lambda(expressionFilter, parameterType);
        entityTypeBuilder.HasQueryFilter(lambdaExpression);
    }
}