using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace EntityFramework.Extensions.AddQueryFilter;

internal sealed class ParameterTypeVisitor<TSource, TTarget> : ExpressionVisitor
{
    private readonly Dictionary<int, ReadOnlyCollection<ParameterExpression>> _parameters = new();
    private int _currentLambdaIndex = -1;

    protected override Expression VisitParameter(ParameterExpression node)
    {
        var parameters = _parameters.Count > _currentLambdaIndex
            ? _parameters[_currentLambdaIndex]
            : null;

        var parameter = parameters?.FirstOrDefault(parameter => parameter.Name == node.Name);

        if (parameter is not null)
        {
            return parameter;
        }

        return node.Type == typeof(TSource)
            ? Expression.Parameter(typeof(TTarget), node.Name)
            : node;
    }

    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        _currentLambdaIndex++;

        try
        {
            _parameters[_currentLambdaIndex] = VisitAndConvert(node.Parameters, nameof(VisitLambda));

            return Expression.Lambda(Visit(node.Body), _parameters[_currentLambdaIndex]);
        }

        finally
        {
            _currentLambdaIndex--;
        }
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        return node.Member.DeclaringType == typeof(TSource)
            ? Expression.Property(Visit(node.Expression)!, node.Member.Name)
            : base.VisitMember(node);
    }
}