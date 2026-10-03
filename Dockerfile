# syntax=docker/dockerfile:1

# builder: restores and publishes the container host only. Building the whole solution would pull
# in the Blazor demo and its wasm workload, which this SDK image does not have. The analyzers run
# here exactly as in `dotnet build`, which is why .editorconfig is part of the context.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /src
COPY global.json Directory.Build.props .editorconfig ./
COPY src/ src/
RUN dotnet restore src/OpenHoldemEngine.Server/OpenHoldemEngine.Server.csproj
RUN dotnet publish src/OpenHoldemEngine.Server/OpenHoldemEngine.Server.csproj -c Release --no-restore -o /app

# final: the provider-neutral runtime image every target below starts from. It sets no provider and
# no connection string, runs as the non-root user `app`, and migrates the database on start-up.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
LABEL org.opencontainers.image.source="https://github.com/miekhra/OpenHoldemEngine" \
      org.opencontainers.image.licenses="Apache-2.0" \
      org.opencontainers.image.title="OpenHoldemEngine"
WORKDIR /app
COPY --from=builder /app .
ENV ASPNETCORE_HTTP_PORTS=8080 \
    HoldemEngine__MigrateDatabaseOnStartup=true
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "OpenHoldemEngine.Server.dll"]

# postgres: PostgreSQL. No default connection string, so a missing one fails fast at start-up.
FROM final AS postgres
ENV HoldemServer__Provider=PostgreSql

# mssql: SQL Server. No default connection string, so a missing one fails fast at start-up.
FROM final AS mssql
ENV HoldemServer__Provider=SqlServer

# sqlite: a self-contained image with the database file in the /data volume, owned by `app`
# because /app is not writable by it. Being the last stage, it is what a plain `docker build` gives.
FROM final AS sqlite
USER root
RUN mkdir -p /data && chown $APP_UID:$APP_UID /data
USER $APP_UID
ENV HoldemServer__Provider=Sqlite \
    ConnectionStrings__Holdem="Data Source=/data/holdem.db"
VOLUME /data
