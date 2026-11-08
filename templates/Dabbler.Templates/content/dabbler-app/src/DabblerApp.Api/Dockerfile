FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files first for layer-cache-friendly restore.
COPY src/DabblerApp.Shared/DabblerApp.Shared.csproj src/DabblerApp.Shared/
COPY src/DabblerApp.Api/DabblerApp.Api.csproj       src/DabblerApp.Api/
RUN dotnet restore src/DabblerApp.Api/DabblerApp.Api.csproj

# Copy source and publish.
COPY src/DabblerApp.Shared/ src/DabblerApp.Shared/
COPY src/DabblerApp.Api/    src/DabblerApp.Api/
RUN dotnet publish src/DabblerApp.Api/DabblerApp.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "DabblerApp.Api.dll"]
