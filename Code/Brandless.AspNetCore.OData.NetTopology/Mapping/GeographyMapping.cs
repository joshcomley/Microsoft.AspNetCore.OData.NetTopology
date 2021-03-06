﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Brandless.AspNetCore.OData.NetTopology.Extensions;
#if NETNEXT
using Microsoft.AspNetCore.OData;
#else
#endif

namespace Brandless.AspNetCore.OData.NetTopology.Mapping
{
    /// <summary>
    ///     Property mappings betwen OData Microsoft.Spatial types and NTS types
    /// </summary>
    internal class GeographyMapping
    {
        internal static GeographyMapping Instance { get; }
            = new GeographyMapping();

        private Dictionary<PropertyInfo, PropertyInfo> PointMappings { get; set; }

        public PropertyInfo GetMappedProperty(PropertyInfo propertyInfo)
        {
            if (PointMappings.ContainsKey(propertyInfo))
            {
                return PointMappings[propertyInfo];
            }

            return null;
        }

        /// <summary>
        ///     Map an OData GeographyPoint to an EF NTS IPoint
        /// </summary>
        /// <param name="odataProperty"></param>
        /// <param name="ntsPropertyInfo"></param>
        public void MapPoint(PropertyInfo odataProperty, PropertyInfo ntsPropertyInfo)
        {
            PointMappings = PointMappings ?? new Dictionary<PropertyInfo, PropertyInfo>();
            EnsureSameType(odataProperty, ntsPropertyInfo);
            PointMappings.EnsureEntry(odataProperty, ntsPropertyInfo);
        }

        private static void EnsureSameType(MemberInfo propertyInfo1, MemberInfo propertyInfo2)
        {
            if (!propertyInfo1.DeclaringType.IsAssignableFrom(propertyInfo2.DeclaringType) &&
                !propertyInfo2.DeclaringType.IsAssignableFrom(propertyInfo1.DeclaringType))
            {
                throw new ArgumentException("Both properties must be from the same type");
            }
        }
    }
}