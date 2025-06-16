using CurrencyConverter.API.Authorization.Filters;
using CurrencyConverter.API.Filters;
using CurrencyConverter.API.Middlewares;
using CurrencyConverter.Cache.Configuration;
using CurrencyConverter.Domain.Configuration;
using CurrencyConverter.ExchangeRate.Configuration;
using CurrencyConverter.Models.Configurations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.

builder.Services.AddScoped<ValidateModelFilter>();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidateModelFilter>();
});

builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.ReportApiVersions = true;
});

builder.Services.AddEndpointsApiExplorer();

AddSwagger(builder);

builder.Services.AddMemoryCache();

builder.Services.Configure<ExchangeRateSettings>(builder.Configuration.GetSection("ExchangeRate"));

AddAuthentication(builder);
RegisterBindings(builder);

AddRateLimiter(builder);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options => options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi2_0);
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Currency Exchange API v1");
    });
}

app.UseHttpsRedirection();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();


app.MapControllers().RequireRateLimiting("fixed");

app.Run();

static void RegisterBindings(WebApplicationBuilder builder)
{
    ProviderBindings.Register(builder.Services, builder.Configuration);
    DomainBindings.Register(builder.Services);
    CacheBindings.Register(builder.Services);
}

static void AddAuthentication(WebApplicationBuilder builder)
{
    var jwtKey = builder.Configuration["Jwt:Key"];
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

    builder.Services.AddScoped<PermissionFilter>();

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,// For development purposes, you might want to set this to true in production
                ValidateAudience = false,// For development purposes, you might want to set this to true in production
                ValidateLifetime = true
            };
        });

    builder.Services.AddAuthorization();
}

static void AddSwagger(WebApplicationBuilder builder)
{
    builder.Services.AddSwaggerGen(options =>
    {
        options.EnableAnnotations();
        options.SwaggerDoc("v1", new() { Title = "Currency Exchange API", Version = "v1" });

        var jwtScheme = new OpenApiSecurityScheme
        {
            Scheme = "bearer",
            BearerFormat = "JWT",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Description = "JWT Authorization header using the Bearer scheme.",
            Reference = new OpenApiReference
            {
                Id = JwtBearerDefaults.AuthenticationScheme,
                Type = ReferenceType.SecurityScheme
            }
        };

        options.AddSecurityDefinition(jwtScheme.Reference.Id, jwtScheme);
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            { jwtScheme, Array.Empty<string>() }
        });
    });
}

static void AddRateLimiter(WebApplicationBuilder builder)
{
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.ContentType = "application/json";

            var response = new
            {
                statusCode = 429,
                errorCode = "rate_limit_exceeded",
                message = "Too many requests. Please wait and try again later."
            };

            var json = System.Text.Json.JsonSerializer.Serialize(response);

            await context.HttpContext.Response.WriteAsync(json, token);
        };

        options.AddFixedWindowLimiter("fixed", config =>
        {
            config.PermitLimit = 100;                  // max 10 requests
            config.Window = TimeSpan.FromSeconds(60); // per 60 seconds
            config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            config.QueueLimit = 0; // no queuing
        });
    });
}

public partial class Program { }
