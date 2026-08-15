using System.Text;
using System.Threading.RateLimiting;
using Amber.Application.Services;
using Amber.Application.Sync.Commands;
using Amber.Application.Users.BackgroundJobs;
using Amber.Application.Users.Queries.GetUserByUsername;
using Amber.Domain.Common.Interfaces;
using Amber.Domain.Sync.Configurations;
using Amber.Domain.Users.ValueObjects;
using Amber.Infrastructure.Database;
using Amber.Infrastructure.Users.Configurations;
using Amber.WebApi;
using Amber.WebApi.Configurations;
using Amber.WebApi.Exceptions;
using Amber.WebApi.Extensions;
using Amber.WebApi.Middlewares;
using Amber.WebApi.Users;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using AsyncKeyedLock;
using EntityFramework.Exceptions.PostgreSQL;
using Grafana.OpenTelemetry;
using Lettermint;
using LiteBus.Commands;
using LiteBus.Extensions.Microsoft.DependencyInjection;
using LiteBus.Queries;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter the JWT token returned by the sign-in/sign-up endpoints.",
        }
    );
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document, null)] = [],
    });
});
builder.Services.AddControllers();

builder.Services.AddRateLimiter(opts =>
{
    static RateLimitPartition<string> CreateRateLimitPartitionFromConfiguration(
        HttpContext context,
        RateLimiterConfiguration configuration
    )
    {
        var options = new TokenBucketRateLimiterOptions
        {
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = configuration.QueueLimit,

            AutoReplenishment = true,
            TokenLimit = configuration.TokenLimit,
            TokensPerPeriod = configuration.TokensPerPeriod,
            ReplenishmentPeriod = TimeSpan.FromSeconds(configuration.ReplenishmentPeriodInSeconds),
        };

        return RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => options
        );
    }
    ;

    opts.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var configuration = builder
            .Configuration.GetSection("GlobalRateLimiterConfiguration")
            .Get<RateLimiterConfiguration>()!;

        return CreateRateLimitPartitionFromConfiguration(context, configuration);
    });

    opts.AddPolicy(
        Constants.Policies.AuthRateLimitingPolicyName,
        context =>
        {
            var configuration = builder
                .Configuration.GetSection("AuthRateLimiterConfiguration")
                .Get<RateLimiterConfiguration>()!;

            return CreateRateLimitPartitionFromConfiguration(context, configuration);
        }
    );

    opts.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsJsonAsync(
            new ProblemDetails { Detail = "Too many requests. Please try again later." },
            ct
        );
    };
});

if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddSimpleConsole(options =>
    {
        options.TimestampFormat = "[yyyy-MM-ddTHH:mm:ssZ] ";
        options.UseUtcTimestamp = false;
    });
}
else
{
    builder
        .Services.AddOpenTelemetry()
        .WithTracing(c => c.UseGrafana())
        .WithMetrics(c => c.UseGrafana());

    builder.Logging.AddOpenTelemetry(o => o.UseGrafana());
}

var jwtConfiguration = builder.Configuration.GetSection("Jwt").Get<JwtConfiguration>()!;
builder.Services.AddSingleton(jwtConfiguration);

var googleAuthConfiguration = builder
    .Configuration.GetSection("GoogleAuth")
    .Get<GoogleAuthConfiguration>()!;
builder.Services.AddSingleton(googleAuthConfiguration);

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.EventsType = typeof(JwtAuthEvents);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtConfiguration.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtConfiguration.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtConfiguration.SecretKey)
            ),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddDbContextPool<AmberContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Database");
    options.UseNpgsql(connectionString);
    // Better exceptions: https://www.nuget.org/packages/EntityFrameworkCore.Exceptions.PostgreSQL
    options.UseExceptionProcessor();
});

builder.Services.AddAllImplementationsForInterface(typeof(IRepository));
builder.Services.AddAllImplementationsForInterface(typeof(IDomainService));
builder.Services.AddAllImplementationsForInterface(typeof(IApplicationService));

builder.Services.AddScoped<JwtAuthEvents>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddSingleton<AsyncKeyedLocker<Username>>();

builder.Services.AddHostedService<InactiveUserCleanupJob>();

builder.Services.AddExceptionHandler<DefaultExceptionHandler<InvalidOperationException>>();
builder.Services.AddExceptionHandler<DefaultExceptionHandler<UnauthorizedException>>();
builder.Services.AddExceptionHandler<DefaultExceptionHandler<InsufficientStorageException>>();
builder.Services.AddExceptionHandler<DefaultExceptionHandler<InternalErrorException>>();

var syncConfigurations = builder
    .Configuration.GetSection("SyncConfigurations")
    .Get<SyncConfiguration>()!;
builder.Services.AddSingleton(syncConfigurations);

builder.Services.AddProblemDetails();
builder
    .Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });
builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();

builder.Services.AddLiteBus(liteBus =>
{
    var appAssembly = typeof(GetUserByUsernameQuery).Assembly;
    liteBus.AddCommandModule(module => module.RegisterFromAssembly(appAssembly));
    liteBus.AddQueryModule(module => module.RegisterFromAssembly(appAssembly));
});

builder.Services.AddLettermint(options =>
{
    options.ApiKey = builder.Configuration["Lettermint:ApiKey"]!;

    if (builder.Environment.IsDevelopment())
    {
        // NOTE: add your email for local development.
        options.EmailWhitelist = ["test@amberapp.dev"];
    }
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AmberContext>();
    if (db.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
    {
        await db.Database.EnsureCreatedAsync();
    }
    else
    {
        await db.Database.MigrateAsync();
    }
}

// Configure the HTTP request pipeline.

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var descriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        foreach (var description in descriptionProvider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json",
                description.GroupName.ToUpperInvariant()
            );
        }
    });
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseExceptionHandler();
app.MapControllers();

app.Run();
