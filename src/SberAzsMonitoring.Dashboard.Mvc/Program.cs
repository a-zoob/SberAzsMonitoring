using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Настройка постоянного хранения ключей шифрования в Docker Volume
var customKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrEmpty(customKeysPath))
{
    var keysFolder = new DirectoryInfo(customKeysPath);
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(keysFolder);
}

builder.Services.AddControllersWithViews();
builder.Services.AddHealthChecks();

// получаем значение переменной из оркестратора
builder.Services.AddHttpClient("BackendApi", client =>
{
    var baseUrl = builder.Configuration["BackendApiSettings:BaseUrl"] ?? "http://localhost:5003/";
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Analytics}/{action=Index}/{id?}");

app.Run();
