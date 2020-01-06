using Microsoft.Spatial;
using NetTopologySuite.Geometries;

namespace Microsoft.AspNetCore.OData.NetTopology.Conversion
{
    /// <summary>
    ///     Performs wrapping between Microsoft.Spatial GeographyPolygon and NTS Polygon
    /// </summary>
    public class PolygonWrapper
    {
        private readonly Polygon _polygon;

        /// <summary>
        ///     NTS &lt;&gt; Microsoft.Spatial Polygon implicit conversion constructor.
        /// </summary>
        /// <param name="polygon">The NTS Polygon to wrap.</param>
        protected PolygonWrapper(Polygon polygon)
        {
            _polygon = polygon;
        }

        /// <summary>
        ///     Cast PolygonWrapper to GeographyPolygon
        /// </summary>
        /// <param name="wrapper">The PolygonWrapper.</param>
        public static implicit operator GeographyPolygon(PolygonWrapper wrapper)
        {
            return wrapper?._polygon?.ToGeographyPolygon();
        }

        /// <summary>
        ///     Cast GeographyPolygon to PolygonWrapper
        /// </summary>
        /// <param name="geographyPolygon">The GeographyPolygon.</param>
        public static implicit operator PolygonWrapper(GeographyPolygon geographyPolygon)
        {
            return new PolygonWrapper(geographyPolygon?.ToNtsPolygon());
        }

        /// <summary>
        ///     Cast PolygonWrapper to NTS Polygon
        /// </summary>
        /// <param name="wrapper">The PolygonWrapper.</param>
        public static implicit operator Polygon(PolygonWrapper wrapper)
        {
            return wrapper?._polygon;
        }

        /// <summary>
        ///     Cast PolygonWrapper to NTS Polygon
        /// </summary>
        /// <param name="polygon">The PolygonWrapper.</param>
        public static implicit operator PolygonWrapper(Polygon polygon)
        {
            return polygon == null ? null : new PolygonWrapper(polygon);
        }
    }
}