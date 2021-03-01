using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Brandless.AspNetCore.OData.NetTopology.Attributes;
using Brandless.AspNetCore.OData.NetTopology.Mapping;
using Microsoft.Spatial;
using NetTopologySuite.Geometries;
#if NETNEXT
using Microsoft.OData.ModelBuilder;
#else
using Microsoft.AspNet.OData.Builder;
#endif

namespace Brandless.AspNetCore.OData.NetTopology
{
    /// <summary>
    ///     Property mappings betwen OData Microsoft.Spatial types and NTS types
    /// </summary>
    public static class GeographyMappingExtensions
    {
        static GeographyMappingExtensions()
        {
            IgnoreMethod = typeof(GeographyMappingExtensions).GetMethod(
                nameof(Ignore),
                BindingFlags.NonPublic | BindingFlags.Static);
        }

        private static MethodInfo IgnoreMethod { get; }

        /// <summary>
        ///     Maps all OData GeographyPoint to an EF NTS IPoint via convention
        /// </summary>
        /// <param name="builder">The ODataModelBuilder.</param>
        /// <returns>The ODataModelBuilder.</returns>
        public static ODataModelBuilder UseNetTopology(this ODataModelBuilder builder)
        {
            var pairs =
                new Tuple<Type, Type>[]
                {
                    new Tuple<Type, Type>(typeof(Point), typeof(GeographyPoint)),
                    new Tuple<Type, Type>(typeof(LineString), typeof(GeographyLineString)),
                    new Tuple<Type, Type>(typeof(Polygon), typeof(GeographyPolygon))
                };

            foreach (var set in builder.EntitySets)
            {
                var properties = set.ClrType.GetRuntimeProperties().ToDictionary(_ => _.Name);
                foreach (var property in properties)
                {
                    foreach (var pair in pairs)
                    {
                        // Try naming convention
                        var mappedName = $"Edm{property.Key}";
                        if (pair.Item1.IsAssignableFrom(property.Value.PropertyType) &&
                            properties.ContainsKey(mappedName))
                        {
                            var mapped = properties[mappedName];
                            if (pair.Item2.IsAssignableFrom(mapped.PropertyType))
                            {
                                MapSpatial(builder, mapped, property.Value, set.ClrType);
                                continue;
                            }
                        }
                        // Try attribute
                        var mapping = property.Value.GetCustomAttribute<MappedGeographyAttribute>();
                        if (mapping != null)
                        {
                            var mappedProperty = properties[mapping.MappedToPropertyName];
                            if (pair.Item1.IsAssignableFrom(property.Value.PropertyType))
                            {
                                if (pair.Item2.IsAssignableFrom(mappedProperty.PropertyType))
                                {
                                    MapSpatial(builder, mappedProperty, property.Value, set.ClrType);
                                    continue;
                                }
                            }
                            if (pair.Item1.IsAssignableFrom(mappedProperty.PropertyType))
                            {
                                if (pair.Item2.IsAssignableFrom(property.Value.PropertyType))
                                {
                                    MapSpatial(builder, property.Value, mappedProperty, set.ClrType);
                                    continue;
                                }
                            }
                        }
                    }
                }
            }
            return builder;
        }

        /// <summary>
        ///     Map an OData GeographyPoint to an EF NTS IPoint
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="builder">The ODataModelBuilder.</param>
        /// <param name="odataProperty">The Microsoft.Spatial GeographyPoint lambda.</param>
        /// <param name="ntsProperty">The NTS Point lambda.</param>
        /// <returns>The ODataModelBuilder.</returns>
        public static ODataModelBuilder MapSpatial<T>(
            this ODataModelBuilder builder,
            Expression<Func<T, GeographyPoint>> odataProperty,
            Expression<Func<T, Point>> ntsProperty)
            where T : class
        {
            var odataPropertyInfo = PropertySelectorVisitor.GetSelectedProperty(odataProperty);
            var ntsPropertyInfo = PropertySelectorVisitor.GetSelectedProperty(ntsProperty);
            return builder.MapSpatial(odataPropertyInfo, ntsPropertyInfo, typeof(T));
        }

