FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
RUN mkdir -p /tls
EXPOSE 443
ENV ASPNETCORE_URLS=""

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY "EventStoreProxy.csproj" .
RUN dotnet restore "EventStoreProxy.csproj"
COPY . .
RUN dotnet publish "EventStoreProxy.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY run.sh .
ENTRYPOINT ["/bin/sh", "/app/run.sh"]
