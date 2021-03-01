using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Brandless.AspNetCore.OData.NetTopology.Sample
{
    /// <summary>
    /// Example URLs:
    /// https://localhost:44320/odata/schools?$filter=geo.distance(Location, geography'POINT(-122.131577 47.678581)') le 10
    /// https://localhost:44320/odata/schools?$filter=geo.intersects(geography'SRID=4326;POINT(-0.2046947 51.4463192)',Polygon)
    /// https://localhost:44320/odata/schools?$filter=geo.length(Line) gt 10
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
