#if NETNEXT
using Microsoft.AspNetCore.OData.Query.Expressions;
#else
#endif
using System.Linq;
using System.Linq.Expressions;

namespace Brandless.AspNetCore.OData.NetTopology.Visitors
{
    internal class GeographyVisitor : ExpressionVisitor
    {
        private GeographyMemberVisitor MemberVisitor { get; } = new GeographyMemberVisitor();

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method == GeographyMethods.GeoDistance)
            {
                var args = ExpressionHelper.ExtractValueFromNullableArguments(node.Arguments).ToArray();
                var methodCallExpression = Expression.Call(
                    MemberVisitor.Visit(args.First()),
                    GeographyMethods.GeoDistanceEf,
                    MemberVisitor.Visit(args.Skip(1).First()));
                return methodCallExpression;
            }

            if (node.Method == GeographyMethods.GeoIntersects)
            {
                var args = ExpressionHelper.ExtractValueFromNullableArguments(node.Arguments).ToArray();
                var methodCallExpression = Expression.Call(
                    MemberVisitor.Visit(args.First()),
                    GeographyMethods.GeoIntersectsEf,
                    MemberVisitor.Visit(args.Skip(1).First()));
                return methodCallExpression;
            }

            if (node.Method == GeographyMethods.GeoLength)
            {
                var args = ExpressionHelper.ExtractValueFromNullableArguments(node.Arguments).ToArray();
                var arg = MemberVisitor.Visit(args.First());
                var methodCallExpression = Expression.Property(
                    arg,
                    GeographyMethods.GeoLengthEf);
                return methodCallExpression;
            }

            return base.VisitMethodCall(node);

        }
    }
}