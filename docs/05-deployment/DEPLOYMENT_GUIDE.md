# Deployment Guide - ImageViewer Platform

## 📋 Tổng Quan

Document này mô tả comprehensive deployment guide cho ImageViewer Platform với 57 database collections và 56 feature categories, bao gồm cả các tính năng enterprise mới.

## 🎯 Deployment Objectives

### **Primary Objectives**
1. **Reliable Deployment**: Đảm bảo deployment process ổn định và repeatable
2. **Zero Downtime**: Minimize downtime during deployments
3. **Scalability**: Support horizontal scaling và load balancing
4. **Security**: Secure deployment với proper access controls
5. **Monitoring**: Comprehensive monitoring và alerting

### **Secondary Objectives**
1. **Automation**: Automated deployment processes
2. **Environment Management**: Multiple environment support
3. **Rollback Capability**: Quick rollback khi có issues
4. **Documentation**: Clear deployment procedures
5. **Compliance**: Meet security và compliance requirements

## 🏗️ Infrastructure Architecture

### **Production Architecture**
```
┌─────────────────────────────────────────────────────────────┐
│                    Load Balancer                            │
│                  (NGINX/HAProxy)                           │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    API Gateway                              │
│                  (Kong/Ambassador)                         │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                        │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │
│  │   API Pod   │  │   API Pod   │  │   API Pod   │        │
│  │   (3x)      │  │   (3x)      │  │   (3x)      │        │
│  └─────────────┘  └─────────────┘  └─────────────┘        │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │
│  │ Worker Pod  │  │ Worker Pod  │  │ Worker Pod  │        │
│  │   (2x)      │  │   (2x)      │  │   (2x)      │        │
│  └─────────────┘  └─────────────┘  └─────────────┘        │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Data Layer                               │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │
│  │   MongoDB   │  │   Redis     │  │   RabbitMQ  │        │
│  │  Cluster    │  │  Cluster    │  │  Cluster    │        │
│  │   (3x)      │  │   (3x)      │  │   (3x)      │        │
│  └─────────────┘  └─────────────┘  └─────────────┘        │
└─────────────────────────────────────────────────────────────┘
```

### **Environment Structure**
```
┌─────────────────────────────────────────────────────────────┐
│                    Development                               │
│  - Single instance deployment                               │
│  - Local MongoDB, Redis, RabbitMQ                          │
│  - Hot reload enabled                                       │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Staging                                  │
│  - Production-like environment                              │
│  - Clustered services                                       │
│  - Full monitoring                                          │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Production                               │
│  - High availability cluster                                │
│  - Load balancing                                           │
│  - Full monitoring & alerting                              │
└─────────────────────────────────────────────────────────────┘
```

## 🐳 Containerization Strategy

### **Docker Configuration**

#### **API Service Dockerfile**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/ImageViewer.Api/ImageViewer.Api.csproj", "src/ImageViewer.Api/"]
COPY ["src/ImageViewer.Application/ImageViewer.Application.csproj", "src/ImageViewer.Application/"]
COPY ["src/ImageViewer.Domain/ImageViewer.Domain.csproj", "src/ImageViewer.Domain/"]
COPY ["src/ImageViewer.Infrastructure/ImageViewer.Infrastructure.csproj", "src/ImageViewer.Infrastructure/"]
RUN dotnet restore "src/ImageViewer.Api/ImageViewer.Api.csproj"
COPY . .
WORKDIR "/src/src/ImageViewer.Api"
RUN dotnet build "ImageViewer.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ImageViewer.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ImageViewer.Api.dll"]
```

#### **Worker Service Dockerfile**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/ImageViewer.Worker/ImageViewer.Worker.csproj", "src/ImageViewer.Worker/"]
COPY ["src/ImageViewer.Application/ImageViewer.Application.csproj", "src/ImageViewer.Application/"]
COPY ["src/ImageViewer.Domain/ImageViewer.Domain.csproj", "src/ImageViewer.Domain/"]
COPY ["src/ImageViewer.Infrastructure/ImageViewer.Infrastructure.csproj", "src/ImageViewer.Infrastructure/"]
RUN dotnet restore "src/ImageViewer.Worker/ImageViewer.Worker.csproj"
COPY . .
WORKDIR "/src/src/ImageViewer.Worker"
RUN dotnet build "ImageViewer.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ImageViewer.Worker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ImageViewer.Worker.dll"]
```

