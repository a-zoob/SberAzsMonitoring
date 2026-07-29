using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SberAzsMonitoring.Dashboard.Api.Endpoints; // Подключаем пространство имен эндпоинтов
using SberAzsMonitoring.Dashboard.Application;
using SberAzsMonitoring.Dashboard.Application.Common.Interfaces;
using SberAzsMonitoring.Dashboard.Application.UseCases;
using SberAzsMonitoring.Dashboard.Infrastructure;
using Scalar.AspNetCore;
using System;
using System.Text.Json;
using SberAzsMonitoring.Dashboard.Api.Endpoints.Analytics;
using SberAzsMonitoring.Dashboard.Application.UseCases.Analytics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();


// Включаем нечувствительность к регистру и camelCase по умолчанию
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

// 1. Подключаем слои Чистой Архитектуры Дашборда к DI-контейнеру
builder.Services.AddDashboardInfrastructure(builder.Configuration);
builder.Services.AddDashboardApplication();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddScoped<GetTenantCredentialsUseCase>();

builder.Services.AddScoped<GetLatestFuelAvailabilityUseCase>();


var app = builder.Build();

app.UseCors();

// 2. Инициализация и сидирование базы данных PostgreSQL при старте микросервиса
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<IDashboardDbContext>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        if (dbContext is Microsoft.EntityFrameworkCore.DbContext efContext)
        {
            await efContext.Database.MigrateAsync();
        }

        await SberAzsMonitoring.Dashboard.Infrastructure.Persistence.DashboardDbContextSeed.SeedDefaultUserAsync(
            dbContext,
            passwordHasher,
            app.Configuration,
            logger);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Критическая ошибка при сидировании СУБД внутри Dashboard.Api.");
    }
}

//if (app.Environment.IsDevelopment())
//{
    // Включаем нативный эндпоинт, который будет отдавать openapi.json
    app.MapOpenApi();

    // Разворачиваем интерактивную документацию Scalar на маршруте /scalar
    app.MapScalarApiReference(options =>
    {
        options.Title = "SberAzs Monitoring Dashboard API";
        options.Theme = ScalarTheme.DeepSpace; // темная тема
        //options.ShowTestRequestButton = true;   // Включает встроенный REST-клиент
        
    });
//}


// 3. Подключаем изолированные эндпоинты через класс-картограф
app.MapAuthEndpoints();
app.MapTenantEndpoints();
app.MapAnalyticsEndpoints();

app.Run();
