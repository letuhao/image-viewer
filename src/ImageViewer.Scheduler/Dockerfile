# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["ImageViewer.Scheduler/ImageViewer.Scheduler.csproj", "ImageViewer.Scheduler/"]
COPY ["ImageViewer.Domain/ImageViewer.Domain.csproj", "ImageViewer.Domain/"]
COPY ["ImageViewer.Application/ImageViewer.Application.csproj", "ImageViewer.Application/"]
COPY ["ImageViewer.Infrastructure/ImageViewer.Infrastructure.csproj", "ImageViewer.Infrastructure/"]
RUN dotnet restore "ImageViewer.Scheduler/ImageViewer.Scheduler.csproj"

# Copy source code
COPY . .

# Build and publish
WORKDIR "/src/ImageViewer.Scheduler"
RUN dotnet build "ImageViewer.Scheduler.csproj" -c Release -o /app/build
RUN dotnet publish "ImageViewer.Scheduler.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install necessary dependencies for SkiaSharp
RUN apt-get update && apt-get install -y \
    libfontconfig1 \
    libfreetype6 \
    libx11-6 \
    libx11-xcb1 \
    libxcb1 \
    libxrender1 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Create logs directory
RUN mkdir -p /app/logs

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENTRYPOINT ["dotnet", "ImageViewer.Scheduler.dll"]