### **Docker Compose Configuration**

#### **Development Environment**
```yaml
version: '3.8'

services:
  mongodb:
    image: mongo:7.0
    container_name: imageviewer-mongodb
    ports:
      - "27017:27017"
    environment:
      MONGO_INITDB_ROOT_USERNAME: admin
      MONGO_INITDB_ROOT_PASSWORD: password
    volumes:
      - mongodb_data:/data/db
      - ./scripts/mongo-init.js:/docker-entrypoint-initdb.d/mongo-init.js:ro

  redis:
    image: redis:7.2-alpine
    container_name: imageviewer-redis
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data

  rabbitmq:
    image: rabbitmq:3.12-management
    container_name: imageviewer-rabbitmq
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: admin
      RABBITMQ_DEFAULT_PASS: password
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq

  api:
    build:
      context: .
      dockerfile: src/ImageViewer.Api/Dockerfile
    container_name: imageviewer-api
    ports:
      - "7001:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__MongoDB=mongodb://admin:password@mongodb:27017
      - ConnectionStrings__Redis=redis:6379
      - RabbitMQ__HostName=rabbitmq
      - RabbitMQ__UserName=admin
      - RabbitMQ__Password=password
    depends_on:
      - mongodb
      - redis
      - rabbitmq

  worker:
    build:
      context: .
      dockerfile: src/ImageViewer.Worker/Dockerfile
    container_name: imageviewer-worker
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__MongoDB=mongodb://admin:password@mongodb:27017
      - ConnectionStrings__Redis=redis:6379
      - RabbitMQ__HostName=rabbitmq
      - RabbitMQ__UserName=admin
      - RabbitMQ__Password=password
    depends_on:
      - mongodb
      - redis
      - rabbitmq

volumes:
  mongodb_data:
  redis_data:
  rabbitmq_data:
```

## ☸️ Kubernetes Deployment

### **Namespace Configuration**
```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: imageviewer
  labels:
    name: imageviewer
```

### **ConfigMap Configuration**
```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: imageviewer-config
  namespace: imageviewer
data:
  appsettings.json: |
    {
      "ConnectionStrings": {
        "MongoDB": "mongodb://mongodb-service:27017",
        "Redis": "redis-service:6379"
      },
      "RabbitMQ": {
        "HostName": "rabbitmq-service",
        "UserName": "admin",
        "Password": "password"
      },
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft.AspNetCore": "Warning"
        }
      }
    }
```

### **Secret Configuration**
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: imageviewer-secrets
  namespace: imageviewer
type: Opaque
data:
  mongodb-password: cGFzc3dvcmQ=  # base64 encoded
  redis-password: cGFzc3dvcmQ=    # base64 encoded
  rabbitmq-password: cGFzc3dvcmQ= # base64 encoded
  jwt-secret: c3VwZXItc2VjcmV0LWtleQ== # base64 encoded
```

### **MongoDB Deployment**
```yaml
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: mongodb
  namespace: imageviewer
spec:
  serviceName: mongodb-service
  replicas: 3
  selector:
    matchLabels:
      app: mongodb
  template:
    metadata:
      labels:
        app: mongodb
    spec:
      containers:
      - name: mongodb
        image: mongo:7.0
        ports:
        - containerPort: 27017
        env:
        - name: MONGO_INITDB_ROOT_USERNAME
          value: "admin"
        - name: MONGO_INITDB_ROOT_PASSWORD
          valueFrom:
            secretKeyRef:
              name: imageviewer-secrets
              key: mongodb-password
        volumeMounts:
        - name: mongodb-storage
          mountPath: /data/db
        resources:
          requests:
            memory: "512Mi"
            cpu: "250m"
          limits:
            memory: "1Gi"
            cpu: "500m"
  volumeClaimTemplates:
  - metadata:
      name: mongodb-storage
    spec:
      accessModes: ["ReadWriteOnce"]
      resources:
        requests:
          storage: 10Gi
---
apiVersion: v1
kind: Service
metadata:
  name: mongodb-service
  namespace: imageviewer
spec:
  selector:
    app: mongodb
  ports:
  - port: 27017
    targetPort: 27017
  clusterIP: None
