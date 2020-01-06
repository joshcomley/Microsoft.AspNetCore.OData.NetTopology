using Microsoft.Spatial;
using NetTopologySuite.Geometries;

namespace Microsoft.AspNetCore.OData.NetTopology.Conversion
{
    /// <summary>
    ///     Performs wrapping between Microsoft.Spatial GeographyLineString and NTS LineString
    /// </summary>
    public class LineStringWrapper
    {
        private readonly LineString _lineString;

        /// <summary>
        ///     NTS &lt;&gt; Microsoft.Spatial LineString implicit conversion constructor.
        /// </summary>
        /// <param name="lineString">The NTS LineString to wrap.</param>
        protected LineStringWrapper(LineString lineString)
        {
            _lineString = lineString;
        }

        /// <summary>
        ///     Cast LineStringWrapper to GeographyLineString
        /// </summary>
        /// <param name="wrapper">The LineStringWrapper.</param>
        public static implicit operator GeographyLineString(LineStringWrapper wrapper)
        {
            return wrapper?._lineString?.ToGeographyLineString();
        }

        /// <summary>
        ///     Cast GeographyLineString to LineStringWrapper
        /// </summary>
        /// <param name="geographyLineString">The GeographyLineString.</param>
        public static implicit operator LineStringWrapper(GeographyLineString geographyLineString)
        {
            return new LineStringWrapper(geographyLineString?.ToNtsLineString());
        }

        /// <summary>
        ///     Cast LineStringWrapper to NTS LineString
        /// </summary>
        /// <param name="wrapper">The LineStringWrapper.</param>
        public static implicit operator LineString(LineStringWrapper wrapper)
        {
            return wrapper?._lineString;
        }

        /// <summary>
        ///     Cast LineStringWrapper to NTS LineString
        /// </summary>
        /// <param name="lineString">The LineStringWrapper.</param>
        public static implicit operator LineStringWrapper(LineString lineString)
        {
            return lineString == null ? null : new LineStringWrapper(lineString);
        }
    }
}