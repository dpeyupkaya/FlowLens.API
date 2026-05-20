using FlowLens.Application.Interfaces.External;
using FlowLens.Application.Interfaces.Infrastructure;
using FlowLens.Domain.Repositories;
using FlowLens.Infrastructure.ExternalServices.GitHub;
using FlowLens.Persistence.Context;
using FlowLens.Persistence.Repositories;
using FlowLens.Persistence.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowLens.Persistence
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

          
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IAnalysisLimitService, AnalysisLimitService>();

            return services;
        }
    }
}