```

### **API Service Deployment**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: imageviewer-api
  namespace: imageviewer
spec:
  replicas: 3
  selector:
    matchLabels:
      app: imageviewer-api
  template:
    metadata:
      labels:
        app: imageviewer-api
    spec:
      containers:
      - name: api
        image: imageviewer/api:latest
        ports:
        - containerPort: 80
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__MongoDB
          value: "mongodb://mongodb-service:27017"
        - name: ConnectionStrings__Redis
          value: "redis-service:6379"
        - name: RabbitMQ__HostName
          value: "rabbitmq-service"
        - name: RabbitMQ__UserName
          value: "admin"
        - name: RabbitMQ__Password
          valueFrom:
            secretKeyRef:
              name: imageviewer-secrets
              key: rabbitmq-password
        resources:
          requests:
            memory: "256Mi"
            cpu: "100m"
          limits:
            memory: "512Mi"
            cpu: "250m"
        livenessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 80
          initialDelaySeconds: 5
          periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: imageviewer-api-service
  namespace: imageviewer
spec:
  selector:
    app: imageviewer-api
  ports:
  - port: 80
    targetPort: 80
  type: ClusterIP
```

### **Worker Service Deployment**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: imageviewer-worker
  namespace: imageviewer
spec:
  replicas: 2
  selector:
    matchLabels:
      app: imageviewer-worker
  template:
    metadata:
      labels:
        app: imageviewer-worker
    spec:
      containers:
      - name: worker
        image: imageviewer/worker:latest
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__MongoDB
          value: "mongodb://mongodb-service:27017"
        - name: ConnectionStrings__Redis
          value: "redis-service:6379"
        - name: RabbitMQ__HostName
          value: "rabbitmq-service"
        - name: RabbitMQ__UserName
          value: "admin"
        - name: RabbitMQ__Password
          valueFrom:
            secretKeyRef:
              name: imageviewer-secrets
              key: rabbitmq-password
        resources:
          requests:
            memory: "256Mi"
            cpu: "100m"
          limits:
            memory: "512Mi"
            cpu: "250m"
        livenessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 10
```

## 🔄 CI/CD Pipeline

### **GitHub Actions Workflow**
```yaml
name: Build and Deploy

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

env:
  REGISTRY: ghcr.io
  IMAGE_NAME: ${{ github.repository }}

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Test
      run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"
    
    - name: Upload coverage reports
      uses: codecov/codecov-action@v3
      with:
        file: ./coverage.xml

  build:
    needs: test
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Set up Docker Buildx
      uses: docker/setup-buildx-action@v3
    
    - name: Log in to Container Registry
      uses: docker/login-action@v3
      with:
        registry: ${{ env.REGISTRY }}
        username: ${{ github.actor }}
        password: ${{ secrets.GITHUB_TOKEN }}
    
    - name: Extract metadata
      id: meta
      uses: docker/metadata-action@v5
      with:
        images: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}
        tags: |
          type=ref,event=branch
          type=ref,event=pr
          type=sha,prefix={{branch}}-
          type=raw,value=latest,enable={{is_default_branch}}
    
    - name: Build and push API image
      uses: docker/build-push-action@v5
      with:
        context: .
        file: ./src/ImageViewer.Api/Dockerfile
        push: true
        tags: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}/api:${{ steps.meta.outputs.tags }}
        labels: ${{ steps.meta.outputs.labels }}
    
    - name: Build and push Worker image
      uses: docker/build-push-action@v5
      with:
        context: .
        file: ./src/ImageViewer.Worker/Dockerfile
        push: true
        tags: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}/worker:${{ steps.meta.outputs.tags }}
        labels: ${{ steps.meta.outputs.labels }}

  deploy:
    needs: build
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Configure kubectl
      uses: azure/k8s-set-context@v3
      with:
        method: kubeconfig
        kubeconfig: ${{ secrets.KUBE_CONFIG }}
    
    - name: Deploy to Kubernetes
      run: |
        kubectl set image deployment/imageviewer-api api=${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}/api:latest -n imageviewer
        kubectl set image deployment/imageviewer-worker worker=${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}/worker:latest -n imageviewer
        kubectl rollout status deployment/imageviewer-api -n imageviewer
        kubectl rollout status deployment/imageviewer-worker -n imageviewer
```

## 📊 Monitoring & Observability

### **Prometheus Configuration**
```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: prometheus-config
  namespace: imageviewer
