using System;
using System.Linq.Expressions;
using System.Reflection;
using Brandless.AspNetCore.OData.NetTopology.Conversion;
using Brandless.AspNetCore.OData.NetTopology.Mapping;
using Microsoft.Spatial;
using NetTopologySuite.Geometries;

namespace Brandless.AspNetCore.OData.NetTopology.Visitors
{
    internal class GeographyMemberVisitor : ExpressionVisitor
    {
        public static string LinqParameterContainerName = "LinqParameterContainer";
        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Member is PropertyInfo info &&
                !node.Expression.Type.Name.Contains(LinqParameterContainerName))
            {
                var mapping = GeographyMapping.Instance.GetMappedProperty(info);
                if (mapping != null)
                {
                    return Expression.Property(node.Expression, mapping.Name);
                }
            }

            switch (node.Expression)
            {
                case ConstantExpression constantExpression
                    when GetFriendlyName(constantExpression.Type) ==
                         GetTypeName<GeographyPoint>() &&
                         Is<GeographyPoint>(constantExpression.Value):
                    {
                        return Convert<GeographyPoint, Point>(node, constantExpression.Value, p => p.ToNtsPoint());
                    }
                case ConstantExpression expression
                    when GetFriendlyName(expression.Type) ==
                         GetTypeName<GeographyPolygon>() &&
                         Is<GeographyPolygon>(expression.Value):
                    {
                        return Convert<GeographyPolygon, Polygon>(node, expression.Value, p => p.ToNtsPolygon());
                    }
                case ConstantExpression expression
                    when GetFriendlyName(expression.Type) ==
                         GetTypeName<GeographyLineString>() &&
                         Is<GeographyLineString>(expression.Value):
                    {
                        return Convert<GeographyLineString, LineString>(node, expression.Value, p => p.ToNtsLineString());
                    }
            }

            return base.VisitMember(node);
        }

        public static string GetFriendlyName(Type type)
        {
            string friendlyName = type.Name;
            if (type.IsGenericType)
            {
                int iBacktick = friendlyName.IndexOf('`');
                if (iBacktick > 0)
                {
                    friendlyName = friendlyName.Remove(iBacktick);
                }
                friendlyName += "<";
                Type[] typeParameters = type.GetGenericArguments();
                for (int i = 0; i < typeParameters.Length; ++i)
                {
                    string typeParamName = GetFriendlyName(typeParameters[i]);
                    friendlyName += (i == 0 ? typeParamName : "," + typeParamName);
                }
                friendlyName += ">";
            }

            return friendlyName;
        }

        private bool Is<T>(object expressionValue)
        {
            var type = expressionValue.GetType();
            return type.GenericTypeArguments != null &&
                   type.GenericTypeArguments.Length == 1 &&
                   type.GenericTypeArguments[0] == typeof(T);
        }

        private static string GetTypeName<T>()
        {
            return $"Typed{LinqParameterContainerName}<{typeof(T).Name}>";
        }

        private Expression Convert<TEdm, TNts>(
            MemberExpression node,
            object container,
            Func<TEdm, TNts> convert)
        {
            var type = container.GetType();
            var property = type.GetProperty("Property");
            if (property != null && property.GetValue(container) is TEdm geographyPoint)
            {
                var ntsPoint = convert(geographyPoint);
                var result = Expression.Property(
                    VisitConstant(
                        Expression.Constant(
                            Activator.CreateInstance(type.GetGenericTypeDefinition()
                                .MakeGenericType(typeof(TNts)), new object[] { ntsPoint })
                        )),
                    node.Member.Name);
                return result;
            }
            return null;
        }
    }
}