using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Spatial;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Implementation;
using Geometry = NetTopologySuite.Geometries.Geometry;
using GeometryFactory = NetTopologySuite.Geometries.GeometryFactory;
using Coordinate = NetTopologySuite.Geometries.Coordinate;

namespace Microsoft.AspNetCore.OData.NetTopology.Conversion
{
    /// <summary>
    ///     Geography extensions for conversion between Microsoft.Spatial and NTS types
    /// </summary>
    public static class GeographyExtensions
    {
        private const int Srid = 4326;
        private static GeometryFactory GeographyFactory { get; }
            = new GeometryFactory(new PrecisionModel(), Srid);
        /// <summary>
        ///     Converts an NTS LineString to a Microsoft.Spatial GeogaphyLineString.
        /// </summary>
        /// <param name="lineString">The NTS LineString.</param>
        /// <returns></returns>
        public static GeographyLineString ToGeographyLineString(this Geometry lineString)
        {
            if (lineString == null)
            {
                return null;
            }

            Debug.Assert(lineString.GeometryType == "LineString");
            var builder = SpatialBuilder.Create();
            var pipeLine = builder.GeographyPipeline;
            pipeLine.SetCoordinateSystem(CoordinateSystem.DefaultGeography);
            pipeLine.BeginGeography(SpatialType.LineString);

            var numPionts = lineString.NumPoints;
            for (var n = 0; n < numPionts; n++)
            {
                var pointN = lineString.GetGeometryN(n + 1);
                var lat = pointN.Coordinate.Y;
                var lon = pointN.Coordinate.X;
                var alt = pointN.Coordinate.Z;
                var m = pointN.Length;
                var position = new GeographyPosition(lat, lon, alt, m);
                if (n == 0)
                {
                    pipeLine.BeginFigure(position);
                }
                else
                {
                    pipeLine.LineTo(position);
                }
            }

            pipeLine.EndFigure();
            pipeLine.EndGeography();
            return (GeographyLineString)builder.ConstructedGeography;
        }

        /// <summary>
        ///     Converts a Microsoft.Spatial GeographyLineString to an NTS LineString.
        /// </summary>
        /// <param name="lineString">The Microsoft.Spatial GeographyLineString.</param>
        /// <returns></returns>
        public static LineString ToNtsLineString(this GeographyLineString lineString)
        {
            if (lineString == null)
            {
                return null;
            }

            var coords = new List<Coordinate>();
            foreach (var coord in lineString.Points)
            {
                coords.Add(new Coordinate(coord.Longitude, coord.Latitude));
            }
            var ntsLineString = GeographyFactory.CreateLineString(coords.ToArray());
            return (LineString)ntsLineString;
        }

        /// <summary>
        ///     Converts an NTS Point to a Microsoft.Spatial GeogaphyPoint.
        /// </summary>
        /// <param name="point">The NTS Point.</param>
        /// <returns></returns>
        public static GeographyPoint ToGeographyPoint(this Point point)
        {
            if (point == null)
            {
                return null;
            }

            Debug.Assert(point.GeometryType == "Point");
            var lat = point.Y;
            var lon = point.X;
            var alt = point.Z;
            var m = point.Length;
            return GeographyPoint.Create(lat, lon, alt, m);
        }

        /// <summary>
        ///     Converts a Microsoft.Spatial GeographyPoint to an NTS Point.
        /// </summary>
        /// <param name="geographyPoint">The Microsoft.Spatial GeographyPoint.</param>
        /// <returns></returns>
        public static Point ToNtsPoint(this GeographyPoint geographyPoint)
        {
            if (geographyPoint == null)
            {
                return null;
            }

            var lat = geographyPoint.Latitude;
            var lon = geographyPoint.Longitude;
            var coord = new Coordinate(lon, lat);
            return (Point)GeographyFactory.CreatePoint(coord);
        }

        /// <summary>
        ///     Converts a Microsoft.Spatial GeographyPolygon to a Polygon.
        /// </summary>
        /// <param name="geographyPolygon">The Microsoft.Spatial GeographyPolygon.</param>
        /// <returns></returns>
        public static Polygon ToNtsPolygon(this GeographyPolygon geographyPolygon)
        {
            if (geographyPolygon == null)
            {
                return null;
            }

            var coords = new List<Coordinate>();
            foreach (var ring in geographyPolygon.Rings)
            {
                foreach (var coord in ring.Points)
                {
                    coords.Add(new Coordinate(coord.Longitude, coord.Latitude));
                }
            }

            var geomFactory = new GeometryFactory(new PrecisionModel(), 4326);
            coords.RemoveAt(coords.Count - 1);
            //coords.Sort(new CoordinateComparer(CalculateCentre(coords)));
            var first = coords.First();
            coords.Add(new Coordinate(first.X, first.Y));
            var sequence = new CoordinateArraySequence(coords.ToArray());
            var poly = new Polygon(
                new LinearRing(sequence, geomFactory),
                geomFactory);
            // Reverse if this is counter clock-wise
            if (poly.Shell.IsCCW)
            {
                poly.Shell.Reverse();
            }
            return poly;
        }

        /// <summary>
        ///     Converts an NTS Polygon to a Microsoft.Spatial GeographyPolygon
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns></returns>
        public static GeographyPolygon ToGeographyPolygon(this Polygon polygon)
        {
            if (polygon == null)
            {
                return null;
            }
            var builder = SpatialImplementation.CurrentImplementation.CreateBuilder();
            builder.GeographyPipeline.SetCoordinateSystem(CoordinateSystem.DefaultGeography);
            builder.GeographyPipeline.BeginGeography(SpatialType.Polygon);
            var exteriorRing = polygon.ExteriorRing;
            if (!new LinearRing(exteriorRing.Coordinates).IsCCW)
            {
                exteriorRing = (LineString)exteriorRing.Reverse();
            }
            BuildRing(exteriorRing, builder);
            foreach (var ring in polygon.Holes)
            {
                var ringCopy = ring;
                if (ringCopy.IsCCW)
                {
                    ringCopy = (LinearRing)ringCopy.Reverse();
                }
                BuildRing(ringCopy, builder);
            }

            builder.GeographyPipeline.EndGeography();
            return (GeographyPolygon)builder.ConstructedGeography;
        }

        private static void BuildRing(Geometry ring, SpatialPipeline builder)
        {
            if (ring == null)
            {
                return;
            }

            var coords = ring.Coordinates.ToList();
            coords.RemoveAt(coords.Count - 1);
            var first = coords.First();
            builder.GeographyPipeline.BeginFigure(new GeographyPosition(first.Y, first.X, first.Z, null));
            for (var i = 1; i < coords.Count; i++)
            {
                var next = coords[i];
                builder.GeographyPipeline.LineTo(new GeographyPosition(next.Y, next.X, next.Z, null));
            }

            builder.GeographyPipeline.LineTo(new GeographyPosition(first.Y, first.X, first.Z, null));
            builder.GeographyPipeline.EndFigure();
        }
    }
}