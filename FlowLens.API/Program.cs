using FlowLens.Application;
using FlowLens.Infrastructure;
using FlowLens.Infrastructure.SignalR;
using FlowLens.Persistence;
using FlowLens.API.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using System.Text;
using System.Threading.RateLimiting;
using FlowLens.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc; // 🚀 EKLENDİ: AutoValidateAntiforgeryTokenAttribute için gerekli

var builder = WebApplication.CreateBuilder(args);

var azurePort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(azurePort))
{
    builder.WebHost.UseUrls($"http://*:{azurePort}");
}

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddDataProtection();
builder.Services.AddHostedService<DailyLimitResetWorker>();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN"; 
});

builder.Services.AddCors(options => {
    options.AddPolicy("FlowLensCors", policy => {
        policy.WithOrigins("https://localhost:5173")
               // policy.WithOrigins("https://flow-lens-ui.vercel.app")
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials();
    });
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddOpenApi(options => {
    options.AddDocumentTransformer((document, context, cancellationToken) => {
        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Name = "Authorization",
            In = ParameterLocation.Header,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Artık cookie tabanlı çalışıyoruz ama Swagger testleri için burası kalabilir."
        };
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes.Add("Bearer", scheme);
        document.SecurityRequirements.Add(new OpenApiSecurityRequirement {
            { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
        });
        return Task.CompletedTask;
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("GlobalIpPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var securitySettings = builder.Configuration.GetSection("SecuritySettings");

var jwtSecret = jwtSettings["Secret"];
if (string.IsNullOrEmpty(jwtSecret))
{
    throw new InvalidOperationException("KRİTİK GÜVENLİK HATASI: 'JwtSettings:Secret' konfigürasyonu bulunamadı! Sistem güvenli olarak ayağa kalkamaz.");
}

var cookieProtectorKey = securitySettings["CookieEncryptionKey"];
if (string.IsNullOrEmpty(cookieProtectorKey))
{
    throw new InvalidOperationException("KRİTİK GÜVENLİK HATASI: 'SecuritySettings:CookieEncryptionKey' konfigürasyonu bulunamadı! Sistem güvenli olarak ayağa kalkamaz.");
}

builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var cookieToken = context.Request.Cookies["_fl_ctx_9x"];

            if (!string.IsNullOrEmpty(cookieToken))
            {
                try
                {
                    var dataProtectionProvider = context.HttpContext.RequestServices.GetRequiredService<IDataProtectionProvider>();
                    var protector = dataProtectionProvider.CreateProtector(cookieProtectorKey);
                    var decryptedJwt = protector.Unprotect(cookieToken);
                    context.Token = decryptedJwt;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FlowLens Auth Hata]: Token çözülemedi: {ex.Message}");
                }
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.None,
    Secure = CookieSecurePolicy.Always,
    HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always
});

app.UseCors("FlowLensCors");
app.UseRateLimiter();

app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<AnalysisHub>("/analysisHub");

app.Run();