using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNet.OData.Query.Expressions;
using Microsoft.AspNetCore.OData.NetTopology.Visitors;
using Microsoft.OData.UriParser;
using Microsoft.Spatial;

namespace Microsoft.AspNetCore.OData.NetTopology
{
    public class NetTopologyFilterBinder : FilterBinder
    {
        private GeographyVisitor GeographyVisitor { get; } = new GeographyVisitor();
        internal const string GeoDistanceFunctionName = "geo.distance";
        internal const string GeoIntersectsFunctionName = "geo.intersects";
        internal const string GeoLengthFunctionName = "geo.length";
        private ODataQuerySettings _querySettings = null;
        public ODataQuerySettings GetQuerySettings()
        {
            if(_querySettings == null)
            {
                _querySettings = GetType().GetProperty("QuerySettings", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(this) as ODataQuerySettings;
            }

            return _querySettings;
        }
        public NetTopologyFilterBinder(IServiceProvider requestContainer) : base(requestContainer)
        {
        }

        public override Expression BindSingleValueFunctionCallNode(SingleValueFunctionCallNode single)
        {
            switch (single.Name)
            {
                case GeoDistanceFunctionName:
                    return BindGeoDistance(single);
                case GeoIntersectsFunctionName:
                    return BindGeoIntersects(single);
                case GeoLengthFunctionName:
                    return BindGeoLength(single);
            }
            return base.BindSingleValueFunctionCallNode(single);
        }

        internal static readonly Expression NullConstant = Expression.Constant(null);
        internal static readonly Expression FalseConstant = Expression.Constant(false);
        internal static readonly Expression TrueConstant = Expression.Constant(true);

        private Expression BindGeoIntersects(SingleValueFunctionCallNode node)
        {
            Contract.Assert("geo.intersects" == node.Name);
            Expression[] arguments = BindArguments(node.Parameters);
            Contract.Assert(arguments.Length == 2 && typeof(Geography).IsAssignableFrom(arguments[0].Type) && typeof(Geography).IsAssignableFrom(arguments[1].Type));
            return GeographyVisitor.Visit(MakeFunctionCall(GeographyMethods.GeoIntersects, arguments));
        }

        private Expression BindGeoDistance(SingleValueFunctionCallNode node)
        {
            Contract.Assert("geo.distance" == node.Name);
            Expression[] arguments = BindArguments(node.Parameters);
            Contract.Assert(arguments.Length == 2 && arguments[0].Type == typeof(GeographyPoint) && arguments[1].Type == typeof(GeographyPoint));
            return GeographyVisitor.Visit(MakeFunctionCall(GeographyMethods.GeoDistance, arguments));
        }

        private Expression BindGeoLength(SingleValueFunctionCallNode node)
        {
            Contract.Assert("geo.length" == node.Name);
            Expression[] arguments = BindArguments(node.Parameters);
            Contract.Assert(arguments.Length == 1 && arguments[0].Type == typeof(GeographyLineString));
            return GeographyVisitor.Visit(MakeFunctionCall(GeographyMethods.GeoLength, arguments));
        }

        private Expression[] BindArguments(IEnumerable<QueryNode> nodes)
        {
            return nodes.OfType<SingleValueNode>().Select(n => Bind(n)).ToArray();
        }

        // creates an expression for the corresponding OData function.
        internal Expression MakeFunctionCall(MemberInfo member, params Expression[] arguments)
        {
            Contract.Assert(member.MemberType == MemberTypes.Property || member.MemberType == MemberTypes.Method);

            IEnumerable<Expression> functionCallArguments = arguments;
            if (GetQuerySettings().HandleNullPropagation == HandleNullPropagationOption.True)
            {
                // we don't have to check if the argument is null inside the function call as we do it already
                // before calling the function. So remove the redundant null checks.
                functionCallArguments = arguments.Select(a => RemoveInnerNullPropagation(a));
            }

            // if the argument is of type Nullable<T>, then translate the argument to Nullable<T>.Value as none
            // of the canonical functions have overloads for Nullable<> arguments.
            functionCallArguments = ExpressionHelper.ExtractValueFromNullableArguments(functionCallArguments);

            Expression functionCall;
            if (member.MemberType == MemberTypes.Method)
            {
                MethodInfo method = member as MethodInfo;
                if (method.IsStatic)
                {
                    functionCall = Expression.Call(null, method, functionCallArguments);
                }
                else
                {
                    functionCall = Expression.Call(functionCallArguments.First(), method, functionCallArguments.Skip(1));
                }
            }
            else
            {
                // property
                functionCall = Expression.Property(functionCallArguments.First(), member as PropertyInfo);
            }

            return CreateFunctionCallWithNullPropagation(functionCall, arguments);
        }

        // we don't have to do null checks inside the function for arguments as we do the null checks before calling
        // the function when null propagation is enabled.
        // this method converts back "arg == null ? null : convert(arg)" to "arg"
        // Also, note that we can do this generically only because none of the odata functions that we support can take null
        // as an argument.
        internal Expression RemoveInnerNullPropagation(Expression expression)
        {
            Contract.Assert(expression != null);

            if (GetQuerySettings().HandleNullPropagation == HandleNullPropagationOption.True)
            {
                // only null propagation generates conditional expressions
                if (expression.NodeType == ExpressionType.Conditional)
                {
                    // make sure to skip the DateTime IFF clause
                    ConditionalExpression conditionalExpr = (ConditionalExpression)expression;
                    if (conditionalExpr.Test.NodeType != ExpressionType.OrElse)
                    {
                        expression = conditionalExpr.IfFalse;
                        Contract.Assert(expression != null);

                        if (expression.NodeType == ExpressionType.Convert)
                        {
                            UnaryExpression unaryExpression = expression as UnaryExpression;
                            Contract.Assert(unaryExpression != null);

                            if (Nullable.GetUnderlyingType(unaryExpression.Type) == unaryExpression.Operand.Type)
                            {
                                // this is a cast from T to Nullable<T> which is redundant.
                                expression = unaryExpression.Operand;
                            }
                        }
                    }
                }
            }

            return expression;
        }

        internal Expression CreateFunctionCallWithNullPropagation(Expression functionCall, Expression[] arguments)
        {
            if (GetQuerySettings().HandleNullPropagation == HandleNullPropagationOption.True)
            {
                Expression test = CheckIfArgumentsAreNull(arguments);

                if (test == FalseConstant)
                {
                    // none of the arguments are/can be null.
                    // so no need to do any null propagation
                    return functionCall;
                }
                else
                {
                    // if one of the arguments is null, result is null (not defined)
                    return
                        Expression.Condition(
                            test: test,
                            ifTrue: Expression.Constant(null, ToNullable(functionCall.Type)),
                            ifFalse: ToNullable(functionCall));
                }
            }
            else
            {
                return functionCall;
            }
        }

        internal static Type ToNullable(Type t)
        {
            if (IsNullable(t))
            {
                return t;
            }
            else
            {
                return typeof(Nullable<>).MakeGenericType(t);
            }
        }

        internal static Expression ToNullable(Expression expression)
        {
            if (!IsNullable(expression.Type))
            {
                return Expression.Convert(expression, ToNullable(expression.Type));
            }

            return expression;
        }


        private static Expression CheckIfArgumentsAreNull(Expression[] arguments)
        {
            if (arguments.Any(arg => arg == NullConstant))
            {
                return TrueConstant;
            }

            arguments =
                arguments
                    .Select(arg => CheckForNull(arg))
                    .Where(arg => arg != null)
                    .ToArray();

            if (arguments.Any())
            {
                return arguments
                    .Aggregate((left, right) => Expression.OrElse(left, right));
            }
            else
            {
                return FalseConstant;
            }
        }

        internal static Expression CheckForNull(Expression expression)
        {
            if (IsNullable(expression.Type) && expression.NodeType != ExpressionType.Constant)
            {
                return Expression.Equal(expression, Expression.Constant(null));
            }
            else
            {
                return null;
            }
        }

        internal static bool IsNullable(Type t)
        {
            if (!t.IsValueType || (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>)))
            {
                return true;
            }

            return false;
        }
    }
}