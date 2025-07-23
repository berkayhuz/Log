#region Usings
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Claims;

using Elastic.Clients.Elasticsearch;

using FluentValidation;

using LogService.API.Middlewares;
using LogService.Application.Abstractions.Caching;
using LogService.Application.Abstractions.Logging;
using LogService.Application.Abstractions.Requests;
using LogService.Application.Behaviors.Pipeline;
using LogService.Application.Features.Logs.Queries.QueryLogsFlexible;
using LogService.Application.Options;
using LogService.Domain.DTOs;
using LogService.Infrastructure.HealthCheck.Extension;
using LogService.Infrastructure.HealthCheck.Metadata;
using LogService.Infrastructure.Services.Caching.Abstractions;
using LogService.Infrastructure.Services.Caching.Extensions;
using LogService.Infrastructure.Services.Caching.Redis;
using LogService.Infrastructure.Services.Elastic.Abstractions;
using LogService.Infrastructure.Services.Elastic.Clients;
using LogService.Infrastructure.Services.Elastic.Health;
using LogService.Infrastructure.Services.Elastic.Indexing;
using LogService.Infrastructure.Services.Fallback.Abstractions;
using LogService.Infrastructure.Services.Fallback.Reprocessing;
using LogService.Infrastructure.Services.Fallback.Writers;
using LogService.Infrastructure.Services.Logging.Abstractions;
using LogService.Infrastructure.Services.Logging.Read;
using LogService.Infrastructure.Services.Logging.Write;
using LogService.Infrastructure.Services.Security;

using MediatR;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;

using Prometheus;

using SharedKernel.Behaviors.Pipeline;
using SharedKernel.Filters;
using SharedKernel.Options;

using StackExchange.Redis;
#endregion

var builder = WebApplication.CreateBuilder(args);
var uri = builder.Configuration["Elasticsearch:Uri"];
var redisConnString = builder.Configuration.GetConnectionString("Redis");

#region Configuration Bindings
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMqSettings"));

builder.Services.Configure<RequestLoggingOptions>(builder.Configuration.GetSection("RequestLoggingOptions"));
#endregion

#region Redis Configuration
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(redisConnString));
#endregion

#region Elasticsearch Configuration
builder.Services.AddHttpClient<IElasticIndexService, ElasticIndexService>(client =>
{
    client.BaseAddress = new Uri(uri);
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddSingleton<ElasticsearchClient>(_ =>
{
    var settings = new ElasticsearchClientSettings(new Uri(uri))
        .DefaultIndex("logservice-logs")
        .DefaultMappingFor<LogEntryDto>(m => m
            .IndexName("logservice-logs")
            .IdProperty(p => p.Id))
        .EnableDebugMode()
        .IncludeServerStackTraceOnError()
        .PrettyJson();

    return new ElasticsearchClient(settings);
});
#endregion

#region Authentication and Authorization
builder.Services.AddSingleton<RsaKeyProvider>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var provider = builder.Services.BuildServiceProvider();
    var rsa = provider.GetRequiredService<RsaKeyProvider>();

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = rsa.PublicKey,
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.NameIdentifier
    };
});

builder.Services.AddAuthorization();
#endregion

#region Model State Customization
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .SelectMany(e => e.Value!.Errors)
            .Select(e => e.ErrorMessage)
            .ToArray();

        return new BadRequestObjectResult(new
        {
            message = "Validation failed",
            errors
        });
    };
});
#endregion

#region Controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddOpenApi();
#endregion

#region MediatR and Pipeline Behaviors
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<QueryLogsFlexibleHandler>());

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestDiagnosticsBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionHandlingBehavior<,>));
#endregion

#region LogService Dependencies
builder.Services.AddScoped<ILogQueryService, LogQueryService>();
builder.Services.AddScoped<IElasticLogClient, ElasticLogClient>();
builder.Services.AddScoped<IResilientLogWriter, ResilientLogWriter>();
builder.Services.AddScoped<ILogEntryWriteService, LogEntryWriteService>();
#endregion

#region Fallback Services
builder.Services.AddSingleton<IFallbackLogWriter, FallbackLogWriter>();
builder.Services.AddSingleton<IFallbackProcessingStateService, FallbackProcessingStateService>();
builder.Services.AddHostedService<FallbackLogReprocessingService>();
#endregion

#region Redis Cache Services
builder.Services.AddSingleton<IStringDistributedCache, StringDistributedCache>();
builder.Services.AddSingleton<ICacheService, RedisCacheService>();
builder.Services.AddSingleton<ICacheRegionSupport, RedisCacheRegionSupport>();

builder.Services.AddScoped<RequireMatchingRoleHeaderFilter>();
#endregion

builder.Services.Configure<BulkLogOptions>(
    builder.Configuration.GetSection("BulkLogOptions"));

builder.Services.AddSingleton<BulkLogEntryWriteService>();
builder.Services.AddSingleton<ILogEntryWriteService>(sp =>
    sp.GetRequiredService<BulkLogEntryWriteService>());
builder.Services.AddHostedService(sp =>
    sp.GetRequiredService<BulkLogEntryWriteService>());

#region HealthChecks
builder.Services.AddHealthChecksUI(setup =>
{
    var healthCheckTypes = Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(t => typeof(IHealthCheck).IsAssignableFrom(t) && !t.IsAbstract);

    foreach (var type in healthCheckTypes)
    {
        var nameAttr = type.GetCustomAttribute<NameAttribute>();
        if (nameAttr is null) continue;

        var path = $"/health/{nameAttr.Name.Replace('_', '-')}";

        setup.AddHealthCheckEndpoint(nameAttr.Name, path);
    }
}).AddInMemoryStorage();

builder.Services.AddAllAnnotatedHealthChecks();
#endregion

#region Miscellaneous
builder.Services.AddHostedService<LogConsumerService>();
builder.Services.AddScoped<IElasticHealthService, ElasticHealthService>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();

builder.Services.Configure<RequestLoggingOptions>(opts =>
{
    opts.MaxBodyLength = 500;
    opts.ExcludedPaths = new[] { "/auth", "/health", "/swagger" };
    opts.ExcludedContentTypes = new[] { "multipart/form-data", "application/octet-stream" };
    opts.LogBody = true;
    opts.MaskSensitiveData = true;
});
#endregion


var app = builder.Build();

#region Development Tools
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
#endregion

#region Middleware Pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseRouting();
app.UseHttpMetrics();

app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<RequestLoggingMiddleware>();
#endregion

#region Endpoints
app.MapAllHealthCheckEndpoints();
app.MapControllers();
#endregion

#region App Run
await app.RunAsync("http://0.0.0.0:80");
#endregion
