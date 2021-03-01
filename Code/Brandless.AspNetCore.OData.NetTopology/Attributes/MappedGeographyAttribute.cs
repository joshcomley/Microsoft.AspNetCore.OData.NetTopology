using System;

namespace Brandless.AspNetCore.OData.NetTopology.Attributes
{
    /// <summary>
    /// Mapping of a geography property
    /// </summary>
    public class MappedGeographyAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        public string MappedToPropertyName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mappedToPropertyName"></param>
        public MappedGeographyAttribute(string mappedToPropertyName)
        {
            MappedToPropertyName = mappedToPropertyName;
        }
    }
}