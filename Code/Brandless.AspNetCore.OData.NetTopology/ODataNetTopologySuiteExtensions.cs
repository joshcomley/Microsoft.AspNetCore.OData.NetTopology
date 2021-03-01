#if NETNEXT
using Microsoft.OData;
using Microsoft.AspNetCore.OData.Query.Expressions;
#else
using Microsoft.OData;
using Microsoft.AspNet.OData.Query.Expressions;
#endif
using Microsoft.Extensions.DependencyInjection;
using ServiceLifetime = Microsoft.OData.ServiceLifetime;

namespace Brandless.AspNetCore.OData.NetTopology
{
    /// <summary>
    /// ODataBuilder extensions
    /// </summary>
    public static class ODataNetTopologySuiteExtensions
    {
        /// <summary>
        /// Use OData route with default route name and route prefix.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddODataNetTopology(this IServiceCollection services)
        {
            services.AddScoped<FilterBinder>(p => new NetTopologyFilterBinder(p));
            return services;
        }

        /// <summary>
        /// Adds NetTopology dependencies to the container builder for OData
        /// </summary>
        /// <param name="containerBuilder"></param>
        /// <returns></returns>
        public static IContainerBuilder AddNetTopology(this IContainerBuilder containerBuilder)
        {
            containerBuilder.AddService<FilterBinder, NetTopologyFilterBinder>(ServiceLifetime.Transient);
            return containerBuilder;
        }
    }
}