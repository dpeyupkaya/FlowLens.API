using FlowLens.Application;
using FlowLens.Infrastructure;
using FlowLens.Infrastructure.Hubs;
using FlowLens.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection; 
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);


builder.Services.AddDataProtection();

builder.Services.AddCors(options => {
    options.AddPolicy("FlowLensCors", policy => {
        policy.WithOrigins("https://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); 
    });
});

builder.Services.AddControllers();

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

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var securitySettings = builder.Configuration.GetSection("SecuritySettings");

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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]!))
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

                    var protectorKey = securitySettings["CookieEncryptionKey"];
                    var protector = dataProtectionProvider.CreateProtector(protectorKey);

                    var decryptedJwt = protector.Unprotect(cookieToken);
                    context.Token = decryptedJwt;
                }
                catch
                {
                    
                }
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors("FlowLensCors");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<AnalysisHub>("/analysisHub");

app.Run();