#if NETNEXT
using Microsoft.OData;
using Microsoft.AspNetCore.OData.Query.Expressions;
#else
using Microsoft.OData;
using Microsoft.AspNet.OData.Query.Expressions;
#endif
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceLifetime = Microsoft.OData.ServiceLifetime;

namespace Brandless.AspNetCore.OData.NetTopology
{
    /// <summary>
    /// ODataBuilder extensions
    /// </summary>
    public static class ODataNetTopologySuiteExtensions
    {
        /// <summary>
        /// Adds NetTopology dependencies to the container builder for OData
        /// </summary>
        /// <param name="serviceCollection"></param>
        /// <returns></returns>
        public static IServiceCollection AddNetTopology(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IFilterBinder, NetTopologyFilterBinder>();
            // serviceCollection.Replace(new ServiceDescriptor(typeof()))
            return serviceCollection;
        }
        
        /// <summary>
        /// Adds NetTopology dependencies to the container builder for OData
        /// </summary>
        /// <param name="containerBuilder"></param>
        /// <returns></returns>
        public static IContainerBuilder AddNetTopology(this IContainerBuilder containerBuilder)
        {
            containerBuilder.AddService<IFilterBinder, NetTopologyFilterBinder>(ServiceLifetime.Transient);
            return containerBuilder;
        }
    }
}