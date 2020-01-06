using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Implementation;

namespace Microsoft.AspNetCore.OData.NetTopology.Sample.Models
{
    internal class DynamicModelCacheKeyFactory : IModelCacheKeyFactory
    {
        public DynamicModelCacheKeyFactory()
        {

        }
        public object Create(DbContext context)
        {
            //if (context is SqlContext dynamicContext)
            //{
            //    return (context.GetType(), dynamicContext._roleCategory);
            //}
            return ((NetTopologyDbContext)context).Id.ToString();
        }
    }
    public class NetTopologyDbContext : DbContext
    {
        public string Filter = null;
        public NetTopologyDbContext(DbContextOptions<NetTopologyDbContext> options, IServiceProvider serviceProvider) : base(options)
        {
            Database.Migrate();
            //            EnsureSchools();
            //this.Accessor = serviceProvider.GetService<IHttpContextAccessor>();
            //if (Accessor != null)
            //{
            //    this.Filter = Accessor.HttpContext.Request.Query["filter"];
            //}
        }

        public IHttpContextAccessor Accessor
        {
            get => _accessor;
            set
            {
                _accessor = value;
                if (value != null)
                {
                    Filter = value.HttpContext.Request.Query["filter"];
                }
                else
                {
                    Filter = null;
                }

            }
        }

        public Guid Id { get; set; } = Guid.NewGuid();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ReplaceService<IModelCacheKeyFactory, DynamicModelCacheKeyFactory>();
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<Student>().HasQueryFilter(_ => Filter == null ? true : _.Name.Contains(Filter));
            modelBuilder.Entity<School>().HasQueryFilter(_ => Filter == null ? true : _.Name.Contains(Filter));
            base.OnModelCreating(modelBuilder);
        }

        private void EnsureSchools()
        {
            EnsureSchool("a92e215b-0dde-4b27-9e9d-186f11579d19", school =>
            {
                school.Name = "Bromsgrove";
                school.State = "Worcestershire";
                school.City = "Bromsgrove";
                school.Location = new Point(-2.0679985, 52.3293925);
                school.Line = GetLine(
                    new Coordinate(0.3207012, 51.2107298),
                    new Coordinate(0.0020977, 51.6491037),
                    new Coordinate(-0.2505877, 51.6320586),
                    new Coordinate(-0.3549578, 51.5467371),
                    new Coordinate(-0.3494647, 51.3961818),
                    new Coordinate(-0.3131103, 51.2550395),
                    new Coordinate(0.0460430, 51.3516044),
                    new Coordinate(-0.0198748, 51.2966803)
                );
                school.Polygon = GetPolygon(
                    new Coordinate(0.3207012, 51.2107298),
                    new Coordinate(0.0020977, 51.6491037),
                    new Coordinate(-0.2505877, 51.6320586),
                    new Coordinate(-0.3549578, 51.5467371),
                    new Coordinate(-0.3494647, 51.3961818),
                    new Coordinate(-0.3131103, 51.2550395),
                    new Coordinate(0.0460430, 51.3516044),
                    new Coordinate(-0.0198748, 51.2966803)
                );
            });
            EnsureSchool("2e888590-40b7-4ada-bdaf-2e9450609358", school =>
            {
                school.Name = "Polish School";
                school.State = "Plonsk";
                school.City = "Big City";
                school.Line = GetLine(
                    new Coordinate(-2.5652017468750046, 54.25162959694112),
                    new Coordinate(-2.6750650281250046, 53.92945423580718),
                    new Coordinate(-2.3235025281250046, 53.94238931142695),
                    new Coordinate(-1.8840494031250046, 54.27729562668008)
                    );
                school.Location = new Point(20.3576398, 52.6245802);
                school.Polygon = GetPolygon(
                    new Coordinate(-2.5652017468750046, 54.25162959694112),
                    new Coordinate(-2.6750650281250046, 53.92945423580718),
                    new Coordinate(-2.3235025281250046, 53.94238931142695),
                    new Coordinate(-1.8840494031250046, 54.27729562668008)
                );
            });
        }

        private readonly GeometryFactory _geomFactory = new GeometryFactory(new PrecisionModel(), 4326);
        private IHttpContextAccessor _accessor;

        private LineString GetLine(params Coordinate[] coordinates)
        {
            var lineString = new LineString(new CoordinateArraySequence(coordinates), _geomFactory);
            return lineString;
        }
        private Polygon GetPolygon(params Coordinate[] coordinates)
        {
            var list = coordinates.ToList();
            list.Add(coordinates[0]);
            var polygon = new Polygon(new LinearRing(
                list.ToArray()), _geomFactory);
            return polygon;
        }
        private void EnsureSchool(string schoolId, Action<School> action)
        {
            var school =
                Schools.SingleOrDefault(_ => _.SchoolId == new Guid(schoolId));
            if (school == null)
            {
                school = new School();
                Add(school);
            }
            action(school);
            SaveChanges();
        }

        public DbSet<School> Schools { get; set; }
        public DbSet<Student> Students { get; set; }
    }
}
