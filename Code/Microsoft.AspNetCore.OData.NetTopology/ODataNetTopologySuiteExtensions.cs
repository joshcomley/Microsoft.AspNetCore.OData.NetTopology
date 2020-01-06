using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.OData.Batch;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Query.Expressions;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNet.OData.Routing.Conventions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OData;
using Microsoft.OData.Edm;
using ServiceLifetime = Microsoft.OData.ServiceLifetime;

namespace Microsoft.AspNetCore.OData.NetTopology
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
            public static IServiceCollection AddODataNetTopologySuite(this IServiceCollection services)
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
            var descriptor =
                new ServiceDescriptor(
                    typeof(FilterBinder),
                    typeof(NetTopologyFilterBinder),
                    Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient);
            var field = containerBuilder.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Single(f => typeof(IServiceCollection).IsAssignableFrom(f.FieldType));
            var services = field.GetValue(containerBuilder) as IServiceCollection;
            services.Replace(descriptor);
            return containerBuilder;
        }

        /// <summary>
        /// Maps the specified OData route and the OData route attributes.
        /// </summary>
        /// <param name="builder">The <see cref="IRouteBuilder"/> to add the route to.</param>
        /// <param name="routeName">The name of the route to map.</param>
        /// <param name="routePrefix">The prefix to add to the OData route's path template.</param>
        /// <param name="configureAction">The configuring action to add the services to the root container.</param>
        /// <returns>The added <see cref="ODataRoute"/>.</returns>
        public static ODataRoute MapODataServiceRouteWithNetTopology(this IRouteBuilder builder, string routeName,
            string routePrefix, Action<IContainerBuilder> configureAction)
        {
            return builder.MapODataServiceRoute(routeName, routePrefix, containerBuilder =>
                {
                    configureAction(containerBuilder);
                    containerBuilder.AddNetTopology();
                });
        }

        /// <summary>
        /// Maps the specified OData route and the OData route attributes.
        /// </summary>
        /// <param name="builder">The <see cref="IRouteBuilder"/> to add the route to.</param>
        /// <param name="routeName">The name of the route to map.</param>
        /// <param name="routePrefix">The prefix to add to the OData route's path template.</param>
        /// <param name="model">The EDM model to use for parsing OData paths.</param>
        /// <returns>The added <see cref="ODataRoute"/>.</returns>
        public static ODataRoute MapODataServiceRouteWithNetTopology(this IRouteBuilder builder, string routeName,
            string routePrefix, IEdmModel model)
        {
            return builder.MapODataServiceRoute(routeName, routePrefix, containerBuilder =>
                containerBuilder.AddService(Microsoft.OData.ServiceLifetime.Singleton, sp => model)
                       .AddService<IEnumerable<IODataRoutingConvention>>(Microsoft.OData.ServiceLifetime.Singleton, sp =>
                           ODataRoutingConventions.CreateDefaultWithAttributeRouting(routeName, builder))
                       .AddNetTopology());
        }

        /// <summary>
        /// Maps the specified OData route and the OData route attributes. When the <paramref name="batchHandler"/> is
        /// non-<c>null</c>, it will create a '$batch' endpoint to handle the batch requests.
        /// </summary>
        /// <param name="builder">The <see cref="IRouteBuilder"/> to add the route to.</param>
        /// <param name="routeName">The name of the route to map.</param>
        /// <param name="routePrefix">The prefix to add to the OData route's path template.</param>
        /// <param name="model">The EDM model to use for parsing OData paths.</param>
        /// <param name="batchHandler">The <see cref="ODataBatchHandler"/>.</param>
        /// <returns>The added <see cref="ODataRoute"/>.</returns>
        public static ODataRoute MapODataServiceRouteWithNetTopology(this IRouteBuilder builder, string routeName,
            string routePrefix, IEdmModel model, ODataBatchHandler batchHandler)
        {
            return builder.MapODataServiceRoute(routeName, routePrefix, containerBuilder =>
                containerBuilder.AddService(ServiceLifetime.Singleton, sp => model)
                       .AddService(ServiceLifetime.Singleton, sp => batchHandler)
                       .AddService<IEnumerable<IODataRoutingConvention>>(ServiceLifetime.Singleton, sp =>
                           ODataRoutingConventions.CreateDefaultWithAttributeRouting(routeName, builder))
                       .AddNetTopology());
        }

        /// <summary>
        /// Maps the specified OData route.
        /// </summary>
        /// <param name="builder">The <see cref="IRouteBuilder"/> to add the route to.</param>
        /// <param name="routeName">The name of the route to map.</param>
        /// <param name="routePrefix">The prefix to add to the OData route's path template.</param>
        /// <param name="model">The EDM model to use for parsing OData paths.</param>
        /// <param name="pathHandler">The <see cref="IODataPathHandler"/> to use for parsing the OData path.</param>
        /// <param name="routingConventions">
        /// The OData routing conventions to use for controller and action selection.
        /// </param>
        /// <returns>The added <see cref="ODataRoute"/>.</returns>
        public static ODataRoute MapODataServiceRouteWithNetTopology(this IRouteBuilder builder, string routeName,
            string routePrefix, IEdmModel model, IODataPathHandler pathHandler,
            IEnumerable<IODataRoutingConvention> routingConventions)
        {
            return builder.MapODataServiceRoute(routeName, routePrefix, containerBuilder =>
                containerBuilder.AddService(Microsoft.OData.ServiceLifetime.Singleton, sp => model)
                       .AddService(Microsoft.OData.ServiceLifetime.Singleton, sp => pathHandler)
                       .AddService(Microsoft.OData.ServiceLifetime.Singleton, sp => routingConventions.ToList().AsEnumerable())
                       .AddNetTopology());
        }

        /// <summary>
        /// Maps the specified OData route. When the <paramref name="batchHandler"/> is non-<c>null</c>, it will
        /// create a '$batch' endpoint to handle the batch requests.
        /// </summary>
        /// <param name="builder">The <see cref="IRouteBuilder"/> to add the route to.</param>
        /// <param name="routeName">The name of the route to map.</param>
        /// <param name="routePrefix">The prefix to add to the OData route's path template.</param>
        /// <param name="model">The EDM model to use for parsing OData paths.</param>
        /// <param name="pathHandler">The <see cref="IODataPathHandler" /> to use for parsing the OData path.</param>
        /// <param name="routingConventions">
        /// The OData routing conventions to use for controller and action selection.
        /// </param>
        /// <param name="batchHandler">The <see cref="ODataBatchHandler"/>.</param>
        /// <returns>The added <see cref="ODataRoute"/>.</returns>
        public static ODataRoute MapODataServiceRouteWithNetTopology(this IRouteBuilder builder, string routeName,
            string routePrefix, IEdmModel model, IODataPathHandler pathHandler,
            IEnumerable<IODataRoutingConvention> routingConventions, ODataBatchHandler batchHandler)
        {
            return builder.MapODataServiceRoute(routeName, routePrefix, containerBuilder =>
                containerBuilder.AddService(ServiceLifetime.Singleton, sp => model)
                       .AddService(ServiceLifetime.Singleton, sp => pathHandler)
                       .AddService(ServiceLifetime.Singleton, sp => routingConventions.ToList().AsEnumerable())
                       .AddService(ServiceLifetime.Singleton, sp => batchHandler)
                       .AddNetTopology());
        }
    }
}