        /// <summary>
        ///     Map an OData GeographyPolygon to an EF NTS IPolygon
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="builder">The ODataModelBuilder.</param>
        /// <param name="odataProperty">The Microsoft.Spatial GeographyPolygon lambda.</param>
        /// <param name="ntsProperty">The NTS Polygon lambda.</param>
        /// <returns>The ODataModelBuilder.</returns>
        public static ODataModelBuilder MapSpatial<T>(
            this ODataModelBuilder builder,
            Expression<Func<T, GeographyPolygon>> odataProperty,
            Expression<Func<T, Polygon>> ntsProperty)
            where T : class
        {
            var odataPropertyInfo = PropertySelectorVisitor.GetSelectedProperty(odataProperty);
            var ntsPropertyInfo = PropertySelectorVisitor.GetSelectedProperty(ntsProperty);
            return builder.MapSpatial(odataPropertyInfo, ntsPropertyInfo, typeof(T));
        }

        /// <summary>
        ///     Map an OData GeographyLineString to an EF NTS ILineString
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="builder">The ODataModelBuilder.</param>
        /// <param name="odataProperty">The Microsoft.Spatial GeographyLineString lambda.</param>
        /// <param name="ntsProperty">The NTS LineString lambda.</param>
        /// <returns>The ODataModelBuilder.</returns>
        public static ODataModelBuilder MapSpatial<T>(
            this ODataModelBuilder builder,
            Expression<Func<T, GeographyLineString>> odataProperty,
            Expression<Func<T, LineString>> ntsProperty)
            where T : class
        {
            var odataPropertyInfo = PropertySelectorVisitor.GetSelectedProperty(odataProperty);
            var ntsPropertyInfo = PropertySelectorVisitor.GetSelectedProperty(ntsProperty);
            return builder.MapSpatial(odataPropertyInfo, ntsPropertyInfo, typeof(T));
        }

        /// <summary>
        ///     Map an OData GeographyPoint to an EF NTS IPoint
        /// </summary>
        /// <param name="builder">The ODataModelBuilder.</param>
        /// <param name="odataPropertyInfo">The Microsoft.Spatial property.</param>
        /// <param name="ntsPropertyInfo">The NTS property.</param>
        /// <param name="clrType">The CLR type for the entity set.</param>
        /// <returns>The ODataModelBuilder.</returns>
        public static ODataModelBuilder MapSpatial(
            this ODataModelBuilder builder,
            PropertyInfo odataPropertyInfo,
            PropertyInfo ntsPropertyInfo,
            Type clrType)
        {
            GeographyMapping.Instance.MapPoint(odataPropertyInfo, ntsPropertyInfo);
            if (odataPropertyInfo.DeclaringType == null)
            {
                throw new ArgumentException(nameof(odataPropertyInfo));
            }

            if (ntsPropertyInfo.DeclaringType == null)
            {
                throw new ArgumentException(nameof(odataPropertyInfo));
            }

            var declaringType = odataPropertyInfo.DeclaringType.IsAssignableFrom(ntsPropertyInfo.DeclaringType)
                ? ntsPropertyInfo.DeclaringType
                : odataPropertyInfo.DeclaringType;

            var configuration = builder.StructuralTypes.First(t => t.ClrType == clrType);
            var primitivePropertyConfiguration = configuration.AddProperty(odataPropertyInfo);
            primitivePropertyConfiguration.Name = ntsPropertyInfo.Name;
            var param = Expression.Parameter(ntsPropertyInfo.DeclaringType);
            var member = Expression.Property(param, ntsPropertyInfo.Name);
            var lambda = Expression.Lambda(member, param);
            IgnoreMethod.MakeGenericMethod(declaringType, ntsPropertyInfo.PropertyType)
                .Invoke(null, new object[] { builder, lambda });
            return builder;
        }

        private static void Ignore<T, TNtsPoint>(ODataModelBuilder builder,
            Expression lambda)
            where T : class
        {
            builder.EntityType<T>().Ignore((Expression<Func<T, TNtsPoint>>)lambda);
        }
    }
}