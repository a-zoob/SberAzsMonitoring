FROM mcr.microsoft.com/dotnet/sdk:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем файлы проектов для восстановления зависимостей (dotnet restore)
COPY ["src/NotificationWorker/NotificationWorker.csproj", "src/NotificationWorker/"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Domain/Domain.csproj", "src/Domain/"]

# Восстанавливаем зависимости
RUN dotnet restore "src/NotificationWorker/NotificationWorker.csproj"

# Копируем весь исходный код и собираем воркер
COPY . .
WORKDIR "/src/src/NotificationWorker"
RUN dotnet build "NotificationWorker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NotificationWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NotificationWorker.dll"]
