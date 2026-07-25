# Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY eCommerce.WebAPI/eCommerce.WebAPI.csproj eCommerce.WebAPI/
COPY eCommerce.Services/eCommerce.Services.csproj eCommerce.Services/
COPY eCommerce.Model/eCommerce.Model.csproj eCommerce.Model/
COPY eCommerce.Common.Services/eCommerce.Common.Services.csproj eCommerce.Common.Services/

RUN dotnet restore eCommerce.WebAPI/eCommerce.WebAPI.csproj

COPY . .
RUN dotnet publish eCommerce.WebAPI/eCommerce.WebAPI.csproj -c Release -o /app/publish --no-restore

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
ENTRYPOINT ["dotnet", "eCommerce.WebAPI.dll"]
