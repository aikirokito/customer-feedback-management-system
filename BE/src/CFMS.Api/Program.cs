using CFMS.Api.Hubs;
using CFMS.Api.Middleware;
using CFMS.Application.Common.Interfaces;
using CFMS.Application.Extensions;
using CFMS.Domain.Constants;
using CFMS.Infrastructure.Extensions;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// Serilog — structured logging with console + file sinks
// ============================================================
builder.Host.UseSerilog((context, services, config) =>
{
    config
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            path: "logs/cfms-.txt",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30);
});

// ============================================================
// Controllers + JSON options
// ============================================================
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddFluentValidationAutoValidation();

// ============================================================
// Application + Infrastructure layers
// ============================================================
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ============================================================
// JWT Authentication
// ============================================================
var jwtSection = builder.Configuration.GetSection("Jwt");
var secret = jwtSection["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
        ClockSkew = TimeSpan.Zero
    };

    // Support JWT in SignalR query string (hub connections)
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            var userIdValue = context.Principal?.FindFirst(AppClaimTypes.UserId)?.Value;
            if (!Guid.TryParse(userIdValue, out var userId))
            {
                context.Fail("The access token does not contain a valid user identifier.");
                return;
            }

            var unitOfWork = context.HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();
            var user = await unitOfWork.Users.GetByIdAsync(userId, context.HttpContext.RequestAborted);
            if (user == null || !user.IsActive)
            {
                context.Fail("The user account is disabled or no longer exists.");
            }
        }
    };
});

builder.Services.AddAuthorization();

// ============================================================
// CORS — allow React frontend
// ============================================================
var corsSection = builder.Configuration.GetSection("Cors");
var allowedOrigins = corsSection.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required for SignalR
    });
});

// ============================================================
// SignalR
// ============================================================
builder.Services.AddSignalR();

// ============================================================
// Swagger / OpenAPI
// ============================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Customer Feedback Management System API",
        Version = "v1",
        Description = "RESTful API for the CFMS application. Supports JWT authentication and Google OAuth.",
    });

    // Enable JWT bearer token input in Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your-token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ============================================================
// HttpContextAccessor (for CurrentUser extraction)
// ============================================================
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CFMS.Application.Common.Interfaces.IRealTimeNotificationService, CFMS.Api.Services.RealTimeNotificationService>();


// ============================================================
// Build the app
// ============================================================
var app = builder.Build();

// ============================================================
// Middleware pipeline
// ============================================================
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CFMS API v1");
        c.RoutePrefix = string.Empty; // Serve at root
    });
}

app.UseHttpsRedirection();
app.UseCors("ReactFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// SignalR hub endpoint
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
