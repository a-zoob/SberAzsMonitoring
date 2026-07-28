var builder = WebApplication.CreateBuilder(args);

// Загружаем правила маршрутизации YARP из appsettings.json
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Включаем маппинг обратного прокси
app.MapReverseProxy();

app.Run();
