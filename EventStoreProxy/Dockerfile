FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl ca-certificates \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 443
ENV ASPNETCORE_URLS=""
ENV PATH="/setup:/client-certs:${PATH}"

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS proxy
WORKDIR /src
COPY "EventStoreProxy/EventStoreProxy.csproj" ./
RUN dotnet restore "EventStoreProxy.csproj"
COPY EventStoreProxy/ ./
RUN dotnet publish "EventStoreProxy.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS clientcerts
WORKDIR /src
COPY "ClientCertificatesCli/ClientCertificatesCli.csproj" ./
RUN dotnet restore "ClientCertificatesCli.csproj"
COPY ClientCertificatesCli/ ./
RUN dotnet publish "ClientCertificatesCli.csproj" -c Release -o /app/publish


FROM mcr.microsoft.com/dotnet/sdk:6.0 AS setup
WORKDIR /src
COPY "SetupCli/SetupCli.csproj" ./
RUN dotnet restore "SetupCli.csproj"
COPY SetupCli/ ./
RUN dotnet publish "SetupCli.csproj" -c Release -o /app/publish
COPY SetupCli/run.sh /app/publish

FROM base AS final
WORKDIR /setup
COPY --from=setup /app/publish .
WORKDIR /client-certs
COPY --from=clientcerts /app/publish .
WORKDIR /app
COPY --from=proxy /app/publish .
COPY EventStoreProxy/run.sh .
ENTRYPOINT ["/bin/sh", "/app/run.sh"]
