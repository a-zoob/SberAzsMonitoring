# 1. Этап выполнения рантайма .NET 10
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS base
WORKDIR /app
EXPOSE 8080

# 2. Этап компиляции приложения внутри SDK-контейнера
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем файлы проектов для восстановления зависимостей (кэширование слоев)
COPY ["src/SberAzsMonitoring.Dashboard.Api/SberAzsMonitoring.Dashboard.Api.csproj", "src/SberAzsMonitoring.Dashboard.Api/"]
COPY ["src/SberAzsMonitoring.Dashboard.Application/SberAzsMonitoring.Dashboard.Application.csproj", "src/SberAzsMonitoring.Dashboard.Application/"]
COPY ["src/SberAzsMonitoring.Dashboard.Domain/SberAzsMonitoring.Dashboard.Domain.csproj", "src/SberAzsMonitoring.Dashboard.Domain/"]
COPY ["src/SberAzsMonitoring.Dashboard.Infrastructure/SberAzsMonitoring.Dashboard.Infrastructure.csproj", "src/SberAzsMonitoring.Dashboard.Infrastructure/"]

# Восстанавливаем пакеты
RUN dotnet restore "src/SberAzsMonitoring.Dashboard.Api/SberAzsMonitoring.Dashboard.Api.csproj"

# Копируем весь исходный код решения
COPY . .

# Собираем проект Дашборда
WORKDIR "/src/src/SberAzsMonitoring.Dashboard.Api"
RUN dotnet build "SberAzsMonitoring.Dashboard.Api.csproj" -c Release -o /app/build

# 3. Этап публикации скомпилированных бинарников
FROM build AS publish
RUN dotnet publish "SberAzsMonitoring.Dashboard.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 4. Финальный легковесный контейнер
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SberAzsMonitoring.Dashboard.Api.dll"]
