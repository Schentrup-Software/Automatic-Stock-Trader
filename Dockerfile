#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["AutomaticStockTrader/AutomaticStockTrader.csproj", "AutomaticStockTrader/"]
COPY ["AutomaticStockTrader.Repository/AutomaticStockTrader.Repository.csproj", "AutomaticStockTrader.Repository/"]
COPY ["AutomaticStockTrader.Domain/AutomaticStockTrader.Domain.csproj", "AutomaticStockTrader.Domain/"]
COPY ["AutomaticStockTrader.Core/AutomaticStockTrader.Core.csproj", "AutomaticStockTrader.Core/"]
RUN dotnet restore "AutomaticStockTrader/AutomaticStockTrader.csproj"
COPY . .
WORKDIR "/src/AutomaticStockTrader"
RUN dotnet build "AutomaticStockTrader.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AutomaticStockTrader.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN chown -R www-data /app
USER www-data

ENTRYPOINT ["dotnet", "AutomaticStockTrader.dll"]
