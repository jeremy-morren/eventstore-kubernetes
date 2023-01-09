FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
EXPOSE 80/tcp
EXPOSE 443/tcp

#Ensure temporary directory is set
ENV CONFIG_BACKUP__TEMPDIRECTORY="/tmp"

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /Build
COPY "EventStoreBackup.csproj" "."
RUN dotnet restore "EventStoreBackup.csproj"
COPY . .
RUN dotnet build "EventStoreBackup.csproj" -c Release
FROM build AS publish
RUN dotnet publish "EventStoreBackup.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EventStoreBackup.dll"]
