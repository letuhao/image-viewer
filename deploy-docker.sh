#!/bin/bash

# Bash script for Docker deployment
set -e

# Default values
ENVIRONMENT="development"
BUILD=false
PULL=false
FORCE=false
HELP=false

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -e|--environment)
            ENVIRONMENT="$2"
            shift 2
            ;;
        -b|--build)
            BUILD=true
            shift
            ;;
        -p|--pull)
            PULL=true
            shift
            ;;
        -f|--force)
            FORCE=true
            shift
            ;;
        -h|--help)
            HELP=true
            shift
            ;;
        *)
            echo "Unknown option $1"
            exit 1
            ;;
    esac
done

# Show help
if [ "$HELP" = true ]; then
    echo "ImageViewer Docker Deployment Script"
    echo ""
    echo "Usage: ./deploy-docker.sh [options]"
    echo ""
    echo "Options:"
    echo "  -e, --environment <env>  Deployment environment (development|production) [default: development]"
    echo "  -b, --build              Build images before deployment"
    echo "  -p, --pull               Pull latest images before deployment"
    echo "  -f, --force              Force recreate containers"
    echo "  -h, --help               Show this help message"
    echo ""
    echo "Examples:"
    echo "  ./deploy-docker.sh                                    # Deploy development environment"
    echo "  ./deploy-docker.sh -e production -b                  # Build and deploy production"
    echo "  ./deploy-docker.sh -f                                # Force recreate all containers"
    exit 0
fi

echo "🚀 ImageViewer Docker Deployment"
echo "Environment: $ENVIRONMENT"

# Check if Docker is running
if ! docker version > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker."
    exit 1
fi
echo "✅ Docker is running"

# Check if Docker Compose is available
if ! docker-compose version > /dev/null 2>&1; then
    echo "❌ Docker Compose is not available. Please install Docker Compose."
    exit 1
fi
echo "✅ Docker Compose is available"

# Create necessary directories
echo "📁 Creating necessary directories..."
directories=("logs" "temp" "data" "nginx/ssl")
for dir in "${directories[@]}"; do
    if [ ! -d "$dir" ]; then
        mkdir -p "$dir"
        echo "  Created: $dir"
    fi
done

# Set environment variables
export COMPOSE_PROJECT_NAME="imageviewer"
export COMPOSE_FILE="docker-compose.yml"

if [ "$ENVIRONMENT" = "development" ]; then
    export COMPOSE_FILE="$COMPOSE_FILE:docker-compose.override.yml"
    echo "🔧 Using development configuration"
else
    echo "🔧 Using production configuration"
fi

# Pull latest images if requested
if [ "$PULL" = true ]; then
    echo "📥 Pulling latest images..."
    docker-compose pull
fi

# Build images if requested
if [ "$BUILD" = true ]; then
    echo "🔨 Building images..."
    docker-compose build --no-cache
fi

# Stop existing containers
echo "🛑 Stopping existing containers..."
docker-compose down

# Start services
echo "🚀 Starting services..."
if [ "$FORCE" = true ]; then
    docker-compose up -d --force-recreate
else
    docker-compose up -d
fi

# Wait for services to be healthy
echo "⏳ Waiting for services to be healthy..."
max_wait=300 # 5 minutes
wait_time=0
interval=10

while [ $wait_time -lt $max_wait ]; do
    sleep $interval
    wait_time=$((wait_time + interval))
    
    services=$(docker-compose ps --services --filter "status=running")
    healthy_services=0
    total_services=$(echo "$services" | wc -l)
    
    for service in $services; do
        health=$(docker-compose ps --format "table {{.Service}}\t{{.State}}" | grep "$service" || true)
        if echo "$health" | grep -q "healthy\|Up"; then
            healthy_services=$((healthy_services + 1))
        fi
    done
    
    echo "  Health check: $healthy_services/$total_services services healthy"
    
    if [ $healthy_services -eq $total_services ]; then
        break
    fi
    
    if [ $wait_time -ge $max_wait ]; then
        echo "❌ Services did not become healthy within $max_wait seconds"
        echo "📋 Service status:"
        docker-compose ps
        exit 1
    fi
done

echo "✅ All services are healthy!"

# Display service information
echo ""
echo "📋 Service Information:"
echo "  API: http://localhost:5000"
echo "  API (HTTPS): https://localhost:5001"
echo "  MongoDB: mongodb://localhost:27017"
echo "  RabbitMQ Management: http://localhost:15672"
echo "  Redis: redis://localhost:6379"

if [ "$ENVIRONMENT" = "production" ]; then
    echo "  Nginx: http://localhost:80"
    echo "  Nginx (HTTPS): https://localhost:443"
fi

echo ""
echo "🔍 Checking service health..."

# Check API health
if curl -f -s http://localhost:5000/health > /dev/null; then
    echo "  ✅ API is healthy"
else
    echo "  ❌ API health check failed"
fi

# Check MongoDB
if docker exec imageviewer-mongodb mongosh --eval "db.adminCommand('ping')" --quiet | grep -q "ok"; then
    echo "  ✅ MongoDB is healthy"
else
    echo "  ❌ MongoDB health check failed"
fi

# Check RabbitMQ
if docker exec imageviewer-rabbitmq rabbitmq-diagnostics ping | grep -q "pong"; then
    echo "  ✅ RabbitMQ is healthy"
else
    echo "  ❌ RabbitMQ health check failed"
fi

echo ""
echo "🎉 Deployment completed successfully!"
echo ""
echo "📝 Useful commands:"
echo "  View logs: docker-compose logs -f"
echo "  Stop services: docker-compose down"
echo "  Restart services: docker-compose restart"
echo "  View status: docker-compose ps"
