# Этап 1: Базовый образ для выполнения приложения
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

# Этап 2: Сборка и восстановление зависимостей
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Копируем файлы проектов для восстановления кэша слоев NuGet
COPY ["src/SberAzsMonitoring.Dashboard.Mvc/SberAzsMonitoring.Dashboard.Mvc.csproj", "src/SberAzsMonitoring.Dashboard.Mvc/"]
COPY ["src/SberAzsMonitoring.Dashboard.Application/SberAzsMonitoring.Dashboard.Application.csproj", "src/SberAzsMonitoring.Dashboard.Application/"]
COPY ["src/SberAzsMonitoring.Dashboard.Domain/SberAzsMonitoring.Dashboard.Domain.csproj", "src/SberAzsMonitoring.Dashboard.Domain/"]

RUN dotnet restore "src/SberAzsMonitoring.Dashboard.Mvc/SberAzsMonitoring.Dashboard.Mvc.csproj"

# Копируем оставшийся исходный код и собираем проект
COPY . .
WORKDIR "/src/src/SberAzsMonitoring.Dashboard.Mvc"
RUN dotnet build "SberAzsMonitoring.Dashboard.Mvc.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Этап 3: Публикация
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SberAzsMonitoring.Dashboard.Mvc.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Этап 4: Финальный контейнер
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SberAzsMonitoring.Dashboard.Mvc.dll"]