data:
  prometheus.yml: |
    global:
      scrape_interval: 15s
      evaluation_interval: 15s
    
    rule_files:
      - "alert_rules.yml"
    
    scrape_configs:
      - job_name: 'imageviewer-api'
        static_configs:
          - targets: ['imageviewer-api-service:80']
        metrics_path: '/metrics'
        scrape_interval: 5s
      
      - job_name: 'imageviewer-worker'
        static_configs:
          - targets: ['imageviewer-worker-service:80']
        metrics_path: '/metrics'
        scrape_interval: 5s
      
      - job_name: 'mongodb'
        static_configs:
          - targets: ['mongodb-exporter:9216']
      
      - job_name: 'redis'
        static_configs:
          - targets: ['redis-exporter:9121']
      
      - job_name: 'rabbitmq'
        static_configs:
          - targets: ['rabbitmq-exporter:9419']
```

### **Grafana Dashboard**
```json
{
  "dashboard": {
    "title": "ImageViewer Platform Dashboard",
    "panels": [
      {
        "title": "API Response Time",
        "type": "graph",
        "targets": [
          {
            "expr": "histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))",
            "legendFormat": "95th percentile"
          }
        ]
      },
      {
        "title": "Request Rate",
        "type": "graph",
        "targets": [
          {
            "expr": "rate(http_requests_total[5m])",
            "legendFormat": "{{method}} {{endpoint}}"
          }
        ]
      },
      {
        "title": "Error Rate",
        "type": "graph",
        "targets": [
          {
            "expr": "rate(http_requests_total{status=~\"5..\"}[5m])",
            "legendFormat": "5xx errors"
          }
        ]
      },
      {
        "title": "MongoDB Connections",
        "type": "graph",
        "targets": [
          {
            "expr": "mongodb_connections_current",
            "legendFormat": "Current connections"
          }
        ]
      },
      {
        "title": "RabbitMQ Queue Length",
        "type": "graph",
        "targets": [
          {
            "expr": "rabbitmq_queue_messages",
            "legendFormat": "{{queue}}"
          }
        ]
      }
    ]
  }
}
```

### **Alert Rules**
```yaml
groups:
- name: imageviewer-alerts
  rules:
  - alert: HighErrorRate
    expr: rate(http_requests_total{status=~"5.."}[5m]) > 0.1
    for: 2m
    labels:
      severity: critical
    annotations:
      summary: "High error rate detected"
      description: "Error rate is {{ $value }} errors per second"
  
  - alert: HighResponseTime
    expr: histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m])) > 1
    for: 2m
    labels:
      severity: warning
    annotations:
      summary: "High response time detected"
      description: "95th percentile response time is {{ $value }} seconds"
  
  - alert: MongoDBDown
    expr: up{job="mongodb"} == 0
    for: 1m
    labels:
      severity: critical
    annotations:
      summary: "MongoDB is down"
      description: "MongoDB instance is not responding"
  
  - alert: RabbitMQQueueBacklog
    expr: rabbitmq_queue_messages > 1000
    for: 5m
    labels:
      severity: warning
    annotations:
      summary: "RabbitMQ queue backlog"
      description: "Queue {{ $labels.queue }} has {{ $value }} messages"
```

## 🔒 Security Configuration

### **Network Policies**
```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: imageviewer-network-policy
  namespace: imageviewer
spec:
  podSelector:
    matchLabels:
      app: imageviewer-api
  policyTypes:
  - Ingress
  - Egress
  ingress:
  - from:
    - namespaceSelector:
        matchLabels:
          name: ingress-nginx
    ports:
    - protocol: TCP
      port: 80
  egress:
  - to:
    - podSelector:
        matchLabels:
          app: mongodb
    ports:
    - protocol: TCP
      port: 27017
  - to:
    - podSelector:
        matchLabels:
          app: redis
    ports:
    - protocol: TCP
      port: 6379
  - to:
    - podSelector:
        matchLabels:
          app: rabbitmq
    ports:
    - protocol: TCP
      port: 5672
```

### **Pod Security Policy**
```yaml
apiVersion: policy/v1beta1
kind: PodSecurityPolicy
metadata:
  name: imageviewer-psp
