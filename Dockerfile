FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props ./
COPY src/RetroScan.Api/RetroScan.Api.csproj src/RetroScan.Api/
RUN dotnet restore src/RetroScan.Api/RetroScan.Api.csproj
COPY src/ src/
RUN dotnet publish src/RetroScan.Api/RetroScan.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "RetroScan.Api.dll"]
