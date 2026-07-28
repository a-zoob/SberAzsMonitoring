using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SberAzsMonitoring.NotificationWorker.Application.Common.Interfaces;
using SberAzsMonitoring.NotificationWorker.Application.UseCases;
using SberAzsMonitoring.NotificationWorker.Configurations;
using SberAzsMonitoring.NotificationWorker.Infrastructure.Cache;
using SberAzsMonitoring.NotificationWorker.Infrastructure.Notifications;
using SberAzsMonitoring.NotificationWorker.Services;

// 1. Инициализируем построитель Generic Host приложения
var builder = Host.CreateApplicationBuilder(args);

// Связываем ENV-переменные из Docker с C# классом
builder.Services.Configure<NotificationWorkerOptions>(
    builder.Configuration.GetSection("NotificationWorkerOptions"));

// Кэш обязан быть Singleton
builder.Services.AddSingleton<ITenantCache, InMemoryTenantCache>();

// Регистрируем стандартный HttpClient для отправщика
builder.Services.AddHttpClient<INotificationSender, NtfyNotificationSender>();

// --- Слой Application ---
builder.Services.AddTransient<UpdateTenantConfigUseCase>();
builder.Services.AddTransient<ProcessFuelSnapshotUseCase>();

// --- Слой Presentation / Hosting ---
builder.Services.AddHostedService<NotificationConsumerService>();

// 2. Сборка готового хоста приложения
var host = builder.Build();


// 3. Запуск воркера в асинхронном режиме выполнения
await host.RunAsync();
