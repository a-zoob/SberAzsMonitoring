FROM mcr.microsoft.com/dotnet/sdk:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем файлы проектов для восстановления зависимостей
COPY ["src/ClickHouseWorker/ClickHouseWorker.csproj", "src/ClickHouseWorker/"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Domain/Domain.csproj", "src/Domain/"]

RUN dotnet restore "src/ClickHouseWorker/ClickHouseWorker.csproj"
COPY . .
WORKDIR "/src/src/ClickHouseWorker"
RUN dotnet build "ClickHouseWorker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ClickHouseWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ClickHouseWorker.dll"]
