FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Analytics/AnalyticsService.csproj .
RUN dotnet restore

COPY src/Analytics/. .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN groupadd -r appgroup && \
    useradd -r -g appgroup -d /app -s /bin/false appuser

COPY --from=build /app/publish .

RUN chown -R appuser:appgroup /app

USER appuser

EXPOSE 5004

# Healthcheck 
HEALTHCHECK --interval=10s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5004/analytics/health || exit 1

ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_HTTP_PORTS=5004

ENTRYPOINT ["dotnet", "AnalyticsService.dll"]
