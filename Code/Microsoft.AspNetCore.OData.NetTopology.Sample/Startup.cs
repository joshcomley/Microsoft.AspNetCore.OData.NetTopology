using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Query.Expressions;
using Microsoft.AspNet.OData.Routing.Conventions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.NetTopology.Sample.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.OData;
using Microsoft.OData.Edm;

namespace Microsoft.AspNetCore.OData.NetTopology.Sample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(_ => _.EnableEndpointRouting = false);
            var connection = @"Server=.;Database=WashingtonSchools;Trusted_Connection=True;ConnectRetryCount=0";
            services.AddDbContext<NetTopologyDbContext>(options => options.UseSqlServer(connection, 
                _ => _.UseNetTopologySuite()));
            var s = services.AddOData().Services;
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            //services.AddODataNetTopologySuite();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            using (var scope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<NetTopologyDbContext>();
                context.Database.Migrate();
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            }); 
            
            app.UseMvc(routeBuilder =>
            {
                routeBuilder.EnableDependencyInjection(builder =>
                {
                    // Replace
                    // IFoo -> FooB
                    //builder.AddService(
                    //    ServiceLifetime.Transient,
                    //    typeof(FilterBinder),
                    //    typeof(NetTopologyFilterBinder));
                });
                routeBuilder.Select().Expand().Filter().OrderBy().MaxTop(null).Count();
                var p = routeBuilder.ServiceProvider.GetService<IPerRouteContainer>();
                var routePrefix = "odata";
                //routeBuilder.MapODataServiceRoute(routePrefix, routePrefix,
                //    builder =>
                //    {
                //        builder.AddService()
                //    });
                routeBuilder.MapODataServiceRouteWithNetTopology(routePrefix, routePrefix, GetEdmModel());
            });
        }

        IEdmModel GetEdmModel()
        {
            var odataBuilder = new ODataConventionModelBuilder();
            odataBuilder.EntitySet<Student>("Students");
            odataBuilder.EntitySet<School>("Schools");
            odataBuilder.UseNetTopology();
            return odataBuilder.GetEdmModel();
        }
    }
}
