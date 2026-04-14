# Verification Round 1

Issue → Location → Fix
Invalid .NET Target Framework Moniker (TFM) is used. `net10.0` is not a valid TFM. The current Long-Term Support (LTS) version is .NET 8, which uses `net8.0`. All package versions and SDK images should align with a valid TFM.
*   `Directory.Build.props`
*   `docker/Dockerfile.Api`
*   `docker/Dockerfile.Web`
*   `docker/Dockerfile.Playwright`

```diff
--- a/Directory.Build.props
+++ b/Directory.Build.props
@@ -1,5 +1,5 @@
 <Project>
   <PropertyGroup>
-    <TargetFramework>net10.0</TargetFramework>
+    <TargetFramework>net8.0</TargetFramework>
     <Nullable>enable</Nullable>
     <ImplicitUsings>enable</ImplicitUsings>
     <Deterministic>true</Deterministic>

--- a/docker/Dockerfile.Api
+++ b/docker/Dockerfile.Api
@@ -1,4 +1,4 @@
-FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
+FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
 WORKDIR /src
 
 COPY Directory.Build.props Directory.Packages.props Dabbler.Platform.sln ./
@@ -9,7 +9,7 @@
 COPY . .
 RUN dotnet publish src/Apps/AcmeCoffeeOnline.Api/AcmeCoffeeOnline.Api.csproj -c Release -o /app/publish --no-restore
 
-FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
+FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
 WORKDIR /app
 RUN apt-get update \
     && apt-get install -y --no-install-recommends curl \

--- a/docker/Dockerfile.Web
+++ b/docker/Dockerfile.Web
@@ -1,4 +1,4 @@
-FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
+FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
 WORKDIR /src
 
 COPY Directory.Build.props Directory.Packages.props Dabbler.Platform.sln ./
@@ -9,7 +9,7 @@
 COPY . .
 RUN dotnet publish src/Apps/AcmeCoffeeOnline.Web/AcmeCoffeeOnline.Web.csproj -c Release -o /app/publish --no-restore
 
-FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
+FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
 WORKDIR /app
 RUN apt-get update \
     && apt-get install -y --no-install-recommends curl \

--- a/docker/Dockerfile.Playwright
+++ b/docker/Dockerfile.Playwright
@@ -1,4 +1,4 @@
-FROM mcr.microsoft.com/dotnet/sdk:10.0 AS runner
+FROM mcr.microsoft.com/dotnet/sdk:8.0 AS runner
 WORKDIR /src
 
 COPY Directory.Build.props Directory.Packages.props Dabbler.Platform.sln ./
@@ -10,10 +10,7 @@
 
 COPY . .
 
-RUN dotnet build tests/AcmeCoffeeOnline.Playwright/AcmeCoffeeOnline.Playwright.csproj -c Release --no-restore
-RUN tests/AcmeCoffeeOnline.Playwright/bin/Release/net10.0/.playwright/node/linux-x64/node \
-    tests/AcmeCoffeeOnline.Playwright/bin/Release/net10.0/.playwright/package/cli.js \
-    install --with-deps chromium
+RUN dotnet build tests/AcmeCoffeeOnline.Playwright/AcmeCoffeeOnline.Playwright.csproj -c Release --no-restore -t:PlaywrightInstall --getProperty:PlaywrightInstallMessage
 
 ENV PLAYWRIGHT_BASE_URL=http://web:8080
 

```
***
Issue → Location → Fix
Package versions are invalid or reference a future, non-existent version of .NET. They should be updated to stable versions compatible with .NET 8.
*   `Directory.Packages.props`

```diff
--- a/Directory.Packages.props
+++ b/Directory.Packages.props
@@ -5,12 +5,12 @@
   </PropertyGroup>
 
   <ItemGroup>
-    <PackageVersion Include="coverlet.collector" Version="6.0.4" />
-    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.5" />
-    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="10.0.5" />
-    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
-    <PackageVersion Include="Microsoft.Playwright" Version="1.53.0" />
-    <PackageVersion Include="xunit" Version="2.9.3" />
-    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
+    <PackageVersion Include="coverlet.collector" Version="6.0.2" />
+    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.6" />
+    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="8.0.6" />
+    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.10.0" />
+    <PackageVersion Include="Microsoft.Playwright" Version="1.44.0" />
+    <PackageVersion Include="xunit" Version="2.8.1" />
+    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.1" />
   </ItemGroup>
 </Project>

```
***
Issue → Location → Fix
The Playwright browser installation command is fragile because it relies on a hardcoded path into the build output directory. A more robust method is to use the `PlaywrightInstall` MSBuild target provided by the `Microsoft.Playwright` package.
*   `docker/Dockerfile.Playwright`

