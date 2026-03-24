FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY *.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o out

# Install Playwright browsers
RUN dotnet tool install --global Microsoft.Playwright.CLI
RUN playwright install chromium

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
RUN apt-get update && apt-get install -y \
    libplaywrightnet1 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/out .

# Copy Playwright browsers from build stage
COPY --from=build /root/.cache/ms-playwright /root/.cache/ms-playwright

ENTRYPOINT ["dotnet", "TeeTimeAutomator.API.dll"]
