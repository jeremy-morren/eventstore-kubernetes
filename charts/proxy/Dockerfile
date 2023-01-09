FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS base
WORKDIR /app
EXPOSE 80
ENV ASPNETCORE_URLS=""

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build
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
