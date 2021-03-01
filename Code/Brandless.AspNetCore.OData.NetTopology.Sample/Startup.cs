using Brandless.AspNetCore.OData.NetTopology.Sample.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace Brandless.AspNetCore.OData.NetTopology.Sample
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
            services.AddOData(opt =>
            {
                opt
                    .AddModel("odata", GetEdmModel(), _ => _.AddNetTopology())
                    .Count().Filter().Expand().Select().OrderBy().SetMaxTop(5);
            });
            services.AddMvc(_ => _.EnableEndpointRouting = false);
            var connection = @"Server=.;Database=WashingtonSchools;Trusted_Connection=True;ConnectRetryCount=0";
            services.AddDbContext<NetTopologyDbContext>(options => options.UseSqlServer(connection,
                _ => _.UseNetTopologySuite()));
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddODataNetTopology();
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
                endpoints.MapControllerRoute(
                    "default",
                    "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }

        IEdmModel GetEdmModel()
        {
            var odataBuilder = new ODataConventionModelBuilder();
            odataBuilder.EntitySet<Student>("Students");
            odataBuilder.EntitySet<School>("Schools");
            odataBuilder.EntitySet<School2>("Schools2");
            odataBuilder.UseNetTopology();
            return odataBuilder.GetEdmModel();
        }
    }
}