spec:
  privileged: false
  allowPrivilegeEscalation: false
  requiredDropCapabilities:
    - ALL
  volumes:
    - 'configMap'
    - 'emptyDir'
    - 'projected'
    - 'secret'
    - 'downwardAPI'
    - 'persistentVolumeClaim'
  runAsUser:
    rule: 'MustRunAsNonRoot'
  seLinux:
    rule: 'RunAsAny'
  fsGroup:
    rule: 'RunAsAny'
```

## 🚀 Deployment Procedures

### **1. Pre-Deployment Checklist**
- [ ] All tests passing
- [ ] Security scan completed
- [ ] Performance tests passed
- [ ] Database migrations ready
- [ ] Configuration updated
- [ ] Monitoring configured
- [ ] Rollback plan ready

### **2. Deployment Steps**

#### **Step 1: Deploy Infrastructure**
```bash
# Deploy MongoDB cluster
kubectl apply -f k8s/mongodb/

# Deploy Redis cluster
kubectl apply -f k8s/redis/

# Deploy RabbitMQ cluster
kubectl apply -f k8s/rabbitmq/

# Wait for services to be ready
kubectl wait --for=condition=ready pod -l app=mongodb -n imageviewer --timeout=300s
kubectl wait --for=condition=ready pod -l app=redis -n imageviewer --timeout=300s
kubectl wait --for=condition=ready pod -l app=rabbitmq -n imageviewer --timeout=300s
```

#### **Step 2: Deploy Application**
```bash
# Deploy API service
kubectl apply -f k8s/api/

# Deploy Worker service
kubectl apply -f k8s/worker/

# Wait for deployments to be ready
kubectl rollout status deployment/imageviewer-api -n imageviewer
kubectl rollout status deployment/imageviewer-worker -n imageviewer
```

#### **Step 3: Deploy Monitoring**
```bash
# Deploy Prometheus
kubectl apply -f k8s/monitoring/prometheus/

# Deploy Grafana
kubectl apply -f k8s/monitoring/grafana/

# Deploy AlertManager
kubectl apply -f k8s/monitoring/alertmanager/
```

#### **Step 4: Verify Deployment**
```bash
# Check pod status
kubectl get pods -n imageviewer

# Check service status
kubectl get services -n imageviewer

# Check ingress status
kubectl get ingress -n imageviewer

# Run health checks
kubectl exec -it deployment/imageviewer-api -n imageviewer -- curl http://localhost/health
```

### **3. Post-Deployment Validation**
- [ ] All services running
- [ ] Health checks passing
- [ ] Monitoring working
- [ ] Alerts configured
- [ ] Performance metrics normal
- [ ] User acceptance testing

### **4. Rollback Procedure**
```bash
# Rollback API deployment
kubectl rollout undo deployment/imageviewer-api -n imageviewer

# Rollback Worker deployment
kubectl rollout undo deployment/imageviewer-worker -n imageviewer

# Verify rollback
kubectl rollout status deployment/imageviewer-api -n imageviewer
kubectl rollout status deployment/imageviewer-worker -n imageviewer
```

## 📈 Scaling Strategy

### **Horizontal Pod Autoscaler**
```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: imageviewer-api-hpa
  namespace: imageviewer
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: imageviewer-api
  minReplicas: 3
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
```

### **Vertical Pod Autoscaler**
```yaml
apiVersion: autoscaling.k8s.io/v1
kind: VerticalPodAutoscaler
metadata:
  name: imageviewer-api-vpa
  namespace: imageviewer
spec:
  targetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: imageviewer-api
  updatePolicy:
    updateMode: "Auto"
  resourcePolicy:
    containerPolicies:
    - containerName: api
      minAllowed:
        cpu: 100m
        memory: 128Mi
      maxAllowed:
        cpu: 500m
        memory: 512Mi
```

## 🎯 Conclusion

Deployment guide này cung cấp comprehensive approach để deploy ImageViewer Platform với:

1. **Infrastructure**: Containerized services với Kubernetes
2. **Automation**: CI/CD pipeline với GitHub Actions
3. **Monitoring**: Prometheus, Grafana, và AlertManager
4. **Security**: Network policies, pod security policies
5. **Scaling**: Horizontal và vertical pod autoscaling
6. **Reliability**: Health checks, rollback procedures

Strategy này đảm bảo platform được deploy reliably, securely, và có thể scale được theo demand.

---

**Created**: 2025-01-04
**Status**: Ready for Implementation
**Priority**: High
