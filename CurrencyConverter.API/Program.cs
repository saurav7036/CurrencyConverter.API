using CurrencyConverter.Cache.Configuration;
using CurrencyConverter.Domain.Configuration;
using CurrencyConverter.ExchangeRate.Configuration;
using CurrencyConverter.Models.Configurations;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
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
