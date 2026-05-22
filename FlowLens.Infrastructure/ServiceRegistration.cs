using FlowLens.API.Services;
using FlowLens.Application.Common.Interfaces;
using FlowLens.Application.Interfaces;
using FlowLens.Application.Interfaces.Auth;
using FlowLens.Application.Interfaces.External;
using FlowLens.Application.Interfaces.Infrastructure;
using FlowLens.Infrastructure.Analysis.Core;
using FlowLens.Infrastructure.Auth;
using FlowLens.Infrastructure.Services;
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

            services.AddScoped<IAnalysisProgressService, AnalysisProgressService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IEncryptionService,EncryptionService>();
            services.AddScoped<RoslynAnalyzerEngine>();
            services.AddHttpClient<IGitHubService, GitHubService>();
            services.AddSignalR();
            services.AddScoped<ICodeAnalyzerService, RoslynAnalyzerService>();
            services.AddHostedService<DailyLimitResetWorker>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            return services;
        }
    }
}
