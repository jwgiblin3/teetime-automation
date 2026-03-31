FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY *.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o out

# Install Playwright browsers in build stage (needs SDK).
# Use explicit path to avoid PATH ordering issues across RUN layers.
RUN dotnet tool install --global Microsoft.Playwright.CLI && \
    /root/.dotnet/tools/playwright install chromium

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Install Chromium runtime dependencies.
# NOTE: libasound2 was renamed to libasound2t64 in Debian Bookworm (used by .NET 8 images).
RUN apt-get update && apt-get install -y \
    libnss3 libatk1.0-0 libatk-bridge2.0-0 libcups2 libdrm2 \
    libxkbcommon0 libxcomposite1 libxdamage1 libxfixes3 libxrandr2 \
    libgbm1 libasound2 libpango-1.0-0 libcairo2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/out .

# Copy Playwright browsers from build stage
COPY --from=build /root/.cache/ms-playwright /root/.cache/ms-playwright

EXPOSE 8080

# Use shell form so ${PORT:-8080} is evaluated at container startup, not at build time.
# Railway injects PORT as a runtime env var; this ensures the app binds to it.
ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet TeeTimeAutomator.API.dll"]
