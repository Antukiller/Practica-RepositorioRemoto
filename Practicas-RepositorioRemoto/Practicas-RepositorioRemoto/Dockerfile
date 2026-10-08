FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Practicas-RepositorioRemoto.csproj", "./"]

RUN dotnet restore "Practicas-RepositorioRemoto.csproj"

COPY . .

RUN dotnet build "Practicas-RepositorioRemoto.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release

RUN dotnet publish "Practicas-RepositorioRemoto.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

RUN mkdir -p /app/data && chown -R app:app /app && chmod -R 777 /app/data

USER app

ENTRYPOINT ["dotnet", "Practicas-RepositorioRemoto.dll"]