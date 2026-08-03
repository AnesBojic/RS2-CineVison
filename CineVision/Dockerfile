# Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY CineVision.WebAPI/CineVision.WebAPI.csproj CineVision.WebAPI/
COPY CineVision.Services/CineVision.Services.csproj CineVision.Services/
COPY CineVision.Model/CineVision.Model.csproj CineVision.Model/
COPY CineVision.Common.Services/CineVision.Common.Services.csproj CineVision.Common.Services/

RUN dotnet restore CineVision.WebAPI/CineVision.WebAPI.csproj

COPY . .
RUN dotnet publish CineVision.WebAPI/CineVision.WebAPI.csproj -c Release -o /app/publish --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Used by docker-compose healthcheck
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

EXPOSE 5121
ENV ASPNETCORE_URLS=http://+:5121
ENV ASPNETCORE_ENVIRONMENT=Development

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CineVision.WebAPI.dll"]
