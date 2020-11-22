#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["Stonks2/Stonks2.csproj", "Stonks2/"]
COPY ["Search.BingNewsSearch/Microsoft.Azure.CognitiveServices.Search.BingNewsSearch.csproj", "Search.BingNewsSearch/"]
RUN dotnet restore "Stonks2/Stonks2.csproj"
COPY . .
WORKDIR "/src/Stonks2"
RUN dotnet build "Stonks2.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Stonks2.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Stonks2.dll"]