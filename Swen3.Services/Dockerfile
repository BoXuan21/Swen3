FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Swen3.Shared/Swen3.Shared.csproj Swen3.Shared/
COPY Swen3.Services/Swen3.Services.csproj Swen3.Services/
RUN dotnet restore "Swen3.Services/Swen3.Services.csproj"

COPY . .
WORKDIR "/src/Swen3.Services"
RUN dotnet publish "Swen3.Services.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0-bookworm-slim AS final
WORKDIR /app

RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    imagemagick \
    tesseract-ocr \
    ghostscript \
    libleptonica-dev \
    tesseract-ocr-eng \
    libgdiplus && \
    rm -rf /var/lib/apt/lists/*

RUN ln -s /usr/lib/x86_64-linux-gnu/libdl.so.2 /usr/lib/x86_64-linux-gnu/libdl.so

WORKDIR /app/x64

RUN ln -s /usr/lib/x86_64-linux-gnu/liblept.so.5 /app/x64/libleptonica-1.82.0.so
RUN ln -s /usr/lib/x86_64-linux-gnu/libtesseract.so.5 /app/x64/libtesseract50.so

RUN apt-get clean && rm -rf /var/lib/apt/lists/*

WORKDIR /app

RUN POLICY_FILE="/etc/ImageMagick-6/policy.xml" && \
    if [ -f "$POLICY_FILE" ]; then \
    sed -i 's/<policy domain="coder" rights="none" pattern="PDF" \/>/<policy domain="coder" rights="read|write" pattern="PDF" \/>/g' $POLICY_FILE; \
    else \
    sed -i 's/rights="none" pattern="PDF"/rights="read|write" pattern="PDF"/g' /etc/ImageMagick-6/policy.xml; \
    fi

ENV TESSDATA_PREFIX=/usr/share/tesseract-ocr/5/tessdata
ENV DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Swen3.Services.dll"]
