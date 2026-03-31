using FlowLens.Application.Interfaces.Auth;
using FlowLens.Application.Interfaces.External;
using FlowLens.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowLens.Infrastructure
{

    public static class ServiceRegistration
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

          
            services.AddScoped<ITokenService, TokenService>();

            services.AddHttpClient<IGitHubService, GitHubService>();

            return services;
        }
    }
}
