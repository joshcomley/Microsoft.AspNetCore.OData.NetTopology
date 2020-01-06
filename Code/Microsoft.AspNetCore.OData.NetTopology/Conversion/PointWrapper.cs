using Microsoft.Spatial;
using NetTopologySuite.Geometries;

namespace Microsoft.AspNetCore.OData.NetTopology.Conversion
{
    /// <summary>
    ///     Performs wrapping between Microsoft.Spatial GeographyPoint and NTS Point
    /// </summary>
    public class PointWrapper
    {
        private readonly Point _point;

        /// <summary>
        ///     NTS &lt;&gt; Microsoft.Spatial Point implicit conversion constructor.
        /// </summary>
        /// <param name="point">The NTS Point to wrap.</param>
        protected PointWrapper(Point point)
        {
            _point = point;
        }

        /// <summary>
        ///     Cast PointWrapper to GeographyPoint
        /// </summary>
        /// <param name="wrapper">The PointWrapper.</param>
        public static implicit operator GeographyPoint(PointWrapper wrapper)
        {
            return wrapper?._point?.ToGeographyPoint();
        }

        /// <summary>
        ///     Cast GeographyPoint to PointWrapper
        /// </summary>
        /// <param name="geographyPoint">The GeographyPoint.</param>
        public static implicit operator PointWrapper(GeographyPoint geographyPoint)
        {
            return new PointWrapper(geographyPoint.ToNtsPoint());
        }

        /// <summary>
        ///     Cast PointWrapper to NTS Point
        /// </summary>
        /// <param name="wrapper">The PointWrapper.</param>
        public static implicit operator Point(PointWrapper wrapper)
        {
            return wrapper?._point;
        }

        /// <summary>
        ///     Cast NTS Point to PointWrapper
        /// </summary>
        /// <param name="point">The NTS Point.</param>
        public static implicit operator PointWrapper(Point point)
        {
            return point == null ? null : new PointWrapper(point);
        }
    }
}