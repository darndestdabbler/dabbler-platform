FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the feed-only nuget.config before restore so the restore layer resolves
# Dabbler.* packages from nuget.org deterministically (<clear/> drops ambient
# sources). No local packages/ feed is copied — the packages restore from the feed.
COPY nuget.config .

# Copy csproj first for layer-cache-friendly restore.
COPY src/DabblerApp.Web/DabblerApp.Web.csproj src/DabblerApp.Web/
RUN dotnet restore src/DabblerApp.Web/DabblerApp.Web.csproj

# Copy source and publish.
# NOTE: do NOT add --no-restore. Under .NET 10 SDK, publishing with
# --no-restore after a csproj-only restore skips the Blazor static-asset
# discovery pass that registers /_framework/blazor.web.js in the
# endpoints manifest. The redundant restore is a no-op for package
# resolution but is required to trigger source-aware static-asset targets.
COPY src/DabblerApp.Web/ src/DabblerApp.Web/
RUN dotnet publish src/DabblerApp.Web/DabblerApp.Web.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "DabblerApp.Web.dll"]
