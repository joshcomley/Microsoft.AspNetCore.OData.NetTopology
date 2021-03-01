using System;

namespace Brandless.AspNetCore.OData.NetTopology.Attributes
{
    public class MappedGeographyAttribute : Attribute
    {
        public string MappedToPropertyName { get; set; }

        public MappedGeographyAttribute(string mappedToPropertyName)
        {
            MappedToPropertyName = mappedToPropertyName;
        }
    }
}