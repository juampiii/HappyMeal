using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Happymeal.Common.DomainEvents;

public static class DomainEventServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Registers the dispatcher and every <see cref="IDomainEventHandler{TDomainEvent}"/> found in the given assembly.</summary>
        public IServiceCollection AddDomainEventHandlers(Assembly assembly)
        {
            var catalog = services.GetOrAddCatalog();
            var handlerInterfaceType = typeof(IDomainEventHandler<>);

            var handlerTypes = assembly.GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false });

            foreach (var handlerType in handlerTypes)
            {
                var closedInterfaces = handlerType.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType);

                foreach (var closedInterface in closedInterfaces)
                {
                    services.AddScoped(closedInterface, handlerType);
                    catalog.Add(closedInterface.GetGenericArguments()[0], handlerType);
                }
            }

            services.TryAddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

            return services;
        }

        private DomainEventHandlerCatalog GetOrAddCatalog()
        {
            var existing = services
                .FirstOrDefault(d => d.ServiceType == typeof(IDomainEventHandlerCatalog))
                ?.ImplementationInstance;

            if (existing is DomainEventHandlerCatalog catalog)
            {
                return catalog;
            }

            catalog = new DomainEventHandlerCatalog();
            services.AddSingleton<IDomainEventHandlerCatalog>(catalog);
            return catalog;
        }
    }
}
