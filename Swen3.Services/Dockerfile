FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Swen3.Shared/Swen3.Shared.csproj Swen3.Shared/
COPY Swen3.Services/Swen3.Services.csproj Swen3.Services/
RUN dotnet restore "Swen3.Services/Swen3.Services.csproj"
COPY . .
WORKDIR "/src/Swen3.Services"
RUN dotnet build "Swen3.Services.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0-bookworm-slim AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    tesseract-ocr \
    tesseract-ocr-eng \
    && rm -rf /var/lib/apt/lists/*

ENV TESSDATA_PREFIX=/usr/share/tesseract-ocr/4.00/tessdata

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Swen3.Services.dll"]