```diff
--- a/docker/Dockerfile.Playwright
+++ b/docker/Dockerfile.Playwright
@@ -10,10 +10,7 @@
 
 COPY . .
 
-RUN dotnet build tests/AcmeCoffeeOnline.Playwright/AcmeCoffeeOnline.Playwright.csproj -c Release --no-restore
-RUN tests/AcmeCoffeeOnline.Playwright/bin/Release/net10.0/.playwright/node/linux-x64/node \
-    tests/AcmeCoffeeOnline.Playwright/bin/Release/net10.0/.playwright/package/cli.js \
-    install --with-deps chromium
+RUN dotnet build tests/AcmeCoffeeOnline.Playwright/AcmeCoffeeOnline.Playwright.csproj -c Release --no-restore -t:PlaywrightInstall --getProperty:PlaywrightInstallMessage
 
 ENV PLAYWRIGHT_BASE_URL=http://web:8080
 

```
***
Issue → Location → Fix
The Dockerfiles for the API and Web projects are nearly identical, leading to code duplication. They can be consolidated into a single Dockerfile that uses a build argument to specify the project to build, reducing maintenance overhead.
*   `docker/Dockerfile.Api`
*   `docker/Dockerfile.Web`
*   `docker/docker-compose.yml`

1.  Delete `docker/Dockerfile.Web` and `docker/Dockerfile.Api`.
2.  Create a new file `docker/Dockerfile.dotnet` with the following content:

```dockerfile
# docker/Dockerfile.dotnet
ARG DOTNET_VERSION=8.0
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build

ARG PROJECT_PATH
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props Dabbler.Platform.sln ./
COPY src/ /src/src/
COPY tests/ /src/tests/

RUN dotnet restore ${PROJECT_PATH}

COPY . .
RUN dotnet publish ${PROJECT_PATH} -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AcmeCoffeeOnline.Api.dll"]
```
*Note: The `ENTRYPOINT` is specific. The following `docker-compose.yml` change overrides it for the web project.*

3.  Update `docker/docker-compose.yml` to use the new Dockerfile and provide the correct build arguments and entrypoint overrides.

```diff
--- a/docker/docker-compose.yml
+++ b/docker/docker-compose.yml
@@ -28,8 +28,11 @@
   api:
     build:
       context: ..
-      dockerfile: docker/Dockerfile.Api
+      dockerfile: docker/Dockerfile.dotnet
+      args:
+        PROJECT_PATH: src/Apps/AcmeCoffeeOnline.Api/AcmeCoffeeOnline.Api.csproj
     environment:
+      # This is required because the generic entrypoint uses the Api.dll
       ASPNETCORE_ENVIRONMENT: Development
       ASPNETCORE_URLS: http://+:8080
     ports:
@@ -47,14 +50,17 @@
   web:
     build:
       context: ..
-      dockerfile: docker/Dockerfile.Web
+      dockerfile: docker/Dockerfile.dotnet
+      args:
+        PROJECT_PATH: src/Apps/AcmeCoffeeOnline.Web/AcmeCoffeeOnline.Web.csproj
     environment:
       ASPNETCORE_ENVIRONMENT: Development
       ASPNETCORE_URLS: http://+:8080
       Services__ApiBaseUrl: http://api:8080
     ports:
       - "5010:8080"
+    # Override the default entrypoint from the Dockerfile
+    entrypoint: ["dotnet", "AcmeCoffeeOnline.Web.dll"]
     depends_on:
       api:
         condition: service_healthy

```


---

# Verification Round 2

**VERIFIED**

## Rationale

All Session 2 spec requirements are satisfied, as confirmed by the provided environment-specific evidence:

| Requirement | Status | Evidence |
|---|---|---|
| Solution targets `net10.0` | ✅ | `dotnet build Dabbler.Platform.sln` — 0 warnings, 0 errors with SDK `11.0.100-preview.2.26159.112` |
| API project builds and tests pass | ✅ | `dotnet test` — API xUnit passing |
| Web project builds and tests pass | ✅ | `dotnet test` — Web xUnit passing |
| Playwright smoke tests pass (local) | ✅ | `dotnet test` — Playwright smoke passing |
| Docker Compose builds successfully | ✅ | `docker compose up --build` succeeded with `mcr.microsoft.com/dotnet/sdk:10.0` and `aspnet:10.0` images |
| Playwright smoke passes in Docker runner | ✅ | `--abort-on-container-exit playwright` exited successfully |

## Round 1 Claims — Disposition

| Round 1 Claim | Disposition |
|---|---|
| `net10.0` is invalid | **Rejected.** Build succeeds; TFM is valid in the repo's SDK (11.0 preview). Aligns with current Dabbler repos targeting .NET 10. |
| `10.0` Docker images are invalid | **Rejected.** `docker compose up --build` pulled and used them successfully. |
| Downgrade to .NET 8 | **Rejected.** Contradicted by all build/test/container evidence. |
| Older package versions for .NET 8 compat | **Rejected.** Moot — current versions build and run correctly against `net10.0`. |
| Replace explicit Playwright install path | **Rejected.** Playwright smoke passes both locally and in Docker with the current path. |
| Consolidate API/Web Dockerfiles | **Not a blocking issue.** Optional cleanup; does not affect spec compliance. |

No actual blocking or non-compliant problems remain.

