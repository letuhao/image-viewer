# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/ImageViewer.Api/ImageViewer.Api.csproj", "src/ImageViewer.Api/"]
COPY ["src/ImageViewer.Application/ImageViewer.Application.csproj", "src/ImageViewer.Application/"]
COPY ["src/ImageViewer.Domain/ImageViewer.Domain.csproj", "src/ImageViewer.Domain/"]
COPY ["src/ImageViewer.Infrastructure/ImageViewer.Infrastructure.csproj", "src/ImageViewer.Infrastructure/"]
COPY ["src/ImageViewer.Worker/ImageViewer.Worker.csproj", "src/ImageViewer.Worker/"]

# Restore dependencies
RUN dotnet restore "src/ImageViewer.Api/ImageViewer.Api.csproj"
RUN dotnet restore "src/ImageViewer.Worker/ImageViewer.Worker.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/src/ImageViewer.Api"
RUN dotnet build "ImageViewer.Api.csproj" -c Release -o /app/build

# Build the worker service
WORKDIR "/src/src/ImageViewer.Worker"
RUN dotnet build "ImageViewer.Worker.csproj" -c Release -o /app/worker/build

# Publish the application
WORKDIR "/src/src/ImageViewer.Api"
RUN dotnet publish "ImageViewer.Api.csproj" -c Release -o /app/publish

# Publish the worker service
WORKDIR "/src/src/ImageViewer.Worker"
RUN dotnet publish "ImageViewer.Worker.csproj" -c Release -o /app/worker/publish

# Use the official .NET 8 runtime image for running
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy the published application
COPY --from=build /app/publish .

# Create directories for logs and temp files
RUN mkdir -p /app/logs /app/temp

# Expose the port
EXPOSE 5000
EXPOSE 5001

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000;https://+:5001

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "ImageViewer.Api.dll"]
