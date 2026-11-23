FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/November.Bff.Api/November.Bff.Api.csproj src/November.Bff.Api/
RUN dotnet restore src/November.Bff.Api/November.Bff.Api.csproj

COPY src/November.Bff.Api/ src/November.Bff.Api/
RUN dotnet publish src/November.Bff.Api/November.Bff.Api.csproj \
    --configuration Release --no-restore --output /app/publish -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "November.Bff.Api.dll"]
