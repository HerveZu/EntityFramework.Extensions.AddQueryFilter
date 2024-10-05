using System.Linq.Expressions;

namespace EntityFramework.Extensions.AddQueryFilter;

internal static class ExpressionExtensions
{
    public static Expression<Func<TTarget, bool>> Convert<TSource, TTarget>(this Expression<Func<TSource, bool>> root)
    {
        var visitor = new ParameterTypeVisitor<TSource, TTarget>();
        return (Expression<Func<TTarget, bool>>)visitor.Visit(root);
    }
}