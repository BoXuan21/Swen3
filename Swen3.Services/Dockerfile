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

<<<<<<< Updated upstream
# Multi-arch support: detect architecture and create appropriate symlinks
=======
>>>>>>> Stashed changes
RUN ARCH=$(dpkg --print-architecture) && \
    if [ "$ARCH" = "amd64" ]; then \
        LIB_PATH="/usr/lib/x86_64-linux-gnu"; \
        ln -sf $LIB_PATH/libdl.so.2 $LIB_PATH/libdl.so 2>/dev/null || true; \
    elif [ "$ARCH" = "arm64" ]; then \
        LIB_PATH="/usr/lib/aarch64-linux-gnu"; \
<<<<<<< Updated upstream
        # On ARM64 Bookworm, libdl is part of libc - create symlink for Tesseract.NET
=======
>>>>>>> Stashed changes
        ln -sf $LIB_PATH/libc.so.6 $LIB_PATH/libdl.so.2 2>/dev/null || true; \
        ln -sf $LIB_PATH/libc.so.6 $LIB_PATH/libdl.so 2>/dev/null || true; \
        ln -sf $LIB_PATH/libc.so.6 /app/libdl.so 2>/dev/null || true; \
    fi && \
    mkdir -p /app/x64 && \
    ln -sf $LIB_PATH/liblept.so.5 /app/x64/libleptonica-1.82.0.so && \
    ln -sf $LIB_PATH/libtesseract.so.5 /app/x64/libtesseract50.so

WORKDIR /app/x64

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
