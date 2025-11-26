# Use the official .NET 8.0 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY Swen3.API/Swen3.API.csproj Swen3.API/
COPY Swen3.Shared/Swen3.Shared.csproj Swen3.Shared/
COPY Swen3.Storage/Swen3.Storage.csproj Swen3.Storage/
RUN dotnet restore "Swen3.API/Swen3.API.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
RUN dotnet build "Swen3.API/Swen3.API.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Swen3.API/Swen3.API.csproj" -c Release -o /app/publish

# Use the official .NET 8.0 runtime image for running
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Expose the port that the API will run on
EXPOSE 8080
EXPOSE 8081

# Set the entry point
ENTRYPOINT ["dotnet", "Swen3.API.dll"]
