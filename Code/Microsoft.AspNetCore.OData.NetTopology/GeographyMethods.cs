using System;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Spatial;
using NetTopologySuite.Geometries;
using Geometry = NetTopologySuite.Geometries.Geometry;

namespace Microsoft.AspNetCore.OData.NetTopology
{
    internal class GeographyMethods
    {
        private static readonly Point DefaultPoint = default(Point);
        private static readonly Geometry DefaultGeometry = default(Geometry);
        private static GeographyLineString _defaultLineString = default(GeographyLineString);
        private static GeographyPoint _defaultPoint = default(GeographyPoint);
        private static Geography _defaultGeography = default(Geography);
        //private static readonly ILineString DefaultLineString = default(ILineString);

        public static readonly MethodInfo GeoIntersects = MethodOf(_ => _defaultGeography.Intersects(default(Geography)));
        public static readonly MethodInfo GeoDistance = MethodOf(_ => _defaultPoint.Distance(default(Geography)));
        public static readonly MethodInfo GeoLength = MethodOf(_ => _defaultLineString.Length());

        public static readonly MethodInfo GeoIntersectsEf =
            MethodOf(_ => DefaultGeometry.Intersects(default(Geometry)));

        public static readonly MethodInfo GeoDistanceEf =
            MethodOf(_ => DefaultPoint.Distance(default(Geometry)));

        public static readonly PropertyInfo GeoLengthEf =
            typeof(Geometry).GetProperty(nameof(Geometry.Length));

        protected static MethodInfo MethodOf<TReturn>(Expression<Func<object, TReturn>> expression)
        {
            return MethodOf(expression as Expression);
        }

        private static MethodInfo MethodOf(Expression expression)
        {
            LambdaExpression lambdaExpression = expression as LambdaExpression;
            Contract.Assert(lambdaExpression != null);
            Contract.Assert(expression.NodeType == ExpressionType.Lambda);
            Contract.Assert(lambdaExpression.Body.NodeType == ExpressionType.Call);
            return (lambdaExpression.Body as MethodCallExpression).Method;
        }
    }
}