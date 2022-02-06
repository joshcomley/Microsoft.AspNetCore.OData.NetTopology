#if NETNEXT
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Expressions;
#else
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNet.OData.Query.Expressions;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Brandless.AspNetCore.OData.NetTopology.Visitors;
using Microsoft.OData.UriParser;
using Microsoft.Spatial;

namespace Brandless.AspNetCore.OData.NetTopology
{
    /// <summary>
    /// 
    /// </summary>
    public class NetTopologyFilterBinder : FilterBinder
    {
        private const string GeoDistanceFunctionName = "geo.distance";
        private const string GeoIntersectsFunctionName = "geo.intersects";
        private const string GeoLengthFunctionName = "geo.length";

        private static readonly Expression NullConstant = Expression.Constant(null);
        private static readonly Expression FalseConstant = Expression.Constant(false);
        private static readonly Expression TrueConstant = Expression.Constant(true);
        private ODataQuerySettings _querySettings;

        private GeographyVisitor GeographyVisitor { get; } = new();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="single"></param>
        /// <returns></returns>
        public override Expression BindSingleValueFunctionCallNode(SingleValueFunctionCallNode single,
            QueryBinderContext context)
        {
            switch (single.Name)
            {
                case GeoDistanceFunctionName:
                    return BindGeoDistance(single, context);
                case GeoIntersectsFunctionName:
                    return BindGeoIntersects(single, context);
                case GeoLengthFunctionName:
                    return BindGeoLength(single, context);
            }

            return base.BindSingleValueFunctionCallNode(single, context);
        }

        private Expression BindGeoIntersects(SingleValueFunctionCallNode node, QueryBinderContext context)
        {
            Contract.Assert("geo.intersects" == node.Name);
            var arguments = BindArguments(node.Parameters, context);
            Contract.Assert(arguments.Length == 2 && typeof(Geography).IsAssignableFrom(arguments[0].Type) &&
                            typeof(Geography).IsAssignableFrom(arguments[1].Type));
            return GeographyVisitor.Visit(MakeFunctionCall(GeographyMethods.GeoIntersects, context, arguments));
        }

        private Expression BindGeoDistance(SingleValueFunctionCallNode node, QueryBinderContext context)
        {
            Contract.Assert("geo.distance" == node.Name);
            var arguments = BindArguments(node.Parameters, context);
            Contract.Assert(arguments.Length == 2 && arguments[0].Type == typeof(GeographyPoint) &&
                            arguments[1].Type == typeof(GeographyPoint));
            return GeographyVisitor.Visit(MakeFunctionCall(GeographyMethods.GeoDistance, context, arguments));
        }

        private Expression BindGeoLength(SingleValueFunctionCallNode node, QueryBinderContext context)
        {
            Contract.Assert("geo.length" == node.Name);
            var arguments = BindArguments(node.Parameters, context);
            Contract.Assert(arguments.Length == 1 && arguments[0].Type == typeof(GeographyLineString));
            return GeographyVisitor.Visit(MakeFunctionCall(GeographyMethods.GeoLength, context, arguments));
        }

        private new Expression[] BindArguments(IEnumerable<QueryNode> nodes, QueryBinderContext context)
        {
            return nodes.OfType<SingleValueNode>().Select(_ => Bind(_, context)).ToArray();
        }

