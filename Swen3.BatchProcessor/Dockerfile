FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Swen3.BatchProcessor/Swen3.BatchProcessor.csproj", "Swen3.BatchProcessor/"]
COPY ["Swen3.API/Swen3.API.csproj", "Swen3.API/"]
COPY ["Swen3.Shared/Swen3.Shared.csproj", "Swen3.Shared/"]
COPY ["Swen3.Storage/Swen3.Storage.csproj", "Swen3.Storage/"]
RUN dotnet restore "Swen3.BatchProcessor/Swen3.BatchProcessor.csproj"
COPY . .
WORKDIR "/src/Swen3.BatchProcessor"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/data/input /app/data/archive
ENTRYPOINT ["dotnet", "Swen3.BatchProcessor.dll"]
