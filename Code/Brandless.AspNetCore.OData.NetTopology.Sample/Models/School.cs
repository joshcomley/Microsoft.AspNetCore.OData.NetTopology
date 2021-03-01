using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Brandless.AspNetCore.OData.NetTopology.Attributes;
using Brandless.AspNetCore.OData.NetTopology.Conversion;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Spatial;
using NetTopologySuite.Geometries;

namespace Brandless.AspNetCore.OData.NetTopology.Sample.Models
{
    public class School2:School{}
    public class School
    {
        public Guid SchoolId { get; set; }
        public string Name { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public ICollection<Student> Students { get; set; }

        private PointWrapper _location;
        [ValidateNever]
        public Point Location
        {
            get => _location;
            set => _location = value;
        }

        [NotMapped]
        public GeographyPoint EdmLocation
        {
            get => _location;
            set => _location = value;
        }

        private PolygonWrapper _polygon;
        [ValidateNever]
        [MappedGeography(nameof(MappedPolygon))]
        public Polygon Polygon
        {
            get => _polygon;
            set => _polygon = value;
        }

        [NotMapped]
        public GeographyPolygon MappedPolygon
        {
            get => _polygon;
            set => _polygon = value;
        }

        private LineStringWrapper _line;
        [ValidateNever]
        public LineString Line
        {
            get => _line;
            set => _line = value;
        }

        [NotMapped]
        public GeographyLineString EdmLine
        {
            get => _line;
            set => _line = value;
        }
    }
}
