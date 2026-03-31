using Microsoft.Extensions.DependencyInjection;
using System.Reflection;


namespace FlowLens.Application
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // MediatR'ı mevcut assembly'deki tüm Handler'ları bulacak şekilde kaydet
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

            return services;
        }
    }
}