        // creates an expression for the corresponding OData function.
        private Expression MakeFunctionCall(MemberInfo member, QueryBinderContext context,
            params Expression[] arguments)
        {
            Contract.Assert(member.MemberType == MemberTypes.Property || member.MemberType == MemberTypes.Method);

            IEnumerable<Expression> functionCallArguments = arguments;
            if (context.QuerySettings?.HandleNullPropagation == HandleNullPropagationOption.True)
                // we don't have to check if the argument is null inside the function call as we do it already
                // before calling the function. So remove the redundant null checks.
                functionCallArguments = arguments.Select(_ => RemoveInnerNullPropagation(_, context));

            // if the argument is of type Nullable<T>, then translate the argument to Nullable<T>.Value as none
            // of the canonical functions have overloads for Nullable<> arguments.
            functionCallArguments = ExpressionHelper.ExtractValueFromNullableArguments(functionCallArguments);

            Expression functionCall;
            if (member.MemberType == MemberTypes.Method)
            {
                var method = member as MethodInfo;
                functionCall = method.IsStatic
                    ? Expression.Call(null, method, functionCallArguments)
                    : Expression.Call(functionCallArguments.First(), method, functionCallArguments.Skip(1));
            }
            else
            {
                // property
                functionCall = Expression.Property(functionCallArguments.First(), member as PropertyInfo);
            }

            return CreateFunctionCallWithNullPropagation(functionCall, context, arguments);
        }

        // we don't have to do null checks inside the function for arguments as we do the null checks before calling
        // the function when null propagation is enabled.
        // this method converts back "arg == null ? null : convert(arg)" to "arg"
        // Also, note that we can do this generically only because none of the odata functions that we support can take null
        // as an argument.
        private Expression RemoveInnerNullPropagation(Expression expression, QueryBinderContext context)
        {
            Contract.Assert(expression != null);

            if (context.QuerySettings.HandleNullPropagation == HandleNullPropagationOption.True)
                // only null propagation generates conditional expressions
                if (expression.NodeType == ExpressionType.Conditional)
                {
                    // make sure to skip the DateTime IFF clause
                    var conditionalExpr = (ConditionalExpression)expression;
                    if (conditionalExpr.Test.NodeType != ExpressionType.OrElse)
                    {
                        expression = conditionalExpr.IfFalse;
                        Contract.Assert(expression != null);

                        if (expression.NodeType == ExpressionType.Convert)
                        {
                            var unaryExpression = expression as UnaryExpression;
                            Contract.Assert(unaryExpression != null);

                            if (Nullable.GetUnderlyingType(unaryExpression.Type) == unaryExpression.Operand.Type)
                                // this is a cast from T to Nullable<T> which is redundant.
                                expression = unaryExpression.Operand;
                        }
                    }
                }

            return expression;
        }

        private Expression CreateFunctionCallWithNullPropagation(Expression functionCall, QueryBinderContext context,
            Expression[] arguments)
        {
            if (context.QuerySettings.HandleNullPropagation == HandleNullPropagationOption.True)
            {
                var test = CheckIfArgumentsAreNull(arguments);

                if (test == FalseConstant)
                    // none of the arguments are/can be null.
                    // so no need to do any null propagation
                    return functionCall;

                // if one of the arguments is null, result is null (not defined)
                return
                    Expression.Condition(
                        test,
                        Expression.Constant(null, ToNullable(functionCall.Type)),
                        ToNullable(functionCall));
            }

            return functionCall;
        }

        private static Type ToNullable(Type t)
        {
            return IsNullable(t) ? t : typeof(Nullable<>).MakeGenericType(t);
        }

        private static Expression ToNullable(Expression expression)
        {
            return !IsNullable(expression.Type)
                ? Expression.Convert(expression, ToNullable(expression.Type))
                : expression;
        }


        private static Expression CheckIfArgumentsAreNull(Expression[] arguments)
        {
            if (arguments.Any(arg => arg == NullConstant)) return TrueConstant;

            arguments =
                arguments
                    .Select(CheckForNull)
                    .Where(arg => arg != null)
                    .ToArray();

            if (arguments.Any())
                return arguments
                    .Aggregate(Expression.OrElse);

            return FalseConstant;
        }

        private static Expression CheckForNull(Expression expression)
        {
            if (IsNullable(expression.Type) && expression.NodeType != ExpressionType.Constant)
                return Expression.Equal(expression, Expression.Constant(null));

            return null;
        }

        private static bool IsNullable(Type t)
        {
            return !t.IsValueType || t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
    }
}