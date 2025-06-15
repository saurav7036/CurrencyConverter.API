using CurrencyConverter.API.Filters;
using CurrencyConverter.API.Middlewares;
using CurrencyConverter.Cache.Configuration;
using CurrencyConverter.Domain.Configuration;
using CurrencyConverter.ExchangeRate.Configuration;
using CurrencyConverter.Models.Configurations;
using Microsoft.AspNetCore.Mvc;
using Serilog;

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

builder.Services.AddControllers(options=>
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
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();

builder.Services.Configure<ExchangeRateSettings>(builder.Configuration.GetSection("ExchangeRate"));

RegisterBindings(builder);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();

static void RegisterBindings(WebApplicationBuilder builder)
{
    ProviderBindings.Register(builder.Services, builder.Configuration);
    DomainBindings.Register(builder.Services);
    CacheBindings.Register(builder.Services);
}

public partial class Program { }
