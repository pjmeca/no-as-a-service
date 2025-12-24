# --- Stage 1: Build ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Install tools required for Native AOT compilation
RUN apt-get update && apt-get install -y --no-install-recommends \
    clang \
    libc6-dev \
    build-essential \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Copy only solution and project files to restore dependencies
COPY *.sln ./
COPY NoAsAService/*.csproj ./NoAsAService/
RUN dotnet restore

# Copy the full project including resources
COPY NoAsAService ./NoAsAService
COPY NoAsAService/reasons ./NoAsAService/reasons

# Publish in Release mode with Native AOT
RUN dotnet publish ./NoAsAService/NoAsAService.csproj \
    -c Release \
    -r linux-x64 \
    -p:PublishAot=true \
    -p:PublishSingleFile=true \
    -o /app/publish /p:UseAppHost=true

# --- Stage 2: Minimal Runtime ---
FROM debian:bookworm-slim AS runtime
WORKDIR /app

# Install minimal libraries required to run the native binary
RUN apt-get update && apt-get install -y --no-install-recommends \
    libicu-dev \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Copy the native binary and resources from the build stage
COPY --from=build /app/publish/* .
COPY NoAsAService/reasons ./reasons

# Expose port and allow external connections
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

# Set the native binary as the entry point
ENTRYPOINT ["./NoAsAService"